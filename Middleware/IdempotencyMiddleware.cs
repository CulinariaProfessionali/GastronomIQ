using System.Security.Cryptography;
using System.Text;
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

        var fingerprint = await CreateFingerprintAsync(context.Request);

        var existing = await store.GetAsync(
            idempotencyKey,
            context.RequestAborted);

        if (existing is not null)
        {
            if (!string.Equals(
                    existing.Fingerprint,
                    fingerprint,
                    StringComparison.Ordinal))
            {
                context.Response.StatusCode =
                    StatusCodes.Status409Conflict;

                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync(
                    """{"error":"Idempotency-Key reuse with different request payload"}""",
                    context.RequestAborted);

                return;
            }

            context.Response.StatusCode = existing.StatusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(
                existing.ResponseBody,
                context.RequestAborted);

            return;
        }

        var originalResponseBody = context.Response.Body;

        await using var responseBuffer = new MemoryStream();

        context.Response.Body = responseBuffer;

        try
        {
            await _next(context);

            responseBuffer.Position = 0;

            using var reader = new StreamReader(
                responseBuffer,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);

            var responseBody = await reader.ReadToEndAsync();

            var record = new IdempotencyRecord(
                idempotencyKey,
                fingerprint,
                context.Response.StatusCode,
                responseBody,
                DateTimeOffset.UtcNow);

            await store.SaveAsync(
                record,
                context.RequestAborted);

            responseBuffer.Position = 0;

            await responseBuffer.CopyToAsync(
                originalResponseBody,
                context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalResponseBody;
        }
    }

    private static async Task<string> CreateFingerprintAsync(
        HttpRequest request)
    {
        request.EnableBuffering();

        request.Body.Position = 0;

        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync();

        request.Body.Position = 0;

        var fingerprintInput =
            $"{request.Method}\n{request.Path}\n{request.QueryString}\n{body}";

        var bytes = Encoding.UTF8.GetBytes(fingerprintInput);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }
}