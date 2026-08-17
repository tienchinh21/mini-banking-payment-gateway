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

    public void AddEntry(Guid accountId, string accountType, Money amount, bool isDebit)
    {
        var entry = new LedgerEntry(Id, accountId, accountType, amount, isDebit);
        _entries.Add(entry);
    }

    public void ValidateInvariant()
    {
        if (!_entries.Any())
            throw new InvalidOperationException("A ledger transaction must have at least one entry.");

        var currencies = _entries.Select(e => e.Currency).Distinct().ToList();
        if (currencies.Count > 1)
            throw new InvalidOperationException($"Ledger transaction entries must share the same currency. Found: {string.Join(", ", currencies)}.");

        var debitSum = _entries.Where(e => e.IsDebit).Sum(e => e.Amount);
        var creditSum = _entries.Where(e => !e.IsDebit).Sum(e => e.Amount);

        if (debitSum != creditSum)
            throw new InvalidOperationException($"Ledger transaction must be balanced. Debits={debitSum}, Credits={creditSum}.");
    }
}
