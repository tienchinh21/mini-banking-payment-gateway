namespace MiniBanking.Modules.Payments.Application.CreateSettlement;

public sealed record CreateSettlementRequest(
    string MerchantId,
    string BatchReference);
