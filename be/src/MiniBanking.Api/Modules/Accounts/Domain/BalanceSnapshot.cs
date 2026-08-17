using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Accounts.Domain;

public class BalanceSnapshot : Entity
{
    public Guid WalletAccountId { get; private set; }
    public long AvailableBalance { get; private set; }
    public long LedgerBalance { get; private set; }
    public string Currency { get; private set; } = "VND";
    public long Version { get; private set; }

    private WalletAccount? _walletAccount;
    public WalletAccount WalletAccount => _walletAccount ?? throw new InvalidOperationException("Wallet account was not loaded.");

    private BalanceSnapshot() { } // EF Core requires parameterless constructor

    public BalanceSnapshot(WalletAccount walletAccount, Money initialBalance)
    {
        if (walletAccount is null)
            throw new ArgumentNullException(nameof(walletAccount));

        if (walletAccount.Currency != initialBalance.Currency)
            throw new InvalidOperationException("Wallet currency and balance currency must match.");

        if (initialBalance.IsNegative)
            throw new InvalidOperationException("Initial balance cannot be negative.");

        WalletAccountId = walletAccount.Id;
        _walletAccount = walletAccount;
        AvailableBalance = initialBalance.Amount;
        LedgerBalance = initialBalance.Amount;
        Currency = initialBalance.Currency;
        Version = 0;
    }

    public Money Available => new(AvailableBalance, Currency);
    public Money Ledger => new(LedgerBalance, Currency);

    public void Credit(Money amount)
    {
        if (amount.Currency != Currency)
            throw new InvalidOperationException("Currency mismatch.");

        if (!amount.IsPositive)
            throw new InvalidOperationException("Credit amount must be positive.");

        AvailableBalance = checked(AvailableBalance + amount.Amount);
        LedgerBalance = checked(LedgerBalance + amount.Amount);
        Version++;
        MarkUpdated();
    }

    public void Debit(Money amount)
    {
        if (amount.Currency != Currency)
            throw new InvalidOperationException("Currency mismatch.");

        if (!amount.IsPositive)
            throw new InvalidOperationException("Debit amount must be positive.");

        if (AvailableBalance < amount.Amount)
            throw new InvalidOperationException("Insufficient available balance.");

        AvailableBalance = checked(AvailableBalance - amount.Amount);
        LedgerBalance = checked(LedgerBalance - amount.Amount);
        Version++;
        MarkUpdated();
    }
}
