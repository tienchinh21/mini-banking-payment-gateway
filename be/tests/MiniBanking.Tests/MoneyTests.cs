using MiniBanking.SharedKernel;

namespace MiniBanking.Tests;

/// <summary>
/// Unit tests for the <see cref="Money"/> value object.
/// </summary>
public class MoneyTests
{
    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidArguments_StoresAmountAndCurrency()
    {
        var money = new Money(10_000, "VND");

        Assert.Equal(10_000, money.Amount);
        Assert.Equal("VND", money.Currency);
    }

    [Fact]
    public void Constructor_NormalisesLowerCaseCurrencyToUpperCase()
    {
        var money = new Money(100, "vnd");

        Assert.Equal("VND", money.Currency);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankCurrency_ThrowsArgumentException(string? currency)
    {
        Assert.Throws<ArgumentException>(() => new Money(100, currency!));
    }

    [Fact]
    public void Zero_ReturnsMoneyWithZeroAmount()
    {
        var zero = Money.Zero("VND");

        Assert.Equal(0, zero.Amount);
        Assert.Equal("VND", zero.Currency);
    }

    [Fact]
    public void Vnd_FactoryMethod_ReturnsMoneWithVndCurrency()
    {
        var money = Money.Vnd(5_000);

        Assert.Equal(5_000, money.Amount);
        Assert.Equal("VND", money.Currency);
    }

    // ── Boolean flags ─────────────────────────────────────────────────────────

    [Fact]
    public void IsPositive_WhenAmountGreaterThanZero_ReturnsTrue()
        => Assert.True(new Money(1, "VND").IsPositive);

    [Fact]
    public void IsPositive_WhenAmountIsZero_ReturnsFalse()
        => Assert.False(Money.Zero("VND").IsPositive);

    [Fact]
    public void IsPositive_WhenAmountIsNegative_ReturnsFalse()
        => Assert.False(new Money(-1, "VND").IsPositive);

    [Fact]
    public void IsZero_WhenAmountIsZero_ReturnsTrue()
        => Assert.True(Money.Zero("VND").IsZero);

    [Fact]
    public void IsZero_WhenAmountIsNonZero_ReturnsFalse()
        => Assert.False(new Money(1, "VND").IsZero);

    [Fact]
    public void IsNegative_WhenAmountBelowZero_ReturnsTrue()
        => Assert.True(new Money(-500, "VND").IsNegative);

    [Fact]
    public void IsNegative_WhenAmountIsZero_ReturnsFalse()
        => Assert.False(Money.Zero("VND").IsNegative);

    [Fact]
    public void IsNegative_WhenAmountIsPositive_ReturnsFalse()
        => Assert.False(new Money(1, "VND").IsNegative);

    // ── Add ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_SameCurrency_ReturnsSumWithCorrectCurrency()
    {
        var a = new Money(3_000, "VND");
        var b = new Money(7_000, "VND");

        var result = a.Add(b);

        Assert.Equal(10_000, result.Amount);
        Assert.Equal("VND", result.Currency);
    }

    [Fact]
    public void Add_DifferentCurrencies_ThrowsInvalidOperationException()
    {
        var vnd = new Money(1_000, "VND");
        var usd = new Money(1, "USD");

        Assert.Throws<InvalidOperationException>(() => vnd.Add(usd));
    }

    [Fact]
    public void Add_Overflow_ThrowsOverflowException()
    {
        var max = new Money(long.MaxValue, "VND");
        var one = new Money(1, "VND");

        Assert.Throws<OverflowException>(() => max.Add(one));
    }

    // ── Subtract ──────────────────────────────────────────────────────────────

    [Fact]
    public void Subtract_SameCurrency_ReturnsDifferenceWithCorrectCurrency()
    {
        var a = new Money(10_000, "VND");
        var b = new Money(3_000, "VND");

        var result = a.Subtract(b);

        Assert.Equal(7_000, result.Amount);
        Assert.Equal("VND", result.Currency);
    }

    [Fact]
    public void Subtract_DifferentCurrencies_ThrowsInvalidOperationException()
    {
        var vnd = new Money(1_000, "VND");
        var usd = new Money(1, "USD");

        Assert.Throws<InvalidOperationException>(() => vnd.Subtract(usd));
    }

    [Fact]
    public void Subtract_ProducesNegativeAmount_AllowedByMoneyItself()
    {
        // Money itself does not enforce positivity on subtraction results;
        // that guard lives in BalanceSnapshot.Debit.
        var a = new Money(1, "VND");
        var b = new Money(5, "VND");

        var result = a.Subtract(b);

        Assert.Equal(-4, result.Amount);
        Assert.True(result.IsNegative);
    }

    [Fact]
    public void Subtract_Overflow_ThrowsOverflowException()
    {
        var min = new Money(long.MinValue, "VND");
        var one = new Money(1, "VND");

        Assert.Throws<OverflowException>(() => min.Subtract(one));
    }

    // ── Value equality ────────────────────────────────────────────────────────

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        var a = new Money(1_000, "VND");
        var b = new Money(1_000, "VND");

        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentAmount_AreNotEqual()
    {
        var a = new Money(1_000, "VND");
        var b = new Money(2_000, "VND");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Equality_DifferentCurrency_AreNotEqual()
    {
        var a = new Money(1_000, "VND");
        var b = new Money(1_000, "USD");

        Assert.NotEqual(a, b);
    }

    // ── ToString ──────────────────────────────────────────────────────────────

    [Fact]
    public void ToString_ReturnsAmountAndCurrencySeparatedBySpace()
    {
        var money = new Money(50_000, "VND");

        Assert.Equal("50000 VND", money.ToString());
    }
}
