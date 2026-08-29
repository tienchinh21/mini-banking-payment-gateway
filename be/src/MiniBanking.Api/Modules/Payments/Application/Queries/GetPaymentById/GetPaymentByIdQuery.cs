using MediatR;

namespace MiniBanking.Modules.Payments.Application.Queries.GetPaymentById;

public sealed record PaymentRefundDto(
    Guid RefundId,
    string MerchantRefundId,
    long Amount,
    string Currency,
    string Status,
    string? Reason,
    DateTime CreatedAt);

public sealed record PaymentDetailResponse(
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
    DateTime? UpdatedAt,
    IReadOnlyList<PaymentRefundDto> Refunds);

public sealed record GetPaymentByIdQuery(Guid PaymentId) : IRequest<PaymentDetailResponse?>;
