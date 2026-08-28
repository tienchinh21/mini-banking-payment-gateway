using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MiniBanking.Infrastructure.Security;
using Xunit;

namespace MiniBanking.Tests;

public class AuthSecurityTests
{
    [Fact]
    public void PasswordHasher_ShouldHashAndVerifyCorrectly()
    {
        var password = "Admin@123";
        var hash = PasswordHasher.Hash(password);

        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.Contains(".", hash); // Salted PBKDF2 format
        Assert.True(PasswordHasher.Verify(password, hash));
        Assert.False(PasswordHasher.Verify("WrongPassword", hash));
    }

    [Fact]
    public void PasswordHasher_ShouldGenerateDifferentSaltForSamePassword()
    {
        var password = "Admin@123";
        var hash1 = PasswordHasher.Hash(password);
        var hash2 = PasswordHasher.Hash(password);

        Assert.NotEqual(hash1, hash2); // Salt ensures distinct hashes
        Assert.True(PasswordHasher.Verify(password, hash1));
        Assert.True(PasswordHasher.Verify(password, hash2));
    }

    [Fact]
    public void PasswordHasher_ShouldVerifyLegacySha256Hash()
    {
        // SHA-256("Admin@123") in hex lowercase
        var legacySha256Hash = "e86f78a8a3caf0b60d8e74e5942aa6d86dc150cd3c03338aef25b7d2d7e3acc7";
        Assert.True(PasswordHasher.Verify("Admin@123", legacySha256Hash));
        Assert.False(PasswordHasher.Verify("WrongPassword", legacySha256Hash));
    }

    [Fact]
    public void JwtTokenService_ShouldGenerateValidTokenWithClaims()
    {
        var options = new JwtOptions
        {
            Secret = "super-secret-key-that-is-long-enough-for-hmac-sha256",
            Issuer = "MiniBanking",
            Audience = "MiniBanking",
            ExpirationHours = 8
        };

        var service = new JwtTokenService(options);
        var tokenString = service.GenerateToken("user-123", "admin@minibanking.local", "Admin User", new[] { "Admin" });

        Assert.NotNull(tokenString);
        Assert.NotEmpty(tokenString);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.Equal("MiniBanking", token.Issuer);
        Assert.Contains("MiniBanking", token.Audiences);

        var subClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.NotNull(subClaim);
        Assert.Equal("user-123", subClaim.Value);

        var roleClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role");
        Assert.NotNull(roleClaim);
        Assert.Equal("Admin", roleClaim.Value);
    }
}
