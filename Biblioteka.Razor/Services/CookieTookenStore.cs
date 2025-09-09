using Microsoft.AspNetCore.Http;

namespace Biblioteka.Razor.Services;

public class CookieTokenStore : ITokenStore
{
    private const string CookieName = "jwt";

    public string? GetToken(HttpContext ctx) =>
        ctx.Request.Cookies.TryGetValue(CookieName, out var v) ? v : null;

    public void SetToken(HttpContext ctx, string token)
    {
        ctx.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = false
        });
    }

    public void Clear(HttpContext ctx) => ctx.Response.Cookies.Delete(CookieName);
}
