using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Biblioteka.Tests;

public class KsiazkiTests : IClassFixture<WebApplicationFactory<Biblioteka.Api.Program>>
{
    private readonly HttpClient _client;

    public KsiazkiTests(WebApplicationFactory<Biblioteka.Api.Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Ksiazki_ReturnsOK()
    {
        // proste smoke – endpoint listy książek
        var res = await _client.GetAsync("/api/ksiazki?page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
