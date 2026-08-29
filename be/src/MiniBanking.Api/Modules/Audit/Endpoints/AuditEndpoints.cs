using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MiniBanking.Modules.Audit.Application.Queries.GetAuditLogs;
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
            IMediator mediator) =>
        {
            var query = new GetAuditLogsQuery(page, pageSize, action, actor, fromDate, toDate);
            var result = await mediator.Send(query);

            return Results.Ok(ApiResponse.Ok("Danh sách nhật ký hệ thống", result));
        };

        group.MapGet("/audit-logs", handler);
        group.MapGet("/audit/logs", handler);

        return routes;
    }
}
