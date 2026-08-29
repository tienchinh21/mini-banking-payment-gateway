using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Accounts.Application.Commands.FreezeWallet;
using MiniBanking.Modules.Accounts.Application.Queries.GetWalletBalance;
using MiniBanking.Modules.Accounts.Application.Queries.GetWalletBalanceByNumber;
using MiniBanking.Modules.Accounts.Application.Queries.GetWalletDetail;
using MiniBanking.Modules.Accounts.Application.Queries.GetWalletLedger;
using MiniBanking.Modules.Accounts.Application.Queries.GetWallets;
using MiniBanking.Modules.Accounts.Domain;
using MiniBanking.Modules.Ledger.Domain;
using MiniBanking.SharedKernel;
using Xunit;

namespace MiniBanking.Tests;

public class AccountCqrsTests
{
    private MiniBankingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<MiniBankingDbContext>()
            .UseInMemoryDatabase(databaseName: $"MiniBankingAccountCqrs_{Guid.NewGuid():N}")
            .Options;

        return new MiniBankingDbContext(options);
    }

    [Fact]
    public async Task GetWalletBalanceHandler_ShouldReturnBalance_WhenExists()
    {
        using var db = CreateInMemoryDbContext();
        var customer = new BankingCustomer("Alice Wonderland", "alice@example.com", "0911222333");
        var wallet = new WalletAccount(customer, "ACC-ALICE-01", "VND");
        var snapshot = new BalanceSnapshot(wallet, Money.Vnd(1_000_000));

        db.BankingCustomers.Add(customer);
        db.WalletAccounts.Add(wallet);
        db.BalanceSnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var handler = new GetWalletBalanceHandler(db);
        var query = new GetWalletBalanceQuery(wallet.Id);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(wallet.Id, result.WalletAccountId);
        Assert.Equal(1_000_000, result.AvailableBalance);
        Assert.Equal(1_000_000, result.LedgerBalance);
        Assert.Equal("VND", result.Currency);
    }

    [Fact]
    public async Task GetWalletBalanceHandler_ShouldReturnNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var handler = new GetWalletBalanceHandler(db);
        var query = new GetWalletBalanceQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetWalletsHandler_ShouldReturnPaginatedAndFilteredList()
    {
        using var db = CreateInMemoryDbContext();
        var cust1 = new BankingCustomer("Bob Builder", "bob@example.com", "0900000001");
        var cust2 = new BankingCustomer("Charlie Chaplin", "charlie@example.com", "0900000002");
        var wallet1 = new WalletAccount(cust1, "ACC-BOB-01", "VND");
        var wallet2 = new WalletAccount(cust2, "ACC-CHARLIE-01", "USD");
        var snap1 = new BalanceSnapshot(wallet1, Money.Vnd(200_000));
        var snap2 = new BalanceSnapshot(wallet2, new Money(500, "USD"));

        db.BankingCustomers.AddRange(cust1, cust2);
        db.WalletAccounts.AddRange(wallet1, wallet2);
        db.BalanceSnapshots.AddRange(snap1, snap2);
        await db.SaveChangesAsync();

        var handler = new GetWalletsHandler(db);

        // Filter by currency VND
        var resultVnd = await handler.Handle(new GetWalletsQuery(1, 10, null, "VND"), CancellationToken.None);
        Assert.Equal(1, resultVnd.TotalCount);
        Assert.Single(resultVnd.Items);
        Assert.Equal("ACC-BOB-01", resultVnd.Items[0].AccountNumber);
        Assert.Equal(200_000, resultVnd.Items[0].AvailableBalance);

        // Search by name
        var resultSearch = await handler.Handle(new GetWalletsQuery(1, 10, "Charlie", null), CancellationToken.None);
        Assert.Equal(1, resultSearch.TotalCount);
        Assert.Equal("ACC-CHARLIE-01", resultSearch.Items[0].AccountNumber);
        Assert.Equal(500, resultSearch.Items[0].AvailableBalance);
    }

    [Fact]
    public async Task GetWalletDetailHandler_ShouldLookupByGuidOrAccountNumber_AndIncludeLedgerEntries()
    {
        using var db = CreateInMemoryDbContext();
        var customer = new BankingCustomer("David Copperfield", "david@example.com", "0988776655");
        var wallet = new WalletAccount(customer, "ACC-DAVID-01", "VND");
        var snapshot = new BalanceSnapshot(wallet, Money.Vnd(750_000));

        var tx = new LedgerTransaction("REF-001", LedgerTransactionType.Payment, "Payment test");
        var entry = new LedgerEntry(tx.Id, wallet.Id, "WalletAccount", Money.Vnd(50_000), isDebit: true);

        db.BankingCustomers.Add(customer);
        db.WalletAccounts.Add(wallet);
        db.BalanceSnapshots.Add(snapshot);
        db.LedgerTransactions.Add(tx);
        db.LedgerEntries.Add(entry);
        await db.SaveChangesAsync();

        var handler = new GetWalletDetailHandler(db);

        // By Guid
        var resultByGuid = await handler.Handle(new GetWalletDetailQuery(wallet.Id.ToString()), CancellationToken.None);
        Assert.NotNull(resultByGuid);
        Assert.Equal(wallet.Id, resultByGuid.WalletId);
        Assert.Equal("David Copperfield", resultByGuid.CustomerName);
        Assert.Equal(750_000, resultByGuid.AvailableBalance);
        Assert.Single(resultByGuid.RecentTransactions);
        Assert.Equal(entry.Id, resultByGuid.RecentTransactions[0].Id);

        // By AccountNumber
        var resultByAcc = await handler.Handle(new GetWalletDetailQuery("ACC-DAVID-01"), CancellationToken.None);
        Assert.NotNull(resultByAcc);
        Assert.Equal(wallet.Id, resultByAcc.WalletId);

        // Not Found
        var resultNotFound = await handler.Handle(new GetWalletDetailQuery("NON-EXISTENT"), CancellationToken.None);
        Assert.Null(resultNotFound);
    }

    [Fact]
    public async Task GetWalletBalanceByNumberHandler_ShouldReturnBalance_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var customer = new BankingCustomer("Eve Adams", "eve@example.com", "0933333333");
        var wallet = new WalletAccount(customer, "ACC-EVE-01", "VND");
        var snapshot = new BalanceSnapshot(wallet, Money.Vnd(120_000));

        db.BankingCustomers.Add(customer);
        db.WalletAccounts.Add(wallet);
        db.BalanceSnapshots.Add(snapshot);
        await db.SaveChangesAsync();

        var handler = new GetWalletBalanceByNumberHandler(db);
        var result = await handler.Handle(new GetWalletBalanceByNumberQuery("ACC-EVE-01"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("ACC-EVE-01", result.AccountNumber);
        Assert.Equal("VND", result.Currency);
        Assert.Equal(120_000, result.AvailableBalance);
        Assert.Equal(120_000, result.LedgerBalance);

        var notFound = await handler.Handle(new GetWalletBalanceByNumberQuery("ACC-UNKNOWN"), CancellationToken.None);
        Assert.Null(notFound);
    }

    [Fact]
    public async Task GetWalletLedgerHandler_ShouldReturnRecentEntries_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var customer = new BankingCustomer("Frank Ocean", "frank@example.com", "0944444444");
        var wallet = new WalletAccount(customer, "ACC-FRANK-01", "VND");

        var tx = new LedgerTransaction("REF-TOPUP", LedgerTransactionType.TopUp, "Top up test");
        var entry1 = new LedgerEntry(tx.Id, wallet.Id, "WalletAccount", Money.Vnd(100_000), isDebit: false);
        var entry2 = new LedgerEntry(tx.Id, wallet.Id, "WalletAccount", Money.Vnd(20_000), isDebit: true);

        db.BankingCustomers.Add(customer);
        db.WalletAccounts.Add(wallet);
        db.LedgerTransactions.Add(tx);
        db.LedgerEntries.AddRange(entry1, entry2);
        await db.SaveChangesAsync();

        var handler = new GetWalletLedgerHandler(db);
        var result = await handler.Handle(new GetWalletLedgerQuery("ACC-FRANK-01"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var notFound = await handler.Handle(new GetWalletLedgerQuery("ACC-UNKNOWN"), CancellationToken.None);
        Assert.Null(notFound);
    }

    [Fact]
    public async Task FreezeWalletHandler_ShouldReturnFrozenResponse()
    {
        var handler = new FreezeWalletHandler();
        var command = new FreezeWalletCommand("W-12345", "Suspicious activity detected");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("W-12345", result.WalletId);
        Assert.Equal("Frozen", result.Status);
        Assert.Equal("Suspicious activity detected", result.Reason);
        Assert.True(result.FrozenAt <= DateTime.UtcNow);
    }
}
