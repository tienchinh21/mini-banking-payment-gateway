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
        Assert.True(PasswordHasher.Verify(password, hash));
        Assert.False(PasswordHasher.Verify("WrongPassword", hash));
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
