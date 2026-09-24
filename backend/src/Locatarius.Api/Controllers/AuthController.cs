using System.Text;
using System.Text.Json;
using Locatarius.Api.Json;
using Locatarius.Api.Validation;
using Locatarius.Infrastructure.Auth;
using Locatarius.Api.Auth;
using Locatarius.Domain.Enums;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Locatarius.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    AuthenticationService authenticationService,
    SessionAuthorizationService sessionAuthorizationService,
    IAntiforgery antiforgery) : ControllerBase
{
    private const long MaxBodyBytes = 16 * 1024;

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var authorization = await sessionAuthorizationService.AuthorizeAsync(
            Request.Cookies[SessionCookieOptions.CookieName],
            requiredType: null, requireAdmin: false, cancellationToken);
        if (authorization.Status != SessionAuthorizationStatus.Authorized)
            return ApiErrors.Unauthorized("UNAUTHENTICATED", "Authentication required.");

        var user = authorization.User!;
        return Ok(new
        {
            id = user.UserId,
            name = $"{user.FirstName} {user.LastName}",
            email = user.Credential!.Email,
            role = user.Role!.Role == UserRoleType.Admin ? "admin" : "resident",
            mustChangePassword = user.Credential.MustChangePassword
        });
    }

    [HttpGet("csrf")]
    public async Task<IActionResult> GetCsrfToken(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        var lookup = await authenticationService.ResolveSessionAsync(
            Request.Cookies[SessionCookieOptions.CookieName], cancellationToken);
        if (lookup.IsValid)
            await authenticationService.RecordAcceptedActivityAsync(
                lookup.Session!.SessionId, cancellationToken);
        return Ok(new { token = tokens.RequestToken });
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        var contentTypeError = CheckContentType();
        if (contentTypeError is not null) return contentTypeError;

        var (body, sizeError) = await ReadBodyAsync(cancellationToken);
        if (sizeError is not null) return sizeError;

        try
        {
            await antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return ApiErrors.Forbidden("CSRF_INVALID", "Refresh the page and try again.");
        }

        if (!TryParseLoginBody(body!, out var emailRaw, out var passwordRaw, out var parseError))
        {
            return parseError!;
        }

        var validation = LoginRequestValidator.Validate(emailRaw, passwordRaw);

        if (!validation.IsValid)
        {
            return ApiErrors.ValidationFailed(validation.Fields!);
        }

        var outcome = await authenticationService.LoginAsync(
            validation.CanonicalEmail!, passwordRaw!, cancellationToken);

        if (outcome.Status == LoginResultStatus.InvalidCredentials)
        {
            return ApiErrors.Unauthorized("INVALID_CREDENTIALS", "Email or password is incorrect.");
        }

        var expires = outcome.SessionType == Locatarius.Domain.Enums.SessionType.Full
            ? DateTimeOffset.UtcNow.AddHours(8)
            : DateTimeOffset.UtcNow.AddMinutes(5);

        Response.Cookies.Append(
            SessionCookieOptions.CookieName,
            outcome.SessionToken!,
            SessionCookieOptions.Build(expires));

        return Ok(new { next = outcome.NextStep });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var contentTypeError = CheckContentType();
        if (contentTypeError is not null) return contentTypeError;
        var (body, sizeError) = await ReadBodyAsync(cancellationToken);
        if (sizeError is not null) return sizeError;
        try
        {
            await antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return ApiErrors.Forbidden("CSRF_INVALID", "Refresh the page and try again.");
        }

        var authorization = await sessionAuthorizationService.AuthorizeAsync(
            Request.Cookies[SessionCookieOptions.CookieName], SessionType.PasswordChange,
            requireAdmin: false, cancellationToken);
        if (authorization.Status == SessionAuthorizationStatus.WrongSessionType)
            return ApiErrors.Forbidden("FORBIDDEN", "Password change session required.");
        if (authorization.Status != SessionAuthorizationStatus.Authorized)
            return ApiErrors.Unauthorized("UNAUTHENTICATED", "Authentication required.");

        if (!TryParseStringPairBody(body!, "newPassword", "confirmPassword",
                out var newPassword, out var confirmPassword, out var parseError))
            return parseError!;
        var fields = ChangePasswordRequestValidator.Validate(newPassword, confirmPassword);
        if (fields.Count != 0) return ApiErrors.ValidationFailed(fields);

        var outcome = await authenticationService.ChangePasswordAsync(
            authorization.Session!, newPassword!, cancellationToken);
        if (outcome.Status != LoginResultStatus.Success)
            return ApiErrors.Unauthorized("UNAUTHENTICATED", "Authentication required.");
        Response.Cookies.Append(SessionCookieOptions.CookieName, outcome.SessionToken!,
            SessionCookieOptions.Build(DateTimeOffset.UtcNow.AddHours(8)));
        return Ok(new { next = "app" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        var contentTypeError = CheckContentType();
        if (contentTypeError is not null) return contentTypeError;

        var (_, sizeError) = await ReadBodyAsync(cancellationToken);
        if (sizeError is not null) return sizeError;

        var rawToken = Request.Cookies[SessionCookieOptions.CookieName];
        var lookup = await authenticationService.ResolveSessionAsync(rawToken, cancellationToken);

        if (!lookup.IsValid)
        {
            return ApiErrors.Unauthorized("UNAUTHENTICATED", "Authentication required.");
        }

        try
        {
            await antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return ApiErrors.Forbidden("CSRF_INVALID", "Refresh the page and try again.");
        }

        await authenticationService.LogoutAsync(lookup.Session!, cancellationToken);

        Response.Cookies.Append(
            SessionCookieOptions.CookieName,
            string.Empty,
            SessionCookieOptions.BuildExpired());

        return Ok(new { message = "Signed out." });
    }

    private IActionResult? CheckContentType()
    {
        var contentType = Request.ContentType;
        if (string.IsNullOrEmpty(contentType) ||
            !contentType.Split(';')[0].Trim().Equals("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return ApiErrors.UnsupportedMediaType();
        }
        return null;
    }

    private async Task<(string? Body, IActionResult? Error)> ReadBodyAsync(CancellationToken ct)
    {
        if (Request.ContentLength is { } len && len > MaxBodyBytes)
        {
            return (null, ApiErrors.PayloadTooLarge());
        }

        // Read at most the limit plus one byte, including chunked requests.
        var bytes = new byte[MaxBodyBytes + 1];
        var total = 0;
        while (total < bytes.Length)
        {
            var read = await Request.Body.ReadAsync(bytes.AsMemory(total), ct);
            if (read == 0) break;
            total += read;
        }
        if (total > MaxBodyBytes) return (null, ApiErrors.PayloadTooLarge());
        try
        {
            return (new UTF8Encoding(false, true).GetString(bytes, 0, total), null);
        }
        catch (DecoderFallbackException)
        {
            return (null, ApiErrors.InvalidRequest());
        }
    }

    private static bool TryParseLoginBody(
        string body, out string? email, out string? password, out IActionResult? error)
        => TryParseStringPairBody(body, "email", "password", out email, out password, out error);

    private static bool TryParseStringPairBody(
        string body, string firstProperty, string secondProperty,
        out string? firstValue, out string? secondValue, out IActionResult? error)
    {
        firstValue = null;
        secondValue = null;
        error = null;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            error = ApiErrors.InvalidRequest();
            return false;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = ApiErrors.InvalidRequest();
                return false;
            }

            var seen = new HashSet<string>();
            var allowed = new HashSet<string> { firstProperty, secondProperty };

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!allowed.Contains(property.Name) || !seen.Add(property.Name))
                {
                    error = ApiErrors.InvalidRequest();
                    return false;
                }
            }

            if (document.RootElement.TryGetProperty(firstProperty, out var firstElement))
            {
                if (firstElement.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                {
                    error = ApiErrors.InvalidRequest();
                    return false;
                }
                firstValue = firstElement.GetString();
            }

            if (document.RootElement.TryGetProperty(secondProperty, out var secondElement))
            {
                if (secondElement.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                {
                    error = ApiErrors.InvalidRequest();
                    return false;
                }
                secondValue = secondElement.GetString();
            }
        }

        return true;
    }
}
