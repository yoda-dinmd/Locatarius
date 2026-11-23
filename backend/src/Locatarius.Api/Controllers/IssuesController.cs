using System.Text;
using System.Text.Json;
using Locatarius.Api.Auth;
using Locatarius.Api.Json;
using Locatarius.Api.Validation;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure.Auth;
using Locatarius.Infrastructure.Issues;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Locatarius.Api.Controllers;

[ApiController]
[Route("api/issues")]
public sealed class IssuesController(
    IssueService issueService,
    SessionAuthorizationService sessionAuthorizationService,
    IAntiforgery antiforgery) : ControllerBase
{
    private const int MaxBodyBytes = 16 * 1024;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var (authorization, denied) = await AuthorizeAsync(requireAdmin: false, cancellationToken);
        if (denied is not null) return denied;

        return Ok(await issueService.ListAsync(authorization!.User!, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var (authorization, denied) = await AuthorizeAsync(requireAdmin: false, cancellationToken);
        if (denied is not null) return denied;

        var issue = await issueService.GetAsync(authorization!.User!, id, cancellationToken);
        return issue is null ? IssueNotFound() : Ok(issue);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var (bytes, length, bodyError) = await ReadProtectedBodyAsync(cancellationToken);
        if (bodyError is not null) return bodyError;

        var (authorization, denied) = await AuthorizeAsync(requireAdmin: false, cancellationToken);
        if (denied is not null) return denied;
        var user = authorization!.User!;
        if (user.Role?.Role != UserRoleType.Resident)
            return ApiErrors.Forbidden("FORBIDDEN", "Only residents can report issues.");

        if (!TryParseObject(bytes!, length, IssueRequestValidator.CreateFields, out var values))
            return ApiErrors.InvalidRequest();
        var validation = IssueRequestValidator.ValidateCreate(values);
        if (validation.Fields.Count != 0) return ApiErrors.ValidationFailed(validation.Fields);

        var result = await issueService.CreateAsync(user, validation.Command!, cancellationToken);
        return result.Status switch
        {
            CreateIssueStatus.ApartmentNotAssigned =>
                ApiErrors.NotFound("APARTMENT_NOT_FOUND", "No apartment is assigned to your account."),
            _ => CreatedAtAction(nameof(Get), new { id = result.Issue!.IssueId }, result.Issue)
        };
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var (bytes, length, bodyError) = await ReadProtectedBodyAsync(cancellationToken);
        if (bodyError is not null) return bodyError;

        var (authorization, denied) = await AuthorizeAsync(requireAdmin: true, cancellationToken);
        if (denied is not null) return denied;

        if (!TryParseObject(bytes!, length, IssueRequestValidator.UpdateFields, out var values))
            return ApiErrors.InvalidRequest();
        var validation = IssueRequestValidator.ValidateUpdate(values);
        if (validation.Fields.Count != 0) return ApiErrors.ValidationFailed(validation.Fields);

        var result = await issueService.UpdateAsync(
            authorization!.User!.UserId, id, validation.Command!, cancellationToken);
        return result.Status switch
        {
            UpdateIssueStatus.NotFound => IssueNotFound(),
            UpdateIssueStatus.ResolvedPriorityLocked =>
                ApiErrors.Conflict("ISSUE_RESOLVED", "Reopen the issue before changing its priority."),
            _ => Ok(result.Issue)
        };
    }

    private static IActionResult IssueNotFound() => ApiErrors.NotFound("ISSUE_NOT_FOUND", "Issue not found.");

    private async Task<(SessionAuthorizationResult? Authorization, IActionResult? Denied)> AuthorizeAsync(
        bool requireAdmin, CancellationToken cancellationToken)
    {
        var authorization = await sessionAuthorizationService.AuthorizeAsync(
            Request.Cookies[SessionCookieOptions.CookieName], SessionType.Full, requireAdmin, cancellationToken);
        return authorization.Status switch
        {
            SessionAuthorizationStatus.Authorized => (authorization, null),
            SessionAuthorizationStatus.Unauthenticated =>
                (null, ApiErrors.Unauthorized("UNAUTHENTICATED", "Authentication required.")),
            SessionAuthorizationStatus.Forbidden =>
                (null, ApiErrors.Forbidden("FORBIDDEN", "Administrator access required.")),
            _ => (null, ApiErrors.Forbidden("FORBIDDEN", "Change your password to continue."))
        };
    }

    // Enforces JSON content type, the 16 KiB body cap (even without Content-Length) and CSRF.
    private async Task<(byte[]? Bytes, int Length, IActionResult? Error)> ReadProtectedBodyAsync(
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(Request.ContentType) ||
            !Request.ContentType.Split(';')[0].Trim().Equals("application/json", StringComparison.OrdinalIgnoreCase))
            return (null, 0, ApiErrors.UnsupportedMediaType());
        if (Request.ContentLength > MaxBodyBytes) return (null, 0, ApiErrors.PayloadTooLarge());

        var bytes = new byte[MaxBodyBytes + 1];
        var total = 0;
        while (total < bytes.Length)
        {
            var read = await Request.Body.ReadAsync(bytes.AsMemory(total), cancellationToken);
            if (read == 0) break;
            total += read;
        }
        if (total > MaxBodyBytes) return (null, 0, ApiErrors.PayloadTooLarge());

        try { await antiforgery.ValidateRequestAsync(HttpContext); }
        catch (AntiforgeryValidationException)
        {
            return (null, 0, ApiErrors.Forbidden("CSRF_INVALID", "Refresh the page and try again."));
        }
        return (bytes, total, null);
    }

    // Accepts only a JSON object with known, non-duplicated property names. Values are type-checked by the validator.
    private static bool TryParseObject(
        byte[] bytes, int length, IReadOnlySet<string> allowed, out Dictionary<string, JsonElement> values)
    {
        values = new Dictionary<string, JsonElement>();
        try
        {
            using var document = JsonDocument.Parse(new UTF8Encoding(false, true).GetString(bytes, 0, length));
            if (document.RootElement.ValueKind != JsonValueKind.Object) return false;
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!allowed.Contains(property.Name)) return false;
                // Decode strings now so unpaired surrogate escapes fail here instead of in the validator.
                if (property.Value.ValueKind == JsonValueKind.String) _ = property.Value.GetString();
                if (!values.TryAdd(property.Name, property.Value.Clone())) return false;
            }
            return true;
        }
        catch (JsonException) { return false; }
        catch (DecoderFallbackException) { return false; }
        catch (InvalidOperationException) { return false; }
    }
}
