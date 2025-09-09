using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class KatalogIndex(IHttpClientFactory httpFactory) : PageModel
{
    private readonly IHttpClientFactory _httpFactory = httpFactory;

    public string Query { get; set; } = "";
    public List<ItemVm> Items { get; set; } = new();

    public async Task OnGet([FromQuery] string? q)
    {
        Query = q ?? "";
        var client = _httpFactory.CreateClient("gql");

        var gql = @"
query Books($where: KsiazkaFilterInput, $order: [KsiazkaSortInput!]) {
  ksiazki(where: $where, order: $order) {
    id tytul autor rok
  }
}";
        var variables = new
        {
            where = string.IsNullOrWhiteSpace(Query) ? null : new
            {
                or = new object[]
                {
                    new { tytul = new { contains = Query } },
                    new { autor = new { contains = Query } },
                    new { isbn  = new { contains = Query } }
                }
            },
            order = new[] { new { rok = "DESC" } }
        };

        var payload = JsonContent.Create(new { query = gql, variables });
        var resp = await client.PostAsync("", payload);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Items = json.GetProperty("data").GetProperty("ksiazki")
            .EnumerateArray()
            .Select(e => new ItemVm
            {
                Id = e.GetProperty("id").GetInt32(),
                Tytul = e.GetProperty("tytul").GetString()!,
                Autor = e.GetProperty("autor").GetString()!,
                Rok = e.GetProperty("rok").GetInt32()
            }).ToList();
    }

    public record ItemVm(int Id, string Tytul, string Autor, int Rok);
}
