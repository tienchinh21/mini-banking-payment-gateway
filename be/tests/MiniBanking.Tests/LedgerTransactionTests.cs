using MiniBanking.Modules.Ledger.Domain;
using MiniBanking.SharedKernel;

namespace MiniBanking.Tests;

/// <summary>
/// Unit tests for <see cref="LedgerTransaction"/> and its
/// double-entry bookkeeping invariant.
/// </summary>
public class LedgerTransactionTests
{
    private static readonly Guid WalletId   = Guid.NewGuid();
    private static readonly Guid ClearingId = SystemAccountIds.PlatformClearing;

    private static LedgerTransaction MakeTransaction(string? referenceId = null)
        => new(
            referenceId ?? $"PAY-{Guid.NewGuid():N}",
            LedgerTransactionType.Payment,
            "Test payment");

    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        const string refId = "PAY-ABC123";
        var tx = new LedgerTransaction(refId, LedgerTransactionType.Payment, "desc");

        Assert.Equal(refId, tx.ReferenceId);
        Assert.Equal(LedgerTransactionType.Payment, tx.Type);
        Assert.Equal("desc", tx.Description);
        Assert.Empty(tx.Entries);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankReferenceId_ThrowsArgumentException(string? refId)
    {
        Assert.Throws<ArgumentException>(() =>
            new LedgerTransaction(refId!, LedgerTransactionType.Payment, "desc"));
    }

    [Fact]
    public void Constructor_NullDescription_StoredAsEmptyString()
    {
        var tx = new LedgerTransaction("REF-001", LedgerTransactionType.TopUp, null!);

        Assert.Equal(string.Empty, tx.Description);
    }

    // ── AddEntry ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddEntry_ValidEntry_IsIncludedInEntries()
    {
        var tx = MakeTransaction();

        tx.AddEntry(WalletId, "WalletAccount", Money.Vnd(10_000), isDebit: true);

        Assert.Single(tx.Entries);
    }

    [Fact]
    public void AddEntry_ZeroAmount_ThrowsArgumentException()
    {
        var tx = MakeTransaction();

        Assert.Throws<ArgumentException>(() =>
            tx.AddEntry(WalletId, "WalletAccount", Money.Zero("VND"), isDebit: true));
    }

    [Fact]
    public void AddEntry_NegativeAmount_ThrowsArgumentException()
    {
        var tx = MakeTransaction();

        Assert.Throws<ArgumentException>(() =>
            tx.AddEntry(WalletId, "WalletAccount", new Money(-1_000, "VND"), isDebit: true));
    }

    // ── ValidateInvariant ─────────────────────────────────────────────────────

    [Fact]
    public void ValidateInvariant_NoEntries_ThrowsInvalidOperationException()
    {
        var tx = MakeTransaction();

        var ex = Assert.Throws<InvalidOperationException>(() => tx.ValidateInvariant());
        Assert.Contains("at least one entry", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateInvariant_BalancedTransaction_DoesNotThrow()
    {
        var tx     = MakeTransaction();
        var amount = Money.Vnd(100_000);

        tx.AddEntry(WalletId,   "WalletAccount",   amount, isDebit: true);
        tx.AddEntry(ClearingId, "PlatformClearing", amount, isDebit: false);

        // Must not throw
        tx.ValidateInvariant();
    }

    [Fact]
    public void ValidateInvariant_UnbalancedDebitsGreaterThanCredits_ThrowsInvalidOperationException()
    {
        var tx = MakeTransaction();

        tx.AddEntry(WalletId,   "WalletAccount",   Money.Vnd(200_000), isDebit: true);
        tx.AddEntry(ClearingId, "PlatformClearing", Money.Vnd(100_000), isDebit: false);

        var ex = Assert.Throws<InvalidOperationException>(() => tx.ValidateInvariant());
        Assert.Contains("balanced", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("200000", ex.Message);
        Assert.Contains("100000", ex.Message);
    }

    [Fact]
    public void ValidateInvariant_UnbalancedCreditsGreaterThanDebits_ThrowsInvalidOperationException()
    {
        var tx = MakeTransaction();

        tx.AddEntry(WalletId,   "WalletAccount",   Money.Vnd(100_000), isDebit: true);
        tx.AddEntry(ClearingId, "PlatformClearing", Money.Vnd(200_000), isDebit: false);

        Assert.Throws<InvalidOperationException>(() => tx.ValidateInvariant());
    }

    [Fact]
    public void ValidateInvariant_MultiCurrencyEntries_ThrowsInvalidOperationException()
    {
        var tx = MakeTransaction();

        tx.AddEntry(WalletId,   "WalletAccount",   new Money(100_000, "VND"), isDebit: true);
        tx.AddEntry(ClearingId, "PlatformClearing", new Money(5,       "USD"), isDebit: false);

        var ex = Assert.Throws<InvalidOperationException>(() => tx.ValidateInvariant());
        Assert.Contains("currency", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateInvariant_SingleDebitEntry_ThrowsUnbalanced()
    {
        var tx = MakeTransaction();

        tx.AddEntry(WalletId, "WalletAccount", Money.Vnd(50_000), isDebit: true);

        Assert.Throws<InvalidOperationException>(() => tx.ValidateInvariant());
    }

    [Fact]
    public void ValidateInvariant_SingleCreditEntry_ThrowsUnbalanced()
    {
        var tx = MakeTransaction();

        tx.AddEntry(ClearingId, "PlatformClearing", Money.Vnd(50_000), isDebit: false);

        Assert.Throws<InvalidOperationException>(() => tx.ValidateInvariant());
    }

    // ── Multi-leg (split) transaction ─────────────────────────────────────────

    [Fact]
    public void ValidateInvariant_MultiLegBalancedTransaction_DoesNotThrow()
    {
        // 3-way split: one debit, two credits that sum to same amount
        var tx       = MakeTransaction();
        var acc1     = Guid.NewGuid();
        var acc2     = Guid.NewGuid();
        var debitAcc = Guid.NewGuid();

        tx.AddEntry(debitAcc, "Source",  Money.Vnd(90_000), isDebit: true);
        tx.AddEntry(acc1,     "Dest1",   Money.Vnd(60_000), isDebit: false);
        tx.AddEntry(acc2,     "Dest2",   Money.Vnd(30_000), isDebit: false);

        // Must not throw
        tx.ValidateInvariant();
    }

    [Fact]
    public void ValidateInvariant_MultiLegUnbalanced_ThrowsInvalidOperationException()
    {
        var tx       = MakeTransaction();
        var debitAcc = Guid.NewGuid();
        var creditAcc = Guid.NewGuid();

        tx.AddEntry(debitAcc,  "Source", Money.Vnd(90_000), isDebit: true);
        tx.AddEntry(creditAcc, "Dest",   Money.Vnd(30_000), isDebit: false);

        Assert.Throws<InvalidOperationException>(() => tx.ValidateInvariant());
    }

    // ── Default status ────────────────────────────────────────────────────────

    [Fact]
    public void NewTransaction_DefaultStatusIsCompleted()
    {
        var tx = MakeTransaction();

        Assert.Equal(LedgerTransactionStatus.Completed, tx.Status);
    }

    // ── Entries are read-only externally ──────────────────────────────────────

    [Fact]
    public void Entries_CannotBeCastToMutableList()
    {
        var tx = MakeTransaction();
        tx.AddEntry(WalletId, "WalletAccount", Money.Vnd(1_000), isDebit: true);

        Assert.IsNotType<List<LedgerEntry>>(tx.Entries);
    }
}
