using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Security;
using MiniBanking.Modules.Accounts.Domain;
using MiniBanking.Modules.Admin.Domain;
using MiniBanking.Modules.Ledger.Domain;
using MiniBanking.Modules.Merchants.Domain;
using MiniBanking.Modules.Payments.Domain;
using MiniBanking.SharedKernel;

namespace MiniBanking.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(MiniBankingDbContext context)
    {
        // 1. Seed Admin user
        if (!await context.AdminUsers.AnyAsync())
        {
            var admin = new AdminUser(
                "admin@minibanking.local",
                "System Administrator",
                PasswordHasher.Hash("Admin@123"),
                "Admin");
            context.AdminUsers.Add(admin);
        }

        // 2. Seed Merchants
        var defaultMerchants = new List<Merchant>
        {
            new Merchant(
                "MCH-ECOM-ALPHA",
                "E-commerce Shop Alpha",
                "mch_live_alpha998127391823791",
                "mch_sec_alpha7761823910283918237192",
                "http://localhost:5335/api/v1/demo/webhook-receiver"),
            new Merchant(
                "MCH-TECH-BETA",
                "Tech Store Beta",
                "mch_live_beta445129381726354",
                "mch_sec_beta9981273645123984756182",
                "http://localhost:5335/api/v1/demo/webhook-receiver"),
            new Merchant(
                "MCH-FASHION-HUB",
                "Fashion Hub",
                "mch_live_hub771829384756123",
                "mch_sec_hub1122334455667788990011",
                "http://localhost:5335/api/v1/demo/webhook-receiver"),
            new Merchant(
                "ecommerce-demo",
                "Demo E-commerce",
                "merchant-api-key",
                "merchant-secret-key",
                "http://localhost:5335/api/v1/demo/webhook-receiver")
        };

        foreach (var m in defaultMerchants)
        {
            if (!await context.Merchants.AnyAsync(x => x.MerchantId == m.MerchantId))
            {
                context.Merchants.Add(m);
            }
        }

        // 3. Seed Customers & Wallets
        if (!await context.BankingCustomers.AnyAsync())
        {
            var seedData = new[]
            {
                (Name: "Nguyễn Văn An", Email: "an.nguyen@example.com", Phone: "0912345678", Acc: "WA-8801928371", Balance: 2_500_000L),
                (Name: "Trần Thị Bình", Email: "binh.tran@example.com", Phone: "0987654321", Acc: "WA-8801928372", Balance: 5_000_000L),
                (Name: "Lê Hoàng Cường", Email: "cuong.le@example.com", Phone: "0903112233", Acc: "WA-8801928373", Balance: 1_200_000L),
                (Name: "Phạm Thu Dung", Email: "dung.pham@example.com", Phone: "0938445566", Acc: "WA-8801928374", Balance: 12_000_000L),
                (Name: "Vũ Đức Em", Email: "em.vu@example.com", Phone: "0977889900", Acc: "WA-8801928375", Balance: 850_000L)
            };

            foreach (var item in seedData)
            {
                var customer = new BankingCustomer(item.Name, item.Email, item.Phone);
                var wallet = new WalletAccount(customer, item.Acc, "VND");
                var balance = new BalanceSnapshot(wallet, Money.Vnd(item.Balance));

                var topUpTxn = new LedgerTransaction(
                    $"TOPUP-{Guid.NewGuid():N}",
                    LedgerTransactionType.TopUp,
                    $"Initial seed deposit for {item.Name}");

                topUpTxn.AddEntry(SystemAccountIds.PlatformClearing, "PlatformClearing", Money.Vnd(item.Balance), isDebit: true);
                topUpTxn.AddEntry(wallet.Id, "WalletAccount", Money.Vnd(item.Balance), isDebit: false);
                topUpTxn.ValidateInvariant();

                context.BankingCustomers.Add(customer);
                context.WalletAccounts.Add(wallet);
                context.BalanceSnapshots.Add(balance);
                context.LedgerTransactions.Add(topUpTxn);
            }
        }

        await context.SaveChangesAsync();
    }
}
