using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteka.Razor.Pages.Books;

[Authorize(Policy = "AdminOnly")]
public class EditModel : PageModel
{
    private readonly IHttpClientFactory _http;
    public EditModel(IHttpClientFactory http) => _http = http;

    [BindProperty] public BookVm Form { get; set; } = new();

    public async Task OnGetAsync(int id)
    {
        var client = _http.CreateClient("api");
        var dto = await client.GetFromJsonAsync<BookVm>($"/api/ksiazki/{id}");
        if (dto is not null) Form = dto;
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var client = _http.CreateClient("api");
        var resp = await client.PutAsJsonAsync($"/api/ksiazki/{id}", Form);
        if (!resp.IsSuccessStatusCode) return Page();
        return RedirectToPage("Index");
    }

    public class BookVm
    {
        public int Id { get; set; }
        public string Tytul { get; set; } = "";
        public string Autor { get; set; } = "";
        public int Rok { get; set; }
        public string ISBN { get; set; } = "";
        public int LiczbaEgzemplarzy { get; set; }
    }
}
