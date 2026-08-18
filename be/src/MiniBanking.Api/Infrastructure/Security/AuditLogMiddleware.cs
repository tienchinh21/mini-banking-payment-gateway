using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Admin.Domain;
using System.Security.Claims;

namespace MiniBanking.Infrastructure.Security;

public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;

    public AuditLogMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, MiniBankingDbContext dbContext)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Only audit admin endpoints.
        if (!path.StartsWith("/api/v1/admin", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering();
        string? requestBody = null;

        if (context.Request.ContentLength > 0 && context.Request.ContentLength <= 1024 * 1024)
        {
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        await _next(context);

        var user = context.User;
        var actorId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
        var actorEmail = user.FindFirst(ClaimTypes.Email)?.Value ?? "anonymous";

        var auditLog = new AuditLog(
            actorId,
            actorEmail,
            ExtractAction(context.Request.Method),
            ExtractResource(path),
            context.Request.Method,
            path,
            requestBody,
            context.Response.StatusCode,
            context.Connection.RemoteIpAddress?.ToString(),
            context.Items["CorrelationId"] as string);

        dbContext.AuditLogs.Add(auditLog);
        await dbContext.SaveChangesAsync();
    }

    private static string ExtractAction(string method)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" => "Read",
            "POST" => "Create",
            "PUT" => "Update",
            "PATCH" => "Update",
            "DELETE" => "Delete",
            _ => method
        };
    }

    private static string ExtractResource(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 4 ? segments[3] : path;
    }
}
