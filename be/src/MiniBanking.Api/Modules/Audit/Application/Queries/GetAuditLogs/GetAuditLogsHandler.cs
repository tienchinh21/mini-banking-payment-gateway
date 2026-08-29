using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;

namespace MiniBanking.Modules.Audit.Application.Queries.GetAuditLogs;

public sealed class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, AuditLogsListResponse>
{
    private readonly MiniBankingDbContext _dbContext;

    public GetAuditLogsHandler(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuditLogsListResponse> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var p = Math.Max(1, request.Page ?? 1);
        var ps = Math.Clamp(request.PageSize ?? 20, 1, 100);

        var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(a => a.Action.Contains(request.Action));

        if (!string.IsNullOrWhiteSpace(request.Actor))
            query = query.Where(a => a.ActorEmail.Contains(request.Actor) || a.ActorId.Contains(request.Actor));

        if (request.FromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= request.FromDate.Value.ToUniversalTime());

        if (request.ToDate.HasValue)
            query = query.Where(a => a.CreatedAt <= request.ToDate.Value.ToUniversalTime());

        var total = await query.CountAsync(cancellationToken);
        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        var items = logs.Select(l => new AuditLogSummaryDto(
            l.Id,
            l.Action,
            l.ActorId,
            l.ActorEmail,
            l.Resource,
            l.Method,
            l.Path,
            l.IpAddress,
            l.CorrelationId,
            l.ResponseStatusCode,
            l.CreatedAt
        )).ToList();

        var totalPages = (int)Math.Ceiling(total / (double)ps);

        return new AuditLogsListResponse(items, total, p, ps, totalPages);
    }
}
