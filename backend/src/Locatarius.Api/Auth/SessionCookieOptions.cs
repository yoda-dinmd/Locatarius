using Microsoft.AspNetCore.Http;

namespace Locatarius.Api.Auth;

public static class SessionCookieOptions
{
    public const string CookieName = "__Host-locatarius";

    public static CookieOptions Build(DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = expires,
        IsEssential = true
    };

    public static CookieOptions BuildExpired() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = DateTimeOffset.UnixEpoch,
        IsEssential = true
    };
}