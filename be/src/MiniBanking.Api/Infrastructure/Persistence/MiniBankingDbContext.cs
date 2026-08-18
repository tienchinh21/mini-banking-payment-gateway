using Microsoft.EntityFrameworkCore;
using MiniBanking.Modules.Accounts.Domain;
using MiniBanking.Modules.Ledger.Domain;
using MiniBanking.Modules.Merchants.Domain;
using MiniBanking.Modules.Payments.Domain;
using MiniBanking.SharedKernel;

namespace MiniBanking.Infrastructure.Persistence;

public class MiniBankingDbContext : DbContext
{
    public MiniBankingDbContext(DbContextOptions<MiniBankingDbContext> options)
        : base(options)
    {
    }

    public DbSet<BankingCustomer> BankingCustomers => Set<BankingCustomer>();
    public DbSet<WalletAccount> WalletAccounts => Set<WalletAccount>();
    public DbSet<BalanceSnapshot> BalanceSnapshots => Set<BalanceSnapshot>();
    public DbSet<LedgerTransaction> LedgerTransactions => Set<LedgerTransaction>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<Settlement> Settlements => Set<Settlement>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("public");
        modelBuilder.Ignore<DomainEvent>();

        modelBuilder.Entity<BankingCustomer>(entity =>
        {
            entity.ToTable("banking_customers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
        });

        modelBuilder.Entity<WalletAccount>(entity =>
        {
            entity.ToTable("wallet_accounts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.HasIndex(e => e.AccountNumber).IsUnique();
            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BalanceSnapshot>(entity =>
        {
            entity.ToTable("balance_snapshots");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AvailableBalance).IsRequired();
            entity.Property(e => e.LedgerBalance).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.HasIndex(e => e.WalletAccountId).IsUnique();
            entity.HasOne(e => e.WalletAccount)
                  .WithMany()
                  .HasForeignKey(e => e.WalletAccountId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LedgerTransaction>(entity =>
        {
            entity.ToTable("ledger_transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReferenceId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.HasIndex(e => e.ReferenceId).IsUnique();
            entity.HasMany(e => e.Entries)
                  .WithOne(e => e.LedgerTransaction)
                  .HasForeignKey(e => e.LedgerTransactionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LedgerEntry>(entity =>
        {
            entity.ToTable("ledger_entries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AccountType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Amount).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.IsDebit).IsRequired();
            entity.Property(e => e.Sequence).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.HasIndex(e => new { e.LedgerTransactionId, e.Sequence });
            entity.HasIndex(e => e.AccountId);
        });

        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.ToTable("merchants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MerchantId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ApiKey).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Secret).HasMaxLength(500).IsRequired();
            entity.Property(e => e.WebhookUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.HasIndex(e => e.MerchantId).IsUnique();
            entity.HasIndex(e => e.ApiKey).IsUnique();
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MerchantId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MerchantOrderId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Amount).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CallbackUrl).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.FailureCode).HasMaxLength(100);
            entity.Property(e => e.IdempotencyKey).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.HasIndex(e => new { e.MerchantId, e.IdempotencyKey }).IsUnique();
            entity.HasIndex(e => e.MerchantOrderId);
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.ToTable("idempotency_records");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MerchantId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Key).HasMaxLength(200).IsRequired();
            entity.Property(e => e.RequestMethod).HasMaxLength(10).IsRequired();
            entity.Property(e => e.RequestPath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.RequestBodyHash).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ResponsePayload);
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.HasIndex(e => new { e.MerchantId, e.Key }).IsUnique();
        });

        modelBuilder.Entity<Refund>(entity =>
        {
            entity.ToTable("refunds");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MerchantId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MerchantRefundId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Amount).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.FailureCode).HasMaxLength(100);
            entity.Property(e => e.IdempotencyKey).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.HasIndex(e => new { e.MerchantId, e.IdempotencyKey }).IsUnique();
            entity.HasIndex(e => new { e.MerchantId, e.MerchantRefundId }).IsUnique();
            entity.HasIndex(e => e.PaymentId);
        });

        modelBuilder.Entity<Settlement>(entity =>
        {
            entity.ToTable("settlements");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MerchantId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.BatchReference).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Amount).IsRequired();
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.PaymentCount).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.HasIndex(e => new { e.MerchantId, e.BatchReference }).IsUnique();
        });
    }
}
