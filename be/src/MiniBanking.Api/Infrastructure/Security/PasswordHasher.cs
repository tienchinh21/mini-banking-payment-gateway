using System.Security.Cryptography;
using System.Text;

namespace MiniBanking.Infrastructure.Security;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DefaultIterations = 100_000;

    /// <summary>
    /// Hashes password using PBKDF2 with SHA-256 and a cryptographically secure random salt.
    /// Format: {iterations}.{salt_hex}.{hash_hex}
    /// </summary>
    public static string Hash(string password, int iterations = DefaultIterations)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return $"{iterations}.{Convert.ToHexString(salt).ToLowerInvariant()}.{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    /// <summary>
    /// Verifies password against PBKDF2 salted hash or legacy SHA-256 hash.
    /// </summary>
    public static bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;

        var parts = hash.Split('.');
        if (parts.Length == 3 && int.TryParse(parts[0], out var iterations))
        {
            try
            {
                var salt = Convert.FromHexString(parts[1]);
                var expectedHash = Convert.FromHexString(parts[2]);

                var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(password),
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedHash.Length);

                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch
            {
                return false;
            }
        }

        // Legacy SHA-256 fallback
        using var sha256 = SHA256.Create();
        var legacyHashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        var legacyHash = Convert.ToHexString(legacyHashBytes).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(legacyHash),
            Encoding.UTF8.GetBytes(hash));
    }
}
