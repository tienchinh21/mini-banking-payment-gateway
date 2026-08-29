using MiniBanking.SharedKernel;

namespace MiniBanking.Modules.Admin.Domain;

public class AdminUser : Entity
{
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = "Admin";
    public bool IsActive { get; private set; } = true;

    private AdminUser() { } // EF Core requires parameterless constructor

    public AdminUser(string email, string fullName, string passwordHash, string role = "Admin")
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        Email = email;
        FullName = fullName;
        PasswordHash = passwordHash;
        Role = role;
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }
}
