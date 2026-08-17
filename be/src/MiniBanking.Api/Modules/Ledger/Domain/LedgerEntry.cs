using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Ledger.Domain;

public class LedgerEntry : Entity
{
    public Guid LedgerTransactionId { get; private set; }
    public Guid AccountId { get; private set; }
    public string AccountType { get; private set; } = string.Empty;
    public long Amount { get; private set; }
    public string Currency { get; private set; } = "VND";
    public bool IsDebit { get; private set; }
    public int Sequence { get; private set; }

    public LedgerTransaction LedgerTransaction { get; private set; } = null!;

    private LedgerEntry() { } // EF Core requires parameterless constructor

    public LedgerEntry(Guid ledgerTransactionId, Guid accountId, string accountType, Money amount, bool isDebit, int sequence = 0)
    {
        if (ledgerTransactionId == Guid.Empty)
            throw new ArgumentException("Ledger transaction id is required.", nameof(ledgerTransactionId));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        if (string.IsNullOrWhiteSpace(accountType))
            throw new ArgumentException("Account type is required.", nameof(accountType));

        if (amount.IsNegative)
            throw new ArgumentException("Ledger entry amount cannot be negative.", nameof(amount));

        LedgerTransactionId = ledgerTransactionId;
        AccountId = accountId;
        AccountType = accountType;
        Amount = amount.Amount;
        Currency = amount.Currency;
        IsDebit = isDebit;
        Sequence = sequence;
    }
}
