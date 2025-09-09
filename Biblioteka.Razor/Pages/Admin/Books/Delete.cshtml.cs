using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteka.Razor.Pages.Books;

[Authorize(Policy = "AdminOnly")]
public class DeleteModel : PageModel
{
    private readonly IHttpClientFactory _http;
    public DeleteModel(IHttpClientFactory http) => _http = http;

    public BookDto? Item { get; set; }

    public async Task OnGetAsync(int id)
    {
        var client = _http.CreateClient("api");
        Item = await client.GetFromJsonAsync<BookDto>($"/api/ksiazki/{id}");
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var client = _http.CreateClient("api");
        var resp = await client.DeleteAsync($"/api/ksiazki/{id}");
        return RedirectToPage("Index");
    }
}
