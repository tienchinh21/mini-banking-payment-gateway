using MediatR;

namespace MiniBanking.Modules.Ledger.Application.Queries.GetLedgerEntries;

public sealed record LedgerEntryItemDto(
    Guid EntryId,
    Guid TransactionId,
    string? TransactionReference,
    string? TransactionType,
    string? TransactionDescription,
    Guid AccountId,
    string AccountType,
    string AccountDisplay,
    long Amount,
    string Currency,
    bool IsDebit,
    string TypeDisplay,
    int Sequence,
    DateTime CreatedAt);

public sealed record GetLedgerEntriesResponse(
    IReadOnlyList<LedgerEntryItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record GetLedgerEntriesQuery(
    int? Page = 1,
    int? PageSize = 20,
    Guid? AccountId = null,
    string? Currency = null,
    bool? IsDebit = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<GetLedgerEntriesResponse>;
