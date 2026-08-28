using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Accounts.Application.Services;
using MiniBanking.Modules.Accounts.Domain;
using MiniBanking.Modules.Ledger.Application.Services;
using MiniBanking.Modules.Ledger.Domain;
using MiniBanking.Modules.Payments.Application;
using MiniBanking.Modules.Payments.Application.Services;
using MiniBanking.SharedKernel;
using Xunit;

namespace MiniBanking.Tests;

public class CleanArchitectureIntegrationTests
{
    private MiniBankingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<MiniBankingDbContext>()
            .UseInMemoryDatabase(databaseName: $"MiniBankingTestDb_{Guid.NewGuid():N}")
            .Options;

        return new MiniBankingDbContext(options);
    }

    [Fact]
    public async Task LedgerPostingService_ShouldEnforceBalancedInvariant_ForDirectDebit()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var ledgerService = new LedgerPostingService(db);
        var walletId = Guid.NewGuid();
        var amount = new Money(500000, "VND");

        // Act
        var tx = await ledgerService.PostPaymentAsync(walletId, amount, "Test Payment");
        await db.SaveChangesAsync();

        // Assert
        Assert.NotNull(tx);
        Assert.Equal(2, tx.Entries.Count);
        var debitSum = tx.Entries.Where(e => e.IsDebit).Sum(e => e.Amount);
        var creditSum = tx.Entries.Where(e => !e.IsDebit).Sum(e => e.Amount);
        Assert.Equal(debitSum, creditSum);
        Assert.Equal(500000, debitSum);
    }

    [Fact]
    public void LedgerTransaction_ShouldThrow_WhenEntriesAreUnbalanced()
    {
        // Arrange
        var tx = new LedgerTransaction("TEST-UNBALANCED", LedgerTransactionType.Payment, "Unbalanced test");
        tx.AddEntry(Guid.NewGuid(), "WalletAccount", new Money(100000, "VND"), isDebit: true);
        tx.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", new Money(80000, "VND"), isDebit: false);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => tx.ValidateInvariant());
        Assert.Contains("must be balanced", ex.Message);
    }

    [Fact]
    public async Task IdempotencyService_ShouldDetectDifferentPayload_WithSameKey()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var service = new IdempotencyService(db);
        var merchantId = "merch_test";
        var key = "order_123_key";

        // 1st request
        var (isCompleted1, _, _) = await service.CheckOrInitializeAsync<PaymentResponse>(
            merchantId, key, "POST", "/api/v1/merchant/payments", "hash_aaa");
        await db.SaveChangesAsync();
        Assert.False(isCompleted1);

        // 2nd request with different body hash -> conflict!
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await service.CheckOrInitializeAsync<PaymentResponse>(
                merchantId, key, "POST", "/api/v1/merchant/payments", "hash_bbb_tampered");
        });
    }

    [Fact]
    public void BalanceSnapshot_Debit_ShouldGuardAgainstNegativeBalance()
    {
        // Arrange
        var customer = new BankingCustomer("Nguyen Van A", "a@test.com", "0900000001");
        var wallet = new WalletAccount(customer, "ACC-01", "VND");
        var snapshot = new BalanceSnapshot(wallet, new Money(100000, "VND"));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => snapshot.Debit(new Money(150000, "VND")));
        Assert.Contains("Insufficient available balance", ex.Message);
    }
}
