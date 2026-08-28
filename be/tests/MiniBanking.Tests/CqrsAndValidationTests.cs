using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Infrastructure.Security;
using MiniBanking.Modules.Accounts.Application.Services;
using MiniBanking.Modules.Accounts.Application.TopUpWallet;
using MiniBanking.Modules.Accounts.Domain;
using MiniBanking.Modules.Ledger.Application.Services;
using MiniBanking.Modules.Payments.Application.CreatePayment;
using MiniBanking.SharedKernel;
using MiniBanking.SharedKernel.Behaviors;
using Xunit;

namespace MiniBanking.Tests;

public class CqrsAndValidationTests
{
    private MiniBankingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<MiniBankingDbContext>()
            .UseInMemoryDatabase(databaseName: $"MiniBankingCqrsTestDb_{Guid.NewGuid():N}")
            .Options;

        return new MiniBankingDbContext(options);
    }

    [Fact]
    public async Task TopUpWalletHandler_ShouldCreditBalanceAndPostLedger_WhenValid()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var customer = new BankingCustomer("Nguyen Van B", "b@test.com", "0900000002");
        var wallet = new WalletAccount(customer, "WA-TEST-001", "VND");
        var snapshot = new BalanceSnapshot(wallet, Money.Vnd(500_000));

        db.BankingCustomers.Add(customer);
        db.WalletAccounts.Add(wallet);
        db.BalanceSnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var accountLockService = new AccountLockService(db);
        var ledgerPostingService = new LedgerPostingService(db);
        var handler = new TopUpWalletHandler(db, accountLockService, ledgerPostingService);

        var command = new TopUpWalletCommand(new TopUpWalletRequest("WA-TEST-001", 300_000, "VND", "Top-up test"));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        await db.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("WA-TEST-001", result.AccountNumber);
        Assert.Equal(300_000, result.TopUpAmount);
        Assert.Equal(800_000, result.NewAvailableBalance);
        Assert.NotEqual(Guid.Empty, result.TransactionId);

        var ledgerTx = await db.LedgerTransactions.Include(t => t.Entries).FirstOrDefaultAsync(t => t.Id == result.TransactionId);
        Assert.NotNull(ledgerTx);
        Assert.Equal(2, ledgerTx.Entries.Count);
        Assert.Equal(300_000, ledgerTx.Entries.Where(e => e.IsDebit).Sum(e => e.Amount));
        Assert.Equal(300_000, ledgerTx.Entries.Where(e => !e.IsDebit).Sum(e => e.Amount));
    }

    [Fact]
    public async Task TopUpWalletHandler_ShouldThrow_WhenWalletNotFound()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var accountLockService = new AccountLockService(db);
        var ledgerPostingService = new LedgerPostingService(db);
        var handler = new TopUpWalletHandler(db, accountLockService, ledgerPostingService);

        var command = new TopUpWalletCommand(new TopUpWalletRequest("WA-NON-EXISTENT", 100_000, "VND", "Top-up"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Không tìm thấy ví tài khoản", ex.Message);
    }

    [Fact]
    public void CreatePaymentCommandValidator_ShouldDetectInvalidFields()
    {
        var validator = new CreatePaymentCommandValidator();
        var invalidCommand = new CreatePaymentCommand(
            "",
            "",
            "POST",
            "/payments",
            "{}",
            new CreatePaymentRequest("", "invalid-guid", -500, "", "Test payment", null));

        var result = validator.Validate(invalidCommand);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.MerchantOrderId");
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.WalletAccountId");
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Amount");
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Currency");
    }

    [Fact]
    public void TopUpWalletCommandValidator_ShouldDetectInvalidAmount()
    {
        var validator = new TopUpWalletCommandValidator();
        var invalidCommand = new TopUpWalletCommand(new TopUpWalletRequest("WA-123", 0, "VND", null));

        var result = validator.Validate(invalidCommand);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Request.Amount");
    }

    [Fact]
    public async Task ValidationBehavior_ShouldThrowValidationException_WhenInvalid()
    {
        var validator = new TopUpWalletCommandValidator();
        var behavior = new ValidationBehavior<TopUpWalletCommand, TopUpWalletResponse>(new[] { validator });

        var invalidCommand = new TopUpWalletCommand(new TopUpWalletRequest("", -10, "VND", null));

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(invalidCommand, () => Task.FromResult<TopUpWalletResponse>(null!), CancellationToken.None));
    }

    [Fact]
    public void WebhookHmacSigning_ShouldComputeConsistentSignature()
    {
        var secret = "merchant_super_secret_key";
        var payload = "{\"PaymentId\":\"abc\",\"Amount\":100000}";

        var sig1 = HmacSignatureService.ComputeHmac(payload, secret);
        var sig2 = HmacSignatureService.ComputeHmac(payload, secret);

        Assert.NotNull(sig1);
        Assert.NotEmpty(sig1);
        Assert.Equal(sig1, sig2);
    }
}
