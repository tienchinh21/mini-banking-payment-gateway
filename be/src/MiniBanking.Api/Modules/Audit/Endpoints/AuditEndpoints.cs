using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Audit.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/admin").RequireAuthorization("Admin").WithTags("Admin - Audit");

        var handler = async (
            int? page,
            int? pageSize,
            string? action,
            string? actor,
            DateTime? fromDate,
            DateTime? toDate,
            MiniBankingDbContext db) =>
        {
            var p = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? 20, 1, 100);

            var query = db.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(action))
                query = query.Where(a => a.Action.Contains(action));

            if (!string.IsNullOrWhiteSpace(actor))
                query = query.Where(a => a.ActorEmail.Contains(actor) || a.ActorId.Contains(actor));

            if (fromDate.HasValue)
                query = query.Where(a => a.CreatedAt >= fromDate.Value.ToUniversalTime());

            if (toDate.HasValue)
                query = query.Where(a => a.CreatedAt <= toDate.Value.ToUniversalTime());

            var total = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((p - 1) * ps)
                .Take(ps)
                .ToListAsync();

            return Results.Ok(ApiResponse.Ok("Danh sách nhật ký hệ thống", new
            {
                Items = logs.Select(l => new
                {
                    l.Id,
                    l.Action,
                    ActorId = l.ActorId,
                    ActorEmail = l.ActorEmail,
                    l.Resource,
                    l.Method,
                    l.Path,
                    l.IpAddress,
                    l.CorrelationId,
                    StatusCode = l.ResponseStatusCode,
                    l.CreatedAt
                }),
                TotalCount = total,
                Page = p,
                PageSize = ps,
                TotalPages = (int)Math.Ceiling(total / (double)ps)
            }));
        };

        group.MapGet("/audit-logs", handler);
        group.MapGet("/audit/logs", handler);

        return routes;
    }
}
