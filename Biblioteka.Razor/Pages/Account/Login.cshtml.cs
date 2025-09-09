using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Biblioteka.Razor.Services;
using System.Net.Http.Json;

public class LoginModel : PageModel
{
    private readonly IHttpClientFactory _http;
    private readonly ITokenStore _store;

    public LoginModel(IHttpClientFactory http, ITokenStore store)
    {
        _http = http;
        _store = store;
    }

    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Haslo { get; set; } = "";
    public string? Error { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Haslo))
        {
            Error = "Podaj email i hasło.";
            return Page();
        }

        var client = _http.CreateClient("api");
        // Twój endpoint REST (Minimal API): query string email/haslo
        var resp = await client.PostAsync($"/api/auth/login?email={Uri.EscapeDataString(Email)}&haslo={Uri.EscapeDataString(Haslo)}", null);

        if (!resp.IsSuccessStatusCode)
        {
            Error = "Niepoprawne dane logowania.";
            return Page();
        }

        var json = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        var token = json?["token"];
        if (string.IsNullOrEmpty(token))
        {
            Error = "Brak tokenu w odpowiedzi.";
            return Page();
        }

        // zapisz token w cookie
        _store.SetToken(HttpContext, token);

        // odczyt roli z JWT
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var role = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value ?? "User";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, Email),
            new Claim(ClaimTypes.Role, role)
        };

        var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));

        return RedirectToPage("/Index");
    }
}
