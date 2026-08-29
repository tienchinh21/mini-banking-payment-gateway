using MediatR;

namespace MiniBanking.Modules.Ledger.Application.Queries.GetLedgerTransactions;

public sealed record LedgerTransactionEntrySummaryDto(
    Guid Id,
    Guid AccountId,
    string AccountType,
    long Amount,
    string Currency,
    bool IsDebit,
    int Sequence);

public sealed record LedgerTransactionListItemDto(
    Guid TransactionId,
    string ReferenceId,
    string Type,
    string Status,
    string Description,
    DateTime CreatedAt,
    int EntriesCount,
    long TotalDebit,
    long TotalCredit,
    bool IsBalanced,
    IReadOnlyList<LedgerTransactionEntrySummaryDto> Entries);

public sealed record GetLedgerTransactionsResponse(
    IReadOnlyList<LedgerTransactionListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record GetLedgerTransactionsQuery(
    int? Page = 1,
    int? PageSize = 20,
    string? Type = null) : IRequest<GetLedgerTransactionsResponse>;
