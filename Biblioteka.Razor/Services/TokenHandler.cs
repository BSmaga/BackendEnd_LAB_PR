using System.Net.Http.Headers;

namespace Biblioteka.Razor.Services;

public class TokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _http;
    private readonly ITokenStore _store;

    public TokenHandler(IHttpContextAccessor http, ITokenStore store)
    {
        _http = http;
        _store = store;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _store.GetToken(_http.HttpContext!);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return base.SendAsync(request, cancellationToken);
    }
}
