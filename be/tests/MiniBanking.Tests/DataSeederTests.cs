using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Infrastructure.Security;
using Xunit;

namespace MiniBanking.Tests;

public class DataSeederTests
{
    private MiniBankingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MiniBankingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new MiniBankingDbContext(options);
    }

    [Fact]
    public async Task SeedAsync_ShouldSeedAdminUserAndMerchantsAndCustomers()
    {
        // Arrange
        using var context = CreateDbContext();

        // Act
        await DataSeeder.SeedAsync(context);

        // Assert
        var admin = await context.AdminUsers.FirstOrDefaultAsync(u => u.Email == "admin@minibanking.local");
        Assert.NotNull(admin);
        Assert.Equal("System Administrator", admin.FullName);
        Assert.Equal("Admin", admin.Role);
        Assert.True(admin.IsActive);
        Assert.True(PasswordHasher.Verify("Admin@123", admin.PasswordHash));

        var merchants = await context.Merchants.ToListAsync();
        Assert.Equal(4, merchants.Count);
        Assert.Contains(merchants, m => m.MerchantId == "MCH-ECOM-ALPHA");
        Assert.Contains(merchants, m => m.MerchantId == "MCH-TECH-BETA");
        Assert.Contains(merchants, m => m.MerchantId == "MCH-FASHION-HUB");
        Assert.Contains(merchants, m => m.MerchantId == "ecommerce-demo");

        var customers = await context.BankingCustomers.ToListAsync();
        Assert.Equal(5, customers.Count);

        var wallets = await context.WalletAccounts.ToListAsync();
        Assert.Equal(5, wallets.Count);

        var balances = await context.BalanceSnapshots.ToListAsync();
        Assert.Equal(5, balances.Count);

        var ledgerTxns = await context.LedgerTransactions.Include(t => t.Entries).ToListAsync();
        Assert.Equal(5, ledgerTxns.Count);
        Assert.All(ledgerTxns, txn => Assert.Equal(2, txn.Entries.Count));
    }

    [Fact]
    public async Task SeedAsync_ShouldBeIdempotent()
    {
        // Arrange
        using var context = CreateDbContext();

        // Act - Run seed twice
        await DataSeeder.SeedAsync(context);
        await DataSeeder.SeedAsync(context);

        // Assert - Counts should remain unchanged
        Assert.Equal(1, await context.AdminUsers.CountAsync());
        Assert.Equal(4, await context.Merchants.CountAsync());
        Assert.Equal(5, await context.BankingCustomers.CountAsync());
        Assert.Equal(5, await context.WalletAccounts.CountAsync());
        Assert.Equal(5, await context.BalanceSnapshots.CountAsync());
        Assert.Equal(5, await context.LedgerTransactions.CountAsync());
    }
}
