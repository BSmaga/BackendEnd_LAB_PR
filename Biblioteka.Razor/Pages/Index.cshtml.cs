using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteka.Razor.Pages.Books;

[Authorize] // obie role mog¹ przegl¹daæ
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _http;
    public IndexModel(IHttpClientFactory http) => _http = http;

    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public int Page { get; set; } = 1;

    public PageResult<BookDto>? Data { get; set; }

    public async Task OnGetAsync()
    {
        var client = _http.CreateClient("api");
        var url = $"/api/ksiazki?q={Uri.EscapeDataString(Q ?? "")}&page={Page}&pageSize=10&sort=tytul&desc=false";
        Data = await client.GetFromJsonAsync<PageResult<BookDto>>(url);
    }
}
