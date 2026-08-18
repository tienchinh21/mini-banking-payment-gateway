using System.Security.Cryptography;
using System.Text;

namespace MiniBanking.Infrastructure.Security;

public static class HmacSignatureService
{
    public static string ComputeSignature(
        string method,
        string path,
        string bodyHash,
        string timestamp,
        string nonce,
        string idempotencyKey,
        string secret)
    {
        var payload = $"{method.ToUpperInvariant()}|{path}|{bodyHash}|{timestamp}|{nonce}|{idempotencyKey}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeBodyHash(string body)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static bool VerifySignature(
        string method,
        string path,
        string body,
        string timestamp,
        string nonce,
        string idempotencyKey,
        string providedSignature,
        string secret)
    {
        var bodyHash = ComputeBodyHash(body);
        var expectedSignature = ComputeSignature(method, path, bodyHash, timestamp, nonce, idempotencyKey, secret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(providedSignature));
    }
}
