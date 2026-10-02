using System.Text.Json;

namespace GastronomIQ.Api.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ApiExceptionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblem(
                context,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteProblem(
                context,
                StatusCodes.Status404NotFound,
                "Not Found",
                ex.Message);
        }
        catch (ArgumentException ex)
        {
            await WriteProblem(
                context,
                StatusCodes.Status400BadRequest,
                "Validation error",
                ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteProblem(
                context,
                StatusCodes.Status409Conflict,
                "Operation rejected",
                ex.Message);
        }
    }

    private static async Task WriteProblem(
        HttpContext context,
        int status,
        string title,
        string detail)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var body = JsonSerializer.Serialize(new
        {
            type = "about:blank",
            title,
            status,
            detail,
            correlationId =
                context.Response.Headers["X-Correlation-ID"].ToString()
        });

        await context.Response.WriteAsync(body);
    }
}
