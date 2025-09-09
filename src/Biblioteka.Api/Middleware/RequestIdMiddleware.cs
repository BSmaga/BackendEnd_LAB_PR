namespace Biblioteka.Api.Middleware;

public sealed class RequestIdMiddleware
{
    private const string HeaderName = "X-Request-Id";
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey(HeaderName))
            context.Request.Headers[HeaderName] = Guid.NewGuid().ToString("N");

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = context.Request.Headers[HeaderName].ToString();
            return Task.CompletedTask;
        });

        await _next(context);
    }
}

public static class RequestIdMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestId(this IApplicationBuilder app)
        => app.UseMiddleware<RequestIdMiddleware>();
}
