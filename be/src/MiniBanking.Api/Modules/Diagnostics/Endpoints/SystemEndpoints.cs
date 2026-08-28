using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using MiniBanking.SharedKernel;
using Serilog;

namespace MiniBanking.Modules.Diagnostics.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder routes)
    {
        var healthOptions = new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                var response = new
                {
                    Status = report.Status.ToString(),
                    TotalDuration = report.TotalDuration.TotalMilliseconds,
                    Checks = report.Entries.Select(e => new
                    {
                        Name = e.Key,
                        Status = e.Value.Status.ToString(),
                        Duration = e.Value.Duration.TotalMilliseconds,
                        Exception = e.Value.Exception?.Message
                    })
                };

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(response);
            }
        };

        routes.MapHealthChecks("/health", healthOptions);
        routes.MapHealthChecks("/api/v1/health", healthOptions);

        routes.MapGet("/api/v1/system/info", (IHostEnvironment env) =>
        {
            return Results.Ok(ApiResponse.Ok("Thông tin hệ thống", new
            {
                Name = "Mini Banking API",
                Version = "1.0.0",
                Framework = ".NET 8",
                Environment = env.EnvironmentName
            }));
        }).WithTags("System");

        routes.MapPost("/api/v1/demo/webhook-receiver", async (HttpContext context) =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            Log.Information("Received webhook event: {Body}", body);
            return Results.Ok();
        }).WithTags("Demo");

        return routes;
    }
}
