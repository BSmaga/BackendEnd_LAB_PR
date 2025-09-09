using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Biblioteka.Razor.Services;

public class LogoutModel : PageModel
{
    private readonly ITokenStore _store;
    public LogoutModel(ITokenStore store) => _store = store;

    public async Task OnGet()
    {
        _store.Clear(HttpContext);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Redirect("/Account/Login");
    }
}
