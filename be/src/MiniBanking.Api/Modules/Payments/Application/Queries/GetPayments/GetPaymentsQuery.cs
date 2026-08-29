using MediatR;

namespace MiniBanking.Modules.Payments.Application.Queries.GetPayments;

public sealed record PaymentSummaryDto(
    Guid PaymentId,
    string MerchantId,
    string MerchantOrderId,
    Guid WalletAccountId,
    string? WalletAccountNumber,
    string? CustomerName,
    string? CustomerEmail,
    long Amount,
    string Currency,
    string Status,
    string? FailureCode,
    string? Description,
    string IdempotencyKey,
    Guid? LedgerTransactionId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record PaymentsListResponse(
    IReadOnlyList<PaymentSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record GetPaymentsQuery(
    int? Page = 1,
    int? PageSize = 20,
    string? Status = null,
    string? MerchantId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<PaymentsListResponse>;
