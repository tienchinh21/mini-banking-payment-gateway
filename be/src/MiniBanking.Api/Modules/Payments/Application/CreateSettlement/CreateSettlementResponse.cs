namespace MiniBanking.Modules.Payments.Application.CreateSettlement;

public sealed record CreateSettlementResponse(
    Guid SettlementId,
    string BatchReference,
    string MerchantId,
    string Status,
    long Amount,
    string Currency,
    int PaymentCount);
