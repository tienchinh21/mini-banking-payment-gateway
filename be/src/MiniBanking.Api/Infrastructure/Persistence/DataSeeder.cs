using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Security;
using MiniBanking.Modules.Accounts.Domain;
using MiniBanking.Modules.Admin.Domain;
using MiniBanking.Modules.Ledger.Domain;
using MiniBanking.Modules.Merchants.Domain;
using MiniBanking.SharedKernel;

namespace MiniBanking.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(MiniBankingDbContext context)
    {
        var existingMerchant = await context.Merchants.FirstOrDefaultAsync(m => m.MerchantId == "ecommerce-demo");
        var demoWebhookUrl = "http://localhost:5335/api/v1/demo/webhook-receiver";
        if (existingMerchant is null)
        {
            var merchant = new Merchant(
                "ecommerce-demo",
                "Demo E-commerce",
                "merchant-api-key",
                "merchant-secret-key",
                demoWebhookUrl);
            context.Merchants.Add(merchant);
        }
        else if (existingMerchant.WebhookUrl != demoWebhookUrl)
        {
            // Keep the demo merchant webhook URL pointed at the local receiver.
            existingMerchant.SetWebhookUrl(demoWebhookUrl);
            context.Merchants.Update(existingMerchant);
        }

        if (!await context.AdminUsers.AnyAsync())
        {
            var admin = new AdminUser(
                "admin@minibanking.local",
                "System Administrator",
                PasswordHasher.Hash("Admin@123"),
                "Admin");
            context.AdminUsers.Add(admin);
        }

        if (await context.BankingCustomers.AnyAsync())
        {
            await context.SaveChangesAsync();
            return;
        }

        var customer = new BankingCustomer("Demo Customer", "demo@minibanking.local", "0900000000");
        var wallet = new WalletAccount(customer, "WALLET_DEMO_001", "VND");
        var balance = new BalanceSnapshot(wallet, Money.Vnd(500_000));

        var topUpTransaction = new LedgerTransaction("TOPUP-001", LedgerTransactionType.TopUp, "Initial demo top-up");
        topUpTransaction.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", Money.Vnd(500_000), isDebit: true);
        topUpTransaction.AddEntry(wallet.Id, "WalletAccount", Money.Vnd(500_000), isDebit: false);
        topUpTransaction.ValidateInvariant();

        context.BankingCustomers.Add(customer);
        context.WalletAccounts.Add(wallet);
        context.BalanceSnapshots.Add(balance);
        context.LedgerTransactions.Add(topUpTransaction);

        await context.SaveChangesAsync();
    }
}
