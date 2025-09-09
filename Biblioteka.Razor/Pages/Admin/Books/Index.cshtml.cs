using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;

[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _http;
    public List<BookVm> Books { get; set; } = new();

    public IndexModel(IHttpClientFactory http) => _http = http;

    public async Task OnGet()
    {
        var client = _http.CreateClient("api");
        var paged = await client.GetFromJsonAsync<Paged<BookVm>>("/api/ksiazki?page=1&pageSize=50");
        Books = paged?.Items ?? new();
    }

    public record Paged<T>(int Page, int PageSize, int Total, int TotalPages, List<T> Items);
    public record BookVm(int Id, string Tytul, string Autor, int Rok, string ISBN, int LiczbaEgzemplarzy);
}
