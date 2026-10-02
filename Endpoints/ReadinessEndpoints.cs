namespace GastronomIQ.Api.Endpoints;

public static class ReadinessEndpoints
{
    public static void MapReadinessEndpoints(this WebApplication app)
    {
        app.MapGet("/ready", () => Results.Ok(new {
            status = "ready",
            service = "GastronomIQ.Api"
        }));

        app.MapGet("/version", () => Results.Ok(new {
            application = "GastronomIQ",
            release = "MVP-RC",
            schema = 11
        }));
    }
}
