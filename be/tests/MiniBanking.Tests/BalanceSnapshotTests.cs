using MiniBanking.Modules.Accounts.Domain;
using MiniBanking.SharedKernel;

namespace MiniBanking.Tests;

/// <summary>
/// Unit tests for <see cref="BalanceSnapshot"/>.
/// </summary>
public class BalanceSnapshotTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (BankingCustomer customer, WalletAccount wallet) MakeWallet(string currency = "VND")
    {
        var customer = new BankingCustomer("Test User", "test@example.com", "0900000000");
        var wallet   = new WalletAccount(customer, "ACC-001", currency);
        return (customer, wallet);
    }

    private static BalanceSnapshot MakeSnapshot(long initialAmount = 100_000, string currency = "VND")
    {
        var (_, wallet) = MakeWallet(currency);
        return new BalanceSnapshot(wallet, new Money(initialAmount, currency));
    }

    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidArguments_SetsBalancesAndVersionToZero()
    {
        var snapshot = MakeSnapshot(50_000);

        Assert.Equal(50_000, snapshot.AvailableBalance);
        Assert.Equal(50_000, snapshot.LedgerBalance);
        Assert.Equal(0, snapshot.Version);
    }

    [Fact]
    public void Constructor_NullWallet_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BalanceSnapshot(null!, Money.Vnd(1_000)));
    }

    [Fact]
    public void Constructor_NegativeInitialBalance_ThrowsInvalidOperationException()
    {
        var (_, wallet) = MakeWallet();

        Assert.Throws<InvalidOperationException>(() =>
            new BalanceSnapshot(wallet, new Money(-1, "VND")));
    }

    [Fact]
    public void Constructor_CurrencyMismatch_ThrowsInvalidOperationException()
    {
        var (_, wallet) = MakeWallet("VND");

        Assert.Throws<InvalidOperationException>(() =>
            new BalanceSnapshot(wallet, new Money(1_000, "USD")));
    }

    [Fact]
    public void Constructor_ZeroInitialBalance_IsAllowed()
    {
        var snapshot = MakeSnapshot(0);

        Assert.Equal(0, snapshot.AvailableBalance);
    }

    // ── Credit ────────────────────────────────────────────────────────────────

    [Fact]
    public void Credit_IncreasesAvailableAndLedgerBalance()
    {
        var snapshot = MakeSnapshot(10_000);

        snapshot.Credit(Money.Vnd(5_000));

        Assert.Equal(15_000, snapshot.AvailableBalance);
        Assert.Equal(15_000, snapshot.LedgerBalance);
    }

    [Fact]
    public void Credit_IncrementsVersion()
    {
        var snapshot = MakeSnapshot(10_000);
        var versionBefore = snapshot.Version;

        snapshot.Credit(Money.Vnd(1_000));

        Assert.Equal(versionBefore + 1, snapshot.Version);
    }

    [Fact]
    public void Credit_ZeroAmount_ThrowsInvalidOperationException()
    {
        var snapshot = MakeSnapshot(10_000);

        Assert.Throws<InvalidOperationException>(() =>
            snapshot.Credit(Money.Zero("VND")));
    }

    [Fact]
    public void Credit_NegativeAmount_ThrowsInvalidOperationException()
    {
        var snapshot = MakeSnapshot(10_000);

        Assert.Throws<InvalidOperationException>(() =>
            snapshot.Credit(new Money(-1_000, "VND")));
    }

    [Fact]
    public void Credit_WrongCurrency_ThrowsInvalidOperationException()
    {
        var snapshot = MakeSnapshot(10_000, "VND");

        Assert.Throws<InvalidOperationException>(() =>
            snapshot.Credit(new Money(100, "USD")));
    }

    [Fact]
    public void Credit_SetsUpdatedAt()
    {
        var snapshot = MakeSnapshot(10_000);

        snapshot.Credit(Money.Vnd(1));

        Assert.NotNull(snapshot.UpdatedAt);
    }

    // ── Debit ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Debit_DecreasesAvailableAndLedgerBalance()
    {
        var snapshot = MakeSnapshot(100_000);

        snapshot.Debit(Money.Vnd(30_000));

        Assert.Equal(70_000, snapshot.AvailableBalance);
        Assert.Equal(70_000, snapshot.LedgerBalance);
    }

    [Fact]
    public void Debit_IncrementsVersion()
    {
        var snapshot = MakeSnapshot(10_000);
        var versionBefore = snapshot.Version;

        snapshot.Debit(Money.Vnd(1_000));

        Assert.Equal(versionBefore + 1, snapshot.Version);
    }

    [Fact]
    public void Debit_ExactBalance_ReducesBalanceToZero()
    {
        var snapshot = MakeSnapshot(50_000);

        snapshot.Debit(Money.Vnd(50_000));

        Assert.Equal(0, snapshot.AvailableBalance);
        Assert.Equal(0, snapshot.LedgerBalance);
    }

    [Fact]
    public void Debit_InsufficientFunds_ThrowsInvalidOperationException()
    {
        var snapshot = MakeSnapshot(10_000);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            snapshot.Debit(Money.Vnd(10_001)));

        Assert.Contains("Insufficient", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Debit_ZeroBalance_ThrowsInsufficientFunds()
    {
        var snapshot = MakeSnapshot(0);

        Assert.Throws<InvalidOperationException>(() =>
            snapshot.Debit(Money.Vnd(1)));
    }

    [Fact]
    public void Debit_ZeroAmount_ThrowsInvalidOperationException()
    {
        var snapshot = MakeSnapshot(10_000);

        Assert.Throws<InvalidOperationException>(() =>
            snapshot.Debit(Money.Zero("VND")));
    }

    [Fact]
    public void Debit_NegativeAmount_ThrowsInvalidOperationException()
    {
        var snapshot = MakeSnapshot(10_000);

        Assert.Throws<InvalidOperationException>(() =>
            snapshot.Debit(new Money(-500, "VND")));
    }

    [Fact]
    public void Debit_WrongCurrency_ThrowsInvalidOperationException()
    {
        var snapshot = MakeSnapshot(10_000, "VND");

        Assert.Throws<InvalidOperationException>(() =>
            snapshot.Debit(new Money(100, "USD")));
    }

    [Fact]
    public void Debit_NeverProducesNegativeBalance()
    {
        var snapshot = MakeSnapshot(5_000);

        // Any attempt beyond balance must throw; balance must never go negative.
        try { snapshot.Debit(Money.Vnd(5_001)); }
        catch (InvalidOperationException) { /* expected */ }

        Assert.True(snapshot.AvailableBalance >= 0,
            $"Balance went negative: {snapshot.AvailableBalance}");
    }

    // ── Available / Ledger projections ────────────────────────────────────────

    [Fact]
    public void Available_ReturnsMoneyWithCurrentBalance()
    {
        var snapshot = MakeSnapshot(25_000);

        Assert.Equal(new Money(25_000, "VND"), snapshot.Available);
    }

    [Fact]
    public void Ledger_ReturnsMoneyWithCurrentLedgerBalance()
    {
        var snapshot = MakeSnapshot(25_000);

        Assert.Equal(new Money(25_000, "VND"), snapshot.Ledger);
    }

    // ── Sequential credit + debit ─────────────────────────────────────────────

    [Fact]
    public void CreditThenDebit_BalancesAndVersionAreConsistent()
    {
        var snapshot = MakeSnapshot(10_000);

        snapshot.Credit(Money.Vnd(5_000));  // 15_000, v=1
        snapshot.Debit(Money.Vnd(8_000));   // 7_000,  v=2

        Assert.Equal(7_000, snapshot.AvailableBalance);
        Assert.Equal(7_000, snapshot.LedgerBalance);
        Assert.Equal(2, snapshot.Version);
    }
}
