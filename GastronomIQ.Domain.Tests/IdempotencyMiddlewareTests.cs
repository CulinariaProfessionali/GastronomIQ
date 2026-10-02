using System.Text;
using GastronomIQ.Api.Middleware;
using GastronomIQ.Infrastructure.Platform;
using Microsoft.AspNetCore.Http;

namespace GastronomIQ.Api.Tests;

public sealed class IdempotencyMiddlewareTests
{
    [Fact]
    public async Task Replays_Post_Response_Without_Reinvoking_Handler()
    {
        var store = new InMemoryIdempotencyStore();
        var invocations = 0;

        var middleware = new IdempotencyMiddleware(async context =>
        {
            invocations++;
            context.Response.StatusCode = StatusCodes.Status201Created;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("""{"ok":true}""");
        });

        var first = CreatePostContext("same-key", """{"value":1}""");
        await middleware.InvokeAsync(first, store);

        var second = CreatePostContext("same-key", """{"value":1}""");
        await middleware.InvokeAsync(second, store);
        second.Response.Body.Position = 0;
        var replayBody = await new StreamReader(second.Response.Body).ReadToEndAsync();

        Assert.Equal(1, invocations);
        Assert.Equal(StatusCodes.Status201Created, second.Response.StatusCode);
        Assert.Equal("""{"ok":true}""", replayBody);
    }

    [Fact]
    public async Task Returns_Conflict_When_Key_Reused_With_Different_Payload()
    {
        var store = new InMemoryIdempotencyStore();
        var invocations = 0;

        var middleware = new IdempotencyMiddleware(async context =>
        {
            invocations++;
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("""{"done":true}""");
        });

        var first = CreatePostContext("same-key", """{"value":1}""");
        await middleware.InvokeAsync(first, store);

        var second = CreatePostContext("same-key", """{"value":2}""");
        await middleware.InvokeAsync(second, store);
        second.Response.Body.Position = 0;
        var conflictBody = await new StreamReader(second.Response.Body).ReadToEndAsync();

        Assert.Equal(1, invocations);
        Assert.Equal(StatusCodes.Status409Conflict, second.Response.StatusCode);
        Assert.Contains("Idempotency-Key reuse with different request payload", conflictBody);
    }

    private static DefaultHttpContext CreatePostContext(string key, string body)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/v1/test";
        context.Request.ContentType = "application/json";
        context.Request.Headers.Append("Idempotency-Key", key);
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Response.Body = new MemoryStream();
        return context;
    }
}
