namespace Biblioteka.Razor.Services;

public interface ITokenStore
{
    string? GetToken(HttpContext ctx);
    void SetToken(HttpContext ctx, string token);
    void Clear(HttpContext ctx);
}
