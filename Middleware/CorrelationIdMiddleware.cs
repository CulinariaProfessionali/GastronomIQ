namespace GastronomIQ.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private const string Header = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        var id = context.Request.Headers[Header].FirstOrDefault()
                 ?? Guid.NewGuid().ToString("N");

        context.Response.Headers[Header] = id;
        await _next(context);
    }
}
