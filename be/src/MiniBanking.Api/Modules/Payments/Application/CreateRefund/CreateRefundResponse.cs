namespace MiniBanking.Modules.Payments.Application.CreateRefund;

public sealed record CreateRefundResponse(
    Guid RefundId,
    string MerchantRefundId,
    Guid PaymentId,
    string Status,
    long Amount,
    string Currency,
    string? FailureCode);
