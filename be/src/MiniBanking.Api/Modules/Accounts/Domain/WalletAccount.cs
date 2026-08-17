using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Accounts.Domain;

public class WalletAccount : Entity
{
    public Guid CustomerId { get; private set; }
    public string AccountNumber { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "VND";

    private BankingCustomer? _customer;
    public BankingCustomer Customer => _customer ?? throw new InvalidOperationException("Customer was not loaded.");

    private WalletAccount() { } // EF Core requires parameterless constructor

    public WalletAccount(BankingCustomer customer, string accountNumber, string currency = "VND")
    {
        if (customer is null)
            throw new ArgumentNullException(nameof(customer));

        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException("Account number is required.", nameof(accountNumber));

        CustomerId = customer.Id;
        _customer = customer;
        AccountNumber = accountNumber;
        Currency = string.IsNullOrWhiteSpace(currency) ? "VND" : currency.ToUpperInvariant();
    }
}
