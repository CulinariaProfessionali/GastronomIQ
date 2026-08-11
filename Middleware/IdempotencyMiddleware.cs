using GastronomIQ.Application.Platform;

namespace GastronomIQ.Api.Middleware;

public sealed class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IIdempotencyStore store)
    {
        if (!HttpMethods.IsPost(context.Request.Method) ||
            !context.Request.Headers.TryGetValue(
                "Idempotency-Key",
                out var key))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = key.ToString();

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _next(context);
            return;
        }

        var existing = await store.GetAsync(
            idempotencyKey,
            context.RequestAborted);

        if (existing is not null)
        {
            context.Response.StatusCode = existing.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(existing.ResponseBody);
            return;
        }

        await _next(context);
    }
}
