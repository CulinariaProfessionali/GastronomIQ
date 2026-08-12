using GastronomIQ.Application.Platform;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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
            !context.Request.Headers.TryGetValue("Idempotency-Key", out var key))
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

        var fingerprint = await BuildFingerprintAsync(context);
        var existing = await store.GetAsync(
            idempotencyKey,
            context.RequestAborted);

        if (existing is not null)
        {
            if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync("""{"type":"about:blank","title":"Conflict","status":409,"detail":"Idempotency-Key reuse with different request payload."}""");
                return;
            }

            context.Response.StatusCode = existing.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(existing.ResponseBody);
            return;
        }

        var originalBody = context.Response.Body;
        await using var captureBody = new MemoryStream();
        context.Response.Body = captureBody;

        try
        {
            await _next(context);
            captureBody.Position = 0;
            var responseBody = await new StreamReader(captureBody).ReadToEndAsync();

            if (ShouldPersist(context.Response, responseBody))
            {
                await store.SaveAsync(
                    new IdempotencyRecord(
                        idempotencyKey,
                        fingerprint,
                        context.Response.StatusCode,
                        responseBody,
                        DateTimeOffset.UtcNow),
                    context.RequestAborted);
            }

            captureBody.Position = 0;
            await captureBody.CopyToAsync(originalBody, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static async Task<string> BuildFingerprintAsync(HttpContext context)
    {
        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;

        var fingerprintSource = $"{context.Request.Method}:{context.Request.Path}:{context.Request.QueryString}:{body}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintSource));
        return Convert.ToHexString(hash);
    }

    private static bool ShouldPersist(HttpResponse response, string responseBody)
    {
        if (response.StatusCode >= StatusCodes.Status500InternalServerError)
            return false;

        if (string.IsNullOrWhiteSpace(responseBody))
            return false;

        if (!(response.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) ?? false))
            return false;

        try
        {
            using var _ = JsonDocument.Parse(responseBody);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
