using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Accounts.Domain;
using MiniBanking.Modules.Ledger.Application.Commands.ReconcileLedger;
using MiniBanking.Modules.Ledger.Application.Queries.GetLedgerEntries;
using MiniBanking.Modules.Ledger.Application.Queries.GetLedgerTransactionById;
using MiniBanking.Modules.Ledger.Application.Queries.GetLedgerTransactions;
using MiniBanking.Modules.Ledger.Domain;
using MiniBanking.SharedKernel;
using Xunit;

namespace MiniBanking.Tests;

public class LedgerCqrsTests
{
    private MiniBankingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<MiniBankingDbContext>()
            .UseInMemoryDatabase(databaseName: $"MiniBankingLedgerTestDb_{Guid.NewGuid():N}")
            .Options;

        return new MiniBankingDbContext(options);
    }

    [Fact]
    public async Task GetLedgerTransactionById_ShouldReturnNull_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var handler = new GetLedgerTransactionByIdHandler(db);

        var result = await handler.Handle(new GetLedgerTransactionByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLedgerTransactionById_ShouldReturnTransaction_WhenFound()
    {
        using var db = CreateInMemoryDbContext();
        var tx = new LedgerTransaction("REF-001", LedgerTransactionType.Payment, "Payment test");
        var walletId = Guid.NewGuid();
        tx.AddEntry(walletId, "WalletAccount", Money.Vnd(50_000), isDebit: true);
        tx.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", Money.Vnd(50_000), isDebit: false);
        tx.ValidateInvariant();

        db.LedgerTransactions.Add(tx);
        await db.SaveChangesAsync();

        var handler = new GetLedgerTransactionByIdHandler(db);
        var result = await handler.Handle(new GetLedgerTransactionByIdQuery(tx.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(tx.Id, result.TransactionId);
        Assert.Equal("REF-001", result.ReferenceId);
        Assert.Equal("Payment", result.Type);
        Assert.Equal("Completed", result.Status);
        Assert.Equal("Payment test", result.Description);
        Assert.Equal(2, result.Entries.Count());
    }

    [Fact]
    public async Task GetLedgerEntries_ShouldFilterAndPaginateCorrectly()
    {
        using var db = CreateInMemoryDbContext();
        var customer = new BankingCustomer("Alice", "alice@example.com", "0900000001");
        var wallet = new WalletAccount(customer, "ACC-ALICE-1", "VND");
        db.BankingCustomers.Add(customer);
        db.WalletAccounts.Add(wallet);

        var tx = new LedgerTransaction("REF-002", LedgerTransactionType.Payment, "Payment Alice");
        tx.AddEntry(wallet.Id, "WalletAccount", Money.Vnd(100_000), isDebit: true);
        tx.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", Money.Vnd(100_000), isDebit: false);
        tx.ValidateInvariant();

        db.LedgerTransactions.Add(tx);
        await db.SaveChangesAsync();

        var handler = new GetLedgerEntriesHandler(db);

        // Act - query all
        var res = await handler.Handle(new GetLedgerEntriesQuery(Page: 1, PageSize: 10), CancellationToken.None);

        // Assert
        Assert.Equal(2, res.TotalCount);
        Assert.Equal(2, res.Items.Count);
        Assert.Equal(1, res.Page);
        Assert.Equal(10, res.PageSize);
        Assert.Equal(1, res.TotalPages);

        var walletEntry = res.Items.First(e => e.AccountId == wallet.Id);
        Assert.Equal("ACC-ALICE-1 (Alice)", walletEntry.AccountDisplay);
        Assert.True(walletEntry.IsDebit);
        Assert.Equal("Debit (Nợ)", walletEntry.TypeDisplay);

        var clearingEntry = res.Items.First(e => e.AccountId == SystemAccountIds.PlatformClearing);
        Assert.Equal("Platform Clearing Account", clearingEntry.AccountDisplay);
        Assert.False(clearingEntry.IsDebit);
        Assert.Equal("Credit (Có)", clearingEntry.TypeDisplay);

        // Act - filter by accountId
        var filteredRes = await handler.Handle(new GetLedgerEntriesQuery(AccountId: wallet.Id), CancellationToken.None);
        Assert.Equal(1, filteredRes.TotalCount);
        Assert.Equal(wallet.Id, filteredRes.Items[0].AccountId);
    }

    [Fact]
    public async Task GetLedgerTransactions_ShouldReturnCalculatedTotalsAndBalance()
    {
        using var db = CreateInMemoryDbContext();
        var tx = new LedgerTransaction("REF-003", LedgerTransactionType.Payment, "Balanced payment");
        var walletId = Guid.NewGuid();
        tx.AddEntry(walletId, "WalletAccount", Money.Vnd(200_000), isDebit: true);
        tx.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", Money.Vnd(200_000), isDebit: false);
        tx.ValidateInvariant();

        db.LedgerTransactions.Add(tx);
        await db.SaveChangesAsync();

        var handler = new GetLedgerTransactionsHandler(db);
        var res = await handler.Handle(new GetLedgerTransactionsQuery(1, 10, "Payment"), CancellationToken.None);

        Assert.Equal(1, res.TotalCount);
        var item = res.Items[0];
        Assert.Equal(tx.Id, item.TransactionId);
        Assert.Equal("Payment", item.Type);
        Assert.Equal(2, item.EntriesCount);
        Assert.Equal(200_000, item.TotalDebit);
        Assert.Equal(200_000, item.TotalCredit);
        Assert.True(item.IsBalanced);
        Assert.Equal(2, item.Entries.Count);
    }

    [Fact]
    public async Task ReconcileLedger_ShouldDetectMatchesAndDiscrepancies()
    {
        using var db = CreateInMemoryDbContext();

        // Wallet 1: balanced (snap 100k, ledger net +100k)
        var cust1 = new BankingCustomer("Bob", "bob@example.com", "0900000002");
        var wallet1 = new WalletAccount(cust1, "ACC-BOB-1", "VND");
        var snap1 = new BalanceSnapshot(wallet1, Money.Vnd(100_000));

        var tx1 = new LedgerTransaction("REF-TOP-1", LedgerTransactionType.TopUp, "Top-up");
        tx1.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", Money.Vnd(100_000), isDebit: true);
        tx1.AddEntry(wallet1.Id, "WalletAccount", Money.Vnd(100_000), isDebit: false);
        tx1.ValidateInvariant();

        // Wallet 2: discrepancy (snap 200k, ledger net 0)
        var cust2 = new BankingCustomer("Charlie", "charlie@example.com", "0900000003");
        var wallet2 = new WalletAccount(cust2, "ACC-CHARLIE-1", "VND");
        var snap2 = new BalanceSnapshot(wallet2, Money.Vnd(200_000));

        db.BankingCustomers.AddRange(cust1, cust2);
        db.WalletAccounts.AddRange(wallet1, wallet2);
        db.BalanceSnapshots.AddRange(snap1, snap2);
        db.LedgerTransactions.Add(tx1);
        await db.SaveChangesAsync();

        var handler = new ReconcileLedgerHandler(db);
        var result = await handler.Handle(new ReconcileLedgerCommand(), CancellationToken.None);

        Assert.Equal(2, result.TotalWallets);
        Assert.Equal(1, result.DiscrepanciesCount);
        Assert.False(result.IsFullyReconciled);
        Assert.Equal(2, result.Details.Count);

        var d1 = result.Details.First(d => d.WalletId == wallet1.Id);
        Assert.True(d1.IsMatched);
        Assert.Equal(100_000, d1.SnapshotBalance);
        Assert.Equal(100_000, d1.CalculatedBalance);
        Assert.Equal(0, d1.Difference);

        var d2 = result.Details.First(d => d.WalletId == wallet2.Id);
        Assert.False(d2.IsMatched);
        Assert.Equal(200_000, d2.SnapshotBalance);
        Assert.Equal(0, d2.CalculatedBalance);
        Assert.Equal(200_000, d2.Difference);
    }
}
