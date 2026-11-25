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
    private const int MaxBodyBytes = 16 * 1024;

    [HttpPost]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var (bytes, length, bodyError) = await ReadProtectedBodyAsync(cancellationToken);
        if (bodyError is not null) return bodyError;
        var (authorization, denied) = await AuthorizeAdminAsync(cancellationToken);
        if (denied is not null) return denied;

        var values = new Dictionary<string, string?>();
        try
        {
            using var document = JsonDocument.Parse(new UTF8Encoding(false, true).GetString(bytes!, 0, length));
            if (document.RootElement.ValueKind != JsonValueKind.Object) return ApiErrors.InvalidRequest();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!ResidentRequestValidator.CreateFields.Contains(property.Name)
                    || property.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null)
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
            authorization!.User!.UserId, validation.Command!, cancellationToken);
        return result.Status switch
        {
            CreateResidentStatus.ApartmentNotFound => ApiErrors.NotFound("APARTMENT_NOT_FOUND", "Apartment not found."),
            CreateResidentStatus.DuplicateEmail => ApiErrors.Conflict("EMAIL_EXISTS", "An account with this email already exists."),
            _ => CreatedAtAction(nameof(List), result.Resident)
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var (bytes, length, bodyError) = await ReadProtectedBodyAsync(cancellationToken);
        if (bodyError is not null) return bodyError;
        var (authorization, denied) = await AuthorizeAdminAsync(cancellationToken);
        if (denied is not null) return denied;

        if (!TryParseUpdateObject(bytes!, length, out var values))
            return ApiErrors.InvalidRequest();

        var validation = ResidentRequestValidator.ValidateUpdate(values);
        if (validation.IsInvalidRequest) return ApiErrors.InvalidRequest();
        if (validation.Fields.Count != 0) return ApiErrors.ValidationFailed(validation.Fields);

        var result = await residentAdministrationService.UpdateResidentAsync(
            authorization!.User!.UserId, id, validation.Command!, cancellationToken);
        return result.Status switch
        {
            UpdateResidentStatus.ResidentNotFound => ApiErrors.NotFound("RESIDENT_NOT_FOUND", "Resident not found."),
            UpdateResidentStatus.ApartmentNotFound => ApiErrors.NotFound("APARTMENT_NOT_FOUND", "Apartment not found."),
            UpdateResidentStatus.DuplicateEmail => ApiErrors.Conflict("EMAIL_EXISTS", "An account with this email already exists."),
            _ => Ok(result.Resident)
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var csrfError = await ValidateCsrfAsync();
        if (csrfError is not null) return csrfError;
        var (authorization, denied) = await AuthorizeAdminAsync(cancellationToken);
        if (denied is not null) return denied;

        var status = await residentAdministrationService.DeactivateResidentAsync(
            authorization!.User!.UserId, id, cancellationToken);
        return status == DeactivateResidentStatus.ResidentNotFound
            ? ApiErrors.NotFound("RESIDENT_NOT_FOUND", "Resident not found.")
            : NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var (authorization, denied) = await AuthorizeAdminAsync(cancellationToken);
        if (denied is not null) return denied;

        return Ok(await residentAdministrationService.ListResidentsAsync(authorization!.User!.UserId, cancellationToken));
    }

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

        var csrfError = await ValidateCsrfAsync();
        if (csrfError is not null) return (null, 0, csrfError);

        return (bytes, total, null);
    }

    private async Task<IActionResult?> ValidateCsrfAsync()
    {
        try
        {
            await antiforgery.ValidateRequestAsync(HttpContext);
            return null;
        }
        catch (AntiforgeryValidationException)
        {
            return ApiErrors.Forbidden("CSRF_INVALID", "Refresh the page and try again.");
        }
    }

    private async Task<(SessionAuthorizationResult? Authorization, IActionResult? Denied)> AuthorizeAdminAsync(
        CancellationToken cancellationToken)
    {
        var authorization = await sessionAuthorizationService.AuthorizeAsync(
            Request.Cookies[SessionCookieOptions.CookieName], SessionType.Full, requireAdmin: true, cancellationToken);
        if (authorization.Status == SessionAuthorizationStatus.Unauthenticated)
            return (null, ApiErrors.Unauthorized("UNAUTHENTICATED", "Authentication required."));
        if (authorization.Status != SessionAuthorizationStatus.Authorized)
            return (null, ApiErrors.Forbidden("FORBIDDEN", "Administrator access required."));
        return (authorization, null);
    }

    private static bool TryParseUpdateObject(byte[] bytes, int length, out Dictionary<string, JsonElement> values)
    {
        values = new Dictionary<string, JsonElement>();
        try
        {
            using var document = JsonDocument.Parse(new UTF8Encoding(false, true).GetString(bytes, 0, length));
            if (document.RootElement.ValueKind != JsonValueKind.Object) return false;
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!ResidentRequestValidator.UpdateFields.Contains(property.Name)) return false;
                if (property.Name == "isActive")
                {
                    if (property.Value.ValueKind is not (JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null))
                        return false;
                }
                else
                {
                    if (property.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                        return false;
                    // Decode strings now so unpaired surrogate escapes fail here instead of in the validator.
                    if (property.Value.ValueKind == JsonValueKind.String) _ = property.Value.GetString();
                }
                if (!values.TryAdd(property.Name, property.Value.Clone())) return false;
            }
            return true;
        }
        catch (JsonException) { return false; }
        catch (DecoderFallbackException) { return false; }
        catch (InvalidOperationException) { return false; }
    }
}
