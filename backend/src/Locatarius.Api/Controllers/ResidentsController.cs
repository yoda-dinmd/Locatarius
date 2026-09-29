using Locatarius.Api.Auth;
using Locatarius.Api.Json;
using Locatarius.Api.Validation;
using System.Text;
using System.Text.Json;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure.Auth;
using Locatarius.Infrastructure.Residents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;

namespace Locatarius.Api.Controllers;

[ApiController]
[Route("api/residents")]
public sealed class ResidentsController(
    ResidentAdministrationService residentAdministrationService,
    SessionAuthorizationService sessionAuthorizationService,
    IAntiforgery antiforgery) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        if (string.IsNullOrEmpty(Request.ContentType) ||
            !Request.ContentType.Split(';')[0].Trim().Equals("application/json", StringComparison.OrdinalIgnoreCase))
            return ApiErrors.UnsupportedMediaType();
        const int maxBodyBytes = 16 * 1024;
        if (Request.ContentLength > maxBodyBytes) return ApiErrors.PayloadTooLarge();
        var bytes = new byte[maxBodyBytes + 1];
        var total = 0;
        while (total < bytes.Length)
        {
            var read = await Request.Body.ReadAsync(bytes.AsMemory(total), cancellationToken);
            if (read == 0) break;
            total += read;
        }
        if (total > maxBodyBytes) return ApiErrors.PayloadTooLarge();
        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException)
        {
            return ApiErrors.Forbidden("CSRF_INVALID", "Refresh the page and try again.");
        }
        var authorization = await sessionAuthorizationService.AuthorizeAsync(
            Request.Cookies[SessionCookieOptions.CookieName], SessionType.Full, requireAdmin: true, cancellationToken);
        if (authorization.Status == SessionAuthorizationStatus.Unauthenticated)
            return ApiErrors.Unauthorized("UNAUTHENTICATED", "Authentication required.");
        if (authorization.Status != SessionAuthorizationStatus.Authorized)
            return ApiErrors.Forbidden("FORBIDDEN", "Administrator access required.");

        var values = new Dictionary<string, string?>();
        var allowed = new HashSet<string> { "firstName", "lastName", "email", "apartmentId", "temporaryPassword", "confirmPassword" };
        try
        {
            using var document = JsonDocument.Parse(new UTF8Encoding(false, true).GetString(bytes, 0, total));
            if (document.RootElement.ValueKind != JsonValueKind.Object) return ApiErrors.InvalidRequest();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!allowed.Contains(property.Name) || property.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null)
                    || !values.TryAdd(property.Name, property.Value.GetString())) return ApiErrors.InvalidRequest();
            }
        }
        catch (JsonException) { return ApiErrors.InvalidRequest(); }
        catch (DecoderFallbackException) { return ApiErrors.InvalidRequest(); }
        // JSON accepts escaped surrogate code units, but accessing an unpaired
        // surrogate in a property name or string value fails during decoding.
        catch (InvalidOperationException) { return ApiErrors.InvalidRequest(); }

        var validation = ResidentRequestValidator.ValidateCreate(values);
        if (validation.Fields.Count != 0) return ApiErrors.ValidationFailed(validation.Fields);
        var result = await residentAdministrationService.CreateResidentAsync(
            authorization.User!.UserId, validation.Command!, cancellationToken);
        return result.Status switch
        {
            CreateResidentStatus.ApartmentNotFound => ApiErrors.NotFound("APARTMENT_NOT_FOUND", "Apartment not found."),
            CreateResidentStatus.DuplicateEmail => ApiErrors.Conflict("EMAIL_EXISTS", "An account with this email already exists."),
            _ => CreatedAtAction(nameof(List), result.Resident)
        };
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var authorization = await sessionAuthorizationService.AuthorizeAsync(
            Request.Cookies[SessionCookieOptions.CookieName],
            requiredType: SessionType.Full, requireAdmin: true, cancellationToken);
        if (authorization.Status == SessionAuthorizationStatus.Unauthenticated)
            return ApiErrors.Unauthorized("UNAUTHENTICATED", "Authentication required.");
        if (authorization.Status != SessionAuthorizationStatus.Authorized)
            return ApiErrors.Forbidden("FORBIDDEN", "Administrator access required.");

        return Ok(await residentAdministrationService.ListResidentsAsync(authorization.User!.UserId, cancellationToken));
    }
}
