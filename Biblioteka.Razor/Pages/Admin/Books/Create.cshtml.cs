using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteka.Razor.Pages.Books;

[Authorize(Policy = "AdminOnly")]
public class CreateModel : PageModel
{
    private readonly IHttpClientFactory _http;
    public CreateModel(IHttpClientFactory http) => _http = http;

    [BindProperty] public BookCreateDto Form { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var client = _http.CreateClient("api");
        var resp = await client.PostAsJsonAsync("/api/ksiazki", Form);
        if (!resp.IsSuccessStatusCode) return Page();
        return RedirectToPage("Index");
    }

    public class BookCreateDto
    {
        public string Tytul { get; set; } = "";
        public string Autor { get; set; } = "";
        public int Rok { get; set; }
        public string ISBN { get; set; } = "";
        public int LiczbaEgzemplarzy { get; set; } = 1;
    }
}
