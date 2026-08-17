using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Accounts.Domain;

public class BankingCustomer : Entity
{
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;

    private BankingCustomer() { } // EF Core requires parameterless constructor

    public BankingCustomer(string fullName, string email, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));

        FullName = fullName;
        Email = email ?? string.Empty;
        PhoneNumber = phoneNumber ?? string.Empty;
    }
}
