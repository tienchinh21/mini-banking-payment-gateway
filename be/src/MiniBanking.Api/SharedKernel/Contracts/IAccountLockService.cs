using MiniBanking.Modules.Accounts.Domain;

namespace MiniBanking.SharedKernel.Contracts;

public record AccountLockResult(
    bool IsSuccess,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    BalanceSnapshot? Balance = null);

public interface IAccountLockService
{
    /// <summary>
    /// Locks the wallet account balance row using SELECT ... FOR UPDATE and debits the amount safely.
    /// </summary>
    Task<AccountLockResult> LockAndDebitWalletAsync(
        Guid walletAccountId,
        Money amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks the wallet account balance row using SELECT ... FOR UPDATE and credits the amount safely.
    /// </summary>
    Task<AccountLockResult> LockAndCreditWalletAsync(
        Guid walletAccountId,
        Money amount,
        CancellationToken cancellationToken = default);
}
