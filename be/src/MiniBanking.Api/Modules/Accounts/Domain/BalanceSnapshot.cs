using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Accounts.Domain;

public class BalanceSnapshot : Entity
{
    public Guid WalletAccountId { get; private set; }
    public long AvailableBalance { get; private set; }
    public long LedgerBalance { get; private set; }
    public string Currency { get; private set; } = "VND";

    /// <summary>
    /// Monotonically increasing version used for optimistic concurrency.
    /// Incremented on every Credit/Debit; a stale row will violate the
    /// DB unique constraint or EF concurrency token before any balance
    /// mutation is committed.
    /// </summary>
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

    /// <summary>
    /// Credits both the available and ledger balance.  Amount must be
    /// positive and share the same currency as this snapshot.
    /// </summary>
    public void Credit(Money amount)
    {
        if (amount.Currency != Currency)
            throw new InvalidOperationException($"Currency mismatch: snapshot is {Currency}, amount is {amount.Currency}.");

        if (!amount.IsPositive)
            throw new InvalidOperationException("Credit amount must be positive.");

        AvailableBalance = checked(AvailableBalance + amount.Amount);
        LedgerBalance = checked(LedgerBalance + amount.Amount);
        Version++;
        MarkUpdated();
    }

    /// <summary>
    /// Debits both the available and ledger balance.  Throws
    /// <see cref="InvalidOperationException"/> when the available balance
    /// is insufficient, preventing a negative balance.
    /// </summary>
    /// <remarks>
    /// Concurrency safety: callers are expected to hold a database-level
    /// row lock (e.g. SELECT … FOR UPDATE) before invoking this method
    /// so that no two concurrent transactions can read stale balances and
    /// both pass the balance check.  The <see cref="Version"/> counter is
    /// also mapped as an EF Core concurrency token so that any out-of-order
    /// commits surface as a <c>DbUpdateConcurrencyException</c>.
    /// </remarks>
    public void Debit(Money amount)
    {
        if (amount.Currency != Currency)
            throw new InvalidOperationException($"Currency mismatch: snapshot is {Currency}, amount is {amount.Currency}.");

        if (!amount.IsPositive)
            throw new InvalidOperationException("Debit amount must be positive.");

        if (AvailableBalance < amount.Amount)
            throw new InvalidOperationException(
                $"Insufficient available balance. Available: {AvailableBalance} {Currency}, Requested: {amount.Amount} {Currency}.");

        AvailableBalance = checked(AvailableBalance - amount.Amount);
        LedgerBalance = checked(LedgerBalance - amount.Amount);
        Version++;
        MarkUpdated();
    }
}
