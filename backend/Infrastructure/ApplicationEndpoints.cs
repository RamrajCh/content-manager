using ContentManager.Api.Features.Admin;
using ContentManager.Api.Features.Publishing;

namespace ContentManager.Api.Infrastructure;

public static class ApplicationEndpoints
{
    public static void MapApplicationEndpoints(this WebApplication app)
    {
        app.MapAdminEndpoints();
        app.MapPublishingEndpoints();
        app.MapGet("/api/v1/health/live", () => Results.Ok(new { status = "live" }));
        app.MapHealthChecks("/api/v1/health/ready", new()
        {
            ResponseWriter = async (context, report) =>
            {
                await context.Response.WriteAsJsonAsync(new
                {
                    status = report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
                        ? "ready" : "unavailable"
                });
            }
        });
    }
}
