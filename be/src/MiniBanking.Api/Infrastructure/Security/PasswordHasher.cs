using System.Security.Cryptography;
using System.Text;

namespace MiniBanking.Infrastructure.Security;

public static class PasswordHasher
{
    public static string Hash(string password)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool Verify(string password, string hash)
    {
        return Hash(password) == hash;
    }
}
