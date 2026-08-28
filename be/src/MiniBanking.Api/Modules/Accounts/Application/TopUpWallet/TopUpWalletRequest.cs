namespace MiniBanking.Modules.Accounts.Application.TopUpWallet;

public sealed record TopUpWalletRequest(
    string AccountNumber,
    long Amount,
    string Currency,
    string? Description);
