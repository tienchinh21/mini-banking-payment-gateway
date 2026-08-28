namespace MiniBanking.Modules.Payments.Application.CreatePayment;

public sealed record CreatePaymentRequest(
    string MerchantOrderId,
    string WalletAccountId,
    long Amount,
    string Currency,
    string Description,
    string? CallbackUrl);
