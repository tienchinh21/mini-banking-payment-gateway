using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Infrastructure.Security;
using MiniBanking.Modules.Accounts.Domain;
using MiniBanking.Modules.Admin.Application.Commands.AdminLogin;
using MiniBanking.Modules.Admin.Application.Queries.GetAdminProfile;
using MiniBanking.Modules.Admin.Application.Queries.GetDashboardStats;
using MiniBanking.Modules.Admin.Application.Queries.GetMerchantSettlementSummary;
using MiniBanking.Modules.Admin.Domain;
using MiniBanking.Modules.Merchants.Domain;
using MiniBanking.Modules.Payments.Domain;
using MiniBanking.SharedKernel;
using Xunit;

namespace MiniBanking.Tests;

public class AdminCqrsTests
{
    private MiniBankingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<MiniBankingDbContext>()
            .UseInMemoryDatabase(databaseName: $"MiniBankingAdminCqrs_{Guid.NewGuid():N}")
            .Options;

        return new MiniBankingDbContext(options);
    }

    private IJwtTokenService CreateJwtTokenService()
    {
        var options = new JwtOptions
        {
            Secret = "a-very-secret-test-key-of-adequate-length-for-hmac256",
            Issuer = "MiniBankingTest",
            Audience = "MiniBankingTest",
            ExpirationHours = 8
        };
        return new JwtTokenService(options);
    }

    [Fact]
    public async Task AdminLoginHandler_ShouldReturnTokenAndUser_WhenCredentialsValid()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var tokenService = CreateJwtTokenService();
        var admin = new AdminUser("admin@minibanking.local", "System Administrator", PasswordHasher.Hash("Admin@123"), "Admin");
        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync();

        var handler = new AdminLoginHandler(db, tokenService);
        var command = new AdminLoginCommand("admin@minibanking.local", "Admin@123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal(admin.Id, result.User.Id);
        Assert.Equal("admin@minibanking.local", result.User.Email);
        Assert.Equal("System Administrator", result.User.FullName);
        Assert.Equal("Admin", result.User.Role);
    }

    [Fact]
    public async Task AdminLoginHandler_ShouldReturnNull_WhenPasswordIsWrong()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var tokenService = CreateJwtTokenService();
        var admin = new AdminUser("admin@minibanking.local", "System Administrator", PasswordHasher.Hash("Admin@123"), "Admin");
        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync();

        var handler = new AdminLoginHandler(db, tokenService);
        var command = new AdminLoginCommand("admin@minibanking.local", "WrongPassword");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AdminLoginHandler_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var tokenService = CreateJwtTokenService();
        var handler = new AdminLoginHandler(db, tokenService);
        var command = new AdminLoginCommand("nonexistent@minibanking.local", "Admin@123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AdminLoginHandler_ShouldReturnNull_WhenUserIsInactive()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var tokenService = CreateJwtTokenService();
        var admin = new AdminUser("inactive@minibanking.local", "Inactive Admin", PasswordHasher.Hash("Admin@123"), "Admin");
        admin.Deactivate();
        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync();

        var handler = new AdminLoginHandler(db, tokenService);
        var command = new AdminLoginCommand("inactive@minibanking.local", "Admin@123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAdminProfileHandler_ShouldExtractClaimsCorrectly()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-guid-123"),
            new Claim(ClaimTypes.Email, "admin@minibanking.local"),
            new Claim(ClaimTypes.Name, "Admin Superuser"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var handler = new GetAdminProfileHandler();
        var query = new GetAdminProfileQuery(principal);

        // Act
        var profile = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(profile);
        Assert.Equal("admin-guid-123", profile.Id);
        Assert.Equal("admin@minibanking.local", profile.Email);
        Assert.Equal("Admin Superuser", profile.FullName);
        Assert.Equal("Admin", profile.Role);
    }

    [Fact]
    public async Task GetDashboardStatsHandler_ShouldComputeStatsAccurately()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();

        // 1. Customers and Wallets
        var customer1 = new BankingCustomer("User 1", "u1@test.com", "0900000001");
        var customer2 = new BankingCustomer("User 2", "u2@test.com", "0900000002");
        var wallet1 = new WalletAccount(customer1, "WA-001", "VND");
        var wallet2 = new WalletAccount(customer2, "WA-002", "VND");
        db.BankingCustomers.AddRange(customer1, customer2);
        db.WalletAccounts.AddRange(wallet1, wallet2);

        // 2. Merchants (1 active, 1 inactive)
        var merchant1 = new Merchant("merch-1", "Merchant 1", "k1", "s1");
        var merchant2 = new Merchant("merch-2", "Merchant 2", "k2", "s2");
        merchant2.Deactivate();
        db.Merchants.AddRange(merchant1, merchant2);

        // 3. Payments
        var p1 = new Payment("merch-1", "ORD-1", wallet1.Id, Money.Vnd(100_000), "Payment 1", null, "idemp-1");
        p1.MarkSucceeded(Guid.NewGuid());

        var p2 = new Payment("merch-1", "ORD-2", wallet1.Id, Money.Vnd(200_000), "Payment 2", null, "idemp-2");
        p2.MarkSucceeded(Guid.NewGuid());

        var p3 = new Payment("merch-1", "ORD-3", wallet2.Id, Money.Vnd(50_000), "Payment 3", null, "idemp-3");
        p3.MarkFailed("INSUFFICIENT_FUNDS");

        db.Payments.AddRange(p1, p2, p3);

        // 4. Refunds
        var r1 = new Refund("merch-1", "REF-1", p1.Id, Money.Vnd(30_000), "Customer requested", "idemp-ref-1");
        r1.MarkSucceeded(Guid.NewGuid());

        var r2 = new Refund("merch-1", "REF-2", p2.Id, Money.Vnd(20_000), "Pending refund", "idemp-ref-2");
        // Pending status

        db.Refunds.AddRange(r1, r2);
        await db.SaveChangesAsync();

        var handler = new GetDashboardStatsHandler(db);

        // Act
        var stats = await handler.Handle(new GetDashboardStatsQuery(), CancellationToken.None);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(2, stats.Wallets.Total);
        Assert.Equal(2, stats.Wallets.Customers);
        Assert.Equal(1, stats.Merchants.Total); // Only active merchants

        Assert.Equal(3, stats.Payments.Total);
        Assert.Equal(2, stats.Payments.Successful);
        Assert.Equal(1, stats.Payments.Failed);
        Assert.Equal(300_000L, stats.Payments.TotalVolume); // 100k + 200k
        Assert.Equal(300_000L, stats.Payments.TodayVolume);
        Assert.Equal(3, stats.Payments.TodayCount);

        Assert.Equal(1, stats.Refunds.TotalCount); // Only succeeded refunds
        Assert.Equal(30_000L, stats.Refunds.TotalAmount);

        Assert.Equal(3, stats.RecentPayments.Count);
    }

    [Fact]
    public async Task GetMerchantSettlementSummaryHandler_ShouldComputeSettlementSummary()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();

        var merchantA = new Merchant("merch-alpha", "Alpha Store", "k1", "s1");
        var merchantB = new Merchant("merch-beta", "Beta Shop", "k2", "s2");
        db.Merchants.AddRange(merchantA, merchantB);

        // Payments for Alpha: 500k succeeded, 100k failed
        var pA1 = new Payment("merch-alpha", "ORD-A1", Guid.NewGuid(), Money.Vnd(300_000), "Order A1", null, "idemp-a1");
        pA1.MarkSucceeded(Guid.NewGuid());

        var pA2 = new Payment("merch-alpha", "ORD-A2", Guid.NewGuid(), Money.Vnd(200_000), "Order A2", null, "idemp-a2");
        pA2.MarkSucceeded(Guid.NewGuid());

        var pA3 = new Payment("merch-alpha", "ORD-A3", Guid.NewGuid(), Money.Vnd(100_000), "Order A3 failed", null, "idemp-a3");
        pA3.MarkFailed("FAILED");

        // Payments for Beta: 400k succeeded
        var pB1 = new Payment("merch-beta", "ORD-B1", Guid.NewGuid(), Money.Vnd(400_000), "Order B1", null, "idemp-b1");
        pB1.MarkSucceeded(Guid.NewGuid());

        db.Payments.AddRange(pA1, pA2, pA3, pB1);

        // Refund for Alpha: 50k succeeded
        var rA1 = new Refund("merch-alpha", "REF-A1", pA1.Id, Money.Vnd(50_000), "Refund 1", "idemp-ra1");
        rA1.MarkSucceeded(Guid.NewGuid());
        db.Refunds.Add(rA1);

        // Settlement for Alpha: 200k completed
        var sA1 = new Settlement("merch-alpha", "BATCH-001", Money.Vnd(200_000), 1);
        sA1.MarkCompleted(Guid.NewGuid());
        db.Settlements.Add(sA1);

        await db.SaveChangesAsync();

        var handler = new GetMerchantSettlementSummaryHandler(db);

        // Act 1: All merchants summary
        var allSummary = await handler.Handle(new GetMerchantSettlementSummaryQuery(), CancellationToken.None);

        // Assert 1
        Assert.NotNull(allSummary);
        Assert.Equal(2, allSummary.TotalMerchants);
        Assert.Equal(900_000L, allSummary.TotalGrossAmount); // 500k + 400k
        Assert.Equal(50_000L, allSummary.TotalRefundAmount); // 50k
        Assert.Equal(200_000L, allSummary.TotalSettledAmount); // 200k
        // Alpha pending = 500k - 50k - 200k = 250k. Beta pending = 400k - 0 - 0 = 400k. Total pending = 650k.
        Assert.Equal(650_000L, allSummary.TotalPendingSettlementAmount);
        Assert.Equal(2, allSummary.Items.Count);

        var alphaItem = allSummary.Items.First(x => x.MerchantId == "merch-alpha");
        Assert.Equal("Alpha Store", alphaItem.MerchantName);
        Assert.Equal(2, alphaItem.TotalPayments);
        Assert.Equal(500_000L, alphaItem.TotalPaymentAmount);
        Assert.Equal(1, alphaItem.TotalRefunds);
        Assert.Equal(50_000L, alphaItem.TotalRefundAmount);
        Assert.Equal(1, alphaItem.TotalSettlements);
        Assert.Equal(200_000L, alphaItem.TotalSettledAmount);
        Assert.Equal(250_000L, alphaItem.PendingSettlementAmount);
        Assert.NotNull(alphaItem.LastSettlementDate);

        var betaItem = allSummary.Items.First(x => x.MerchantId == "merch-beta");
        Assert.Equal("Beta Shop", betaItem.MerchantName);
        Assert.Equal(1, betaItem.TotalPayments);
        Assert.Equal(400_000L, betaItem.TotalPaymentAmount);
        Assert.Equal(0, betaItem.TotalRefunds);
        Assert.Equal(0L, betaItem.TotalRefundAmount);
        Assert.Equal(0, betaItem.TotalSettlements);
        Assert.Equal(0L, betaItem.TotalSettledAmount);
        Assert.Equal(400_000L, betaItem.PendingSettlementAmount);
        Assert.Null(betaItem.LastSettlementDate);

        // Act 2: Single merchant summary (filtered by MerchantId)
        var alphaOnlySummary = await handler.Handle(
            new GetMerchantSettlementSummaryQuery("merch-alpha"),
            CancellationToken.None);

        // Assert 2
        Assert.NotNull(alphaOnlySummary);
        Assert.Equal(1, alphaOnlySummary.TotalMerchants);
        Assert.Single(alphaOnlySummary.Items);
        Assert.Equal("merch-alpha", alphaOnlySummary.Items[0].MerchantId);
        Assert.Equal(500_000L, alphaOnlySummary.TotalGrossAmount);
        Assert.Equal(50_000L, alphaOnlySummary.TotalRefundAmount);
        Assert.Equal(200_000L, alphaOnlySummary.TotalSettledAmount);
        Assert.Equal(250_000L, alphaOnlySummary.TotalPendingSettlementAmount);
    }
}
