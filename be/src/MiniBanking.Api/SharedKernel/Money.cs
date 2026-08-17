namespace MiniBanking.SharedKernel;

public readonly record struct Money
{
    public long Amount { get; }
    public string Currency { get; }

    public Money(long amount, string currency = "VND")
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency = "VND") => new(0, currency);

    public static Money Vnd(long amount) => new(amount, "VND");

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(checked(Amount + other.Amount), Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(checked(Amount - other.Amount), Currency);
    }

    public bool IsNegative => Amount < 0;

    public bool IsZero => Amount == 0;

    public bool IsPositive => Amount > 0;

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot operate on different currencies: {Currency} and {other.Currency}.");
    }

    public override string ToString() => $"{Amount} {Currency}";
}
