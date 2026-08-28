namespace MiniBanking.Modules.Payments.Application.CreateRefund;

public sealed record CreateRefundRequest(
    string MerchantRefundId,
    Guid PaymentId,
    long Amount,
    string Currency,
    string Reason);
