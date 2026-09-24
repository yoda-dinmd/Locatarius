using Locatarius.Api.Auth;
using Locatarius.Api.Json;
using Locatarius.Domain.Enums;
using Locatarius.Infrastructure.Auth;
using Locatarius.Infrastructure.Residents;
using Microsoft.AspNetCore.Mvc;

namespace Locatarius.Api.Controllers;

[ApiController]
[Route("api/residents")]
public sealed class ResidentsController(
    ResidentAdministrationService residentAdministrationService,
    SessionAuthorizationService sessionAuthorizationService) : ControllerBase
{
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
