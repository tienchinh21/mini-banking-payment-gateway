namespace MiniBanking.Modules.Accounts.Application.TopUpWallet;

public sealed record TopUpWalletResponse(
    string AccountNumber,
    long TopUpAmount,
    string Currency,
    long NewAvailableBalance,
    long NewLedgerBalance,
    Guid TransactionId);
