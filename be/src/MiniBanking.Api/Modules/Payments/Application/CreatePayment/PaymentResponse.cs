namespace MiniBanking.Modules.Payments.Application.CreatePayment;

public sealed record PaymentResponse(
    Guid PaymentId,
    string MerchantOrderId,
    string Status,
    long Amount,
    string Currency,
    string? FailureCode = null);
