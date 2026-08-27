using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Ledger.Domain;

public class LedgerTransaction : Entity
{
    public string ReferenceId { get; private set; } = string.Empty;
    public LedgerTransactionType Type { get; private set; }
    public LedgerTransactionStatus Status { get; private set; } = LedgerTransactionStatus.Completed;
    public string Description { get; private set; } = string.Empty;

    private List<LedgerEntry> _entries = new();
    public IReadOnlyCollection<LedgerEntry> Entries => _entries.AsReadOnly();

    private LedgerTransaction() { } // EF Core requires parameterless constructor

    public LedgerTransaction(string referenceId, LedgerTransactionType type, string description)
    {
        if (string.IsNullOrWhiteSpace(referenceId))
            throw new ArgumentException("Reference id is required.", nameof(referenceId));

        ReferenceId = referenceId;
        Type = type;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Adds a double-entry bookkeeping line to this transaction.
    /// </summary>
    /// <param name="accountId">The account receiving or giving the amount.</param>
    /// <param name="accountType">Human-readable account type tag.</param>
    /// <param name="amount">Monetary amount – must be positive.</param>
    /// <param name="isDebit">True for debit (money out of account), false for credit.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="amount"/> is zero or negative.</exception>
    public void AddEntry(Guid accountId, string accountType, Money amount, bool isDebit)
    {
        if (!amount.IsPositive)
            throw new ArgumentException("Ledger entry amount must be positive.", nameof(amount));

        var entry = new LedgerEntry(Id, accountId, accountType, amount, isDebit);
        _entries.Add(entry);
    }

    /// <summary>
    /// Validates the double-entry bookkeeping invariant:
    /// sum of all debit amounts must equal sum of all credit amounts,
    /// and all entries must share a single currency.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when there are no entries, when entries span multiple
    /// currencies, or when debits and credits do not balance.
    /// </exception>
    public void ValidateInvariant()
    {
        if (!_entries.Any())
            throw new InvalidOperationException("A ledger transaction must have at least one entry.");

        var currencies = _entries.Select(e => e.Currency).Distinct().ToList();
        if (currencies.Count > 1)
            throw new InvalidOperationException(
                $"Ledger transaction entries must share the same currency. Found: {string.Join(", ", currencies)}.");

        var debitSum  = _entries.Where(e =>  e.IsDebit).Sum(e => e.Amount);
        var creditSum = _entries.Where(e => !e.IsDebit).Sum(e => e.Amount);

        if (debitSum != creditSum)
            throw new InvalidOperationException(
                $"Ledger transaction must be balanced. Debits={debitSum}, Credits={creditSum}.");
    }
}
