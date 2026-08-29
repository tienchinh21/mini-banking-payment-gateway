using MediatR;

namespace MiniBanking.Modules.Ledger.Application.Queries.GetLedgerTransactionById;

public sealed record LedgerTransactionEntryDto(
    Guid EntryId,
    Guid AccountId,
    string AccountType,
    long Amount,
    string Currency,
    bool IsDebit);

public sealed record GetLedgerTransactionByIdResponse(
    Guid TransactionId,
    string ReferenceId,
    string Type,
    string Status,
    string Description,
    IEnumerable<LedgerTransactionEntryDto> Entries);

public sealed record GetLedgerTransactionByIdQuery(Guid Id) : IRequest<GetLedgerTransactionByIdResponse?>;
