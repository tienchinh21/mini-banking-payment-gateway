using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Accounts.Domain;
using MiniBanking.SharedKernel;
using MiniBanking.SharedKernel.Contracts;

namespace MiniBanking.Modules.Accounts.Application.Services;

public class AccountLockService : IAccountLockService
{
    private readonly MiniBankingDbContext _dbContext;

    public AccountLockService(MiniBankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountLockResult> LockAndDebitWalletAsync(
        Guid walletAccountId,
        Money amount,
        CancellationToken cancellationToken = default)
    {
        // Issue SELECT ... FOR UPDATE to lock the balance snapshot row exclusively
        var balance = await _dbContext.BalanceSnapshots
            .FromSqlInterpolated($"SELECT * FROM public.balance_snapshots WHERE \"WalletAccountId\" = {walletAccountId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);

        if (balance is null)
        {
            return new AccountLockResult(false, "WALLET_NOT_FOUND", "Wallet account does not exist.");
        }

        if (balance.Currency != amount.Currency)
        {
            return new AccountLockResult(false, "CURRENCY_MISMATCH", $"Wallet currency is {balance.Currency} but requested {amount.Currency}.");
        }

        if (balance.AvailableBalance < amount.Amount)
        {
            return new AccountLockResult(false, "INSUFFICIENT_FUNDS", "Wallet available balance is not enough for this payment.", balance);
        }

        try
        {
            balance.Debit(amount);
            return new AccountLockResult(true, null, null, balance);
        }
        catch (InvalidOperationException ex)
        {
            return new AccountLockResult(false, "DEBIT_FAILED", ex.Message, balance);
        }
    }

    public async Task<AccountLockResult> LockAndCreditWalletAsync(
        Guid walletAccountId,
        Money amount,
        CancellationToken cancellationToken = default)
    {
        var balance = await _dbContext.BalanceSnapshots
            .FromSqlInterpolated($"SELECT * FROM public.balance_snapshots WHERE \"WalletAccountId\" = {walletAccountId} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);

        if (balance is null)
        {
            return new AccountLockResult(false, "WALLET_NOT_FOUND", "Wallet account does not exist.");
        }

        if (balance.Currency != amount.Currency)
        {
            return new AccountLockResult(false, "CURRENCY_MISMATCH", $"Wallet currency is {balance.Currency} but requested {amount.Currency}.");
        }

        try
        {
            balance.Credit(amount);
            return new AccountLockResult(true, null, null, balance);
        }
        catch (InvalidOperationException ex)
        {
            return new AccountLockResult(false, "CREDIT_FAILED", ex.Message, balance);
        }
    }
}
