using MediatR;

namespace MiniBanking.Modules.Audit.Application.Queries.GetAuditLogs;

public sealed record AuditLogSummaryDto(
    Guid Id,
    string Action,
    string ActorId,
    string ActorEmail,
    string Resource,
    string Method,
    string Path,
    string? IpAddress,
    string? CorrelationId,
    int StatusCode,
    DateTime CreatedAt);

public sealed record AuditLogsListResponse(
    IReadOnlyList<AuditLogSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record GetAuditLogsQuery(
    int? Page = 1,
    int? PageSize = 20,
    string? Action = null,
    string? Actor = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<AuditLogsListResponse>;
