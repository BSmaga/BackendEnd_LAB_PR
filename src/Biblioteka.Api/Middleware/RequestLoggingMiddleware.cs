using System.Diagnostics;

namespace Biblioteka.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _log;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> log)
    {
        _next = next; _log = log;
    }

    public async Task Invoke(HttpContext ctx)
    {
        var sw = Stopwatch.StartNew();

        // przykład: wczytaj własny nagłówek klienta (opcjonalny)
        var client = ctx.Request.Headers.TryGetValue("X-Client", out var v) ? v.ToString() : "unknown";
        var reqId = ctx.TraceIdentifier;

        _log.LogInformation("⇢ {Method} {Path} (X-Client: {Client}, reqId: {ReqId})",
            ctx.Request.Method, ctx.Request.Path, client, reqId);

        await _next(ctx);

        sw.Stop();
        _log.LogInformation("⇠ {Status} {Path} ({Elapsed} ms, reqId: {ReqId})",
            ctx.Response.StatusCode, ctx.Request.Path, sw.ElapsedMilliseconds, reqId);
    }
}

// extension
public static class RequestLoggingExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        => app.UseMiddleware<RequestLoggingMiddleware>();
}
