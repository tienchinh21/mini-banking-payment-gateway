using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Modules.Merchants.Application.Commands.CreateMerchant;
using MiniBanking.Modules.Merchants.Application.Commands.DeactivateMerchant;
using MiniBanking.Modules.Merchants.Application.Commands.RegenerateMerchantKeys;
using MiniBanking.Modules.Merchants.Application.Commands.UpdateMerchant;
using MiniBanking.Modules.Merchants.Application.Queries.GetMerchantById;
using MiniBanking.Modules.Merchants.Application.Queries.GetMerchants;
using MiniBanking.Modules.Merchants.Domain;
using MiniBanking.Modules.Payments.Domain;
using MiniBanking.SharedKernel;
using Xunit;

namespace MiniBanking.Tests;

public class MerchantCqrsTests
{
    private MiniBankingDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<MiniBankingDbContext>()
            .UseInMemoryDatabase(databaseName: $"MiniBankingMerchantCqrs_{Guid.NewGuid():N}")
            .Options;

        return new MiniBankingDbContext(options);
    }

    [Fact]
    public async Task GetMerchantsHandler_ShouldReturnPaginatedList_WithSearchAndFilter()
    {
        using var db = CreateInMemoryDbContext();
        var m1 = new Merchant("shopee-vn", "Shopee Vietnam", "apiKey1", "secret1234567890", "https://shopee.vn/webhook");
        var m2 = new Merchant("lazada-vn", "Lazada Vietnam", "apiKey2", "secret2234567890", "https://lazada.vn/webhook");
        var m3 = new Merchant("tiki-vn", "Tiki Vietnam", "apiKey3", "secret3234567890", null);
        m3.Deactivate();

        db.Merchants.AddRange(m1, m2, m3);
        await db.SaveChangesAsync();

        var handler = new GetMerchantsHandler(db);

        // 1. All merchants
        var allRes = await handler.Handle(new GetMerchantsQuery(Page: 1, PageSize: 10), CancellationToken.None);
        Assert.Equal(3, allRes.TotalCount);
        Assert.Equal(3, allRes.Items.Count);
        Assert.NotNull(allRes.Meta);
        Assert.Equal(3, allRes.Meta.TotalItems);
        Assert.Equal(1, allRes.Meta.CurrentPage);
        Assert.False(allRes.Meta.HasNext);

        // Check masked secret
        var item1 = allRes.Items.First(x => x.MerchantId == "shopee-vn");
        Assert.Equal("Shopee Vietnam", item1.Name);
        Assert.Equal("shopee-vn", item1.Code);
        Assert.Equal("ACTIVE", item1.Status);
        Assert.True(item1.IsActive);
        Assert.StartsWith("secr", item1.SecretMasked);
        Assert.EndsWith("7890", item1.SecretMasked);
        Assert.Contains("••••••••", item1.SecretMasked);

        // 2. Filter by search
        var searchRes = await handler.Handle(new GetMerchantsQuery(Search: "lazada"), CancellationToken.None);
        Assert.Equal(1, searchRes.TotalCount);
        Assert.Single(searchRes.Items);
        Assert.Equal("lazada-vn", searchRes.Items[0].MerchantId);

        // 3. Filter by isActive
        var activeRes = await handler.Handle(new GetMerchantsQuery(IsActive: true), CancellationToken.None);
        Assert.Equal(2, activeRes.TotalCount);

        var inactiveRes = await handler.Handle(new GetMerchantsQuery(IsActive: false), CancellationToken.None);
        Assert.Equal(1, inactiveRes.TotalCount);
        Assert.Equal("tiki-vn", inactiveRes.Items[0].MerchantId);
        Assert.Equal("SUSPENDED", inactiveRes.Items[0].Status);
    }

    [Fact]
    public async Task GetMerchantByIdHandler_ShouldReturnMerchant_WithVolumeAndPaymentStats()
    {
        using var db = CreateInMemoryDbContext();
        var merchant = new Merchant("grab-vn", "Grab Vietnam", "grabApiKey", "grabSecret123456", "https://grab.vn/webhook");
        db.Merchants.Add(merchant);

        var p1 = new Payment(merchant.MerchantId, "ORD-001", Guid.NewGuid(), Money.Vnd(150_000), "Payment 1", null, "idemp-001");
        p1.MarkSucceeded(Guid.NewGuid());

        var p2 = new Payment(merchant.MerchantId, "ORD-002", Guid.NewGuid(), Money.Vnd(250_000), "Payment 2", null, "idemp-002");
        p2.MarkSucceeded(Guid.NewGuid());

        var p3 = new Payment(merchant.MerchantId, "ORD-003", Guid.NewGuid(), Money.Vnd(100_000), "Payment 3", null, "idemp-003");
        p3.MarkFailed("INSUFFICIENT_FUNDS");

        db.Payments.AddRange(p1, p2, p3);
        await db.SaveChangesAsync();

        var handler = new GetMerchantByIdHandler(db);

        // Query by Guid
        var byGuid = await handler.Handle(new GetMerchantByIdQuery(merchant.Id.ToString()), CancellationToken.None);
        Assert.NotNull(byGuid);
        Assert.Equal("grab-vn", byGuid.MerchantId);
        Assert.Equal("grab-vn", byGuid.Code);
        Assert.Equal("Grab Vietnam", byGuid.Name);
        Assert.Equal(3, byGuid.TotalPayments);
        Assert.Equal(400_000L, byGuid.TotalVolume);
        Assert.Equal(400_000L, byGuid.TotalPaidAmount);

        // Query by string MerchantId
        var byMerchantId = await handler.Handle(new GetMerchantByIdQuery("grab-vn"), CancellationToken.None);
        Assert.NotNull(byMerchantId);
        Assert.Equal(merchant.Id, byMerchantId.Id);

        // Query Non-existent
        var notFound = await handler.Handle(new GetMerchantByIdQuery("non-existent-merchant"), CancellationToken.None);
        Assert.Null(notFound);
    }

    [Fact]
    public async Task CreateMerchantHandler_ShouldCreateMerchant_WithGeneratedCredentials()
    {
        using var db = CreateInMemoryDbContext();
        var handler = new CreateMerchantHandler(db);
        var command = new CreateMerchantCommand("be-tech", "BE Technology", "https://be.com.vn/webhook");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("be-tech", result.MerchantId);
        Assert.Equal("be-tech", result.Code);
        Assert.Equal("BE Technology", result.Name);
        Assert.Equal("https://be.com.vn/webhook", result.WebhookUrl);
        Assert.True(result.IsActive);
        Assert.Equal("ACTIVE", result.Status);
        Assert.StartsWith("mb_live_", result.ApiKey);
        Assert.NotEmpty(result.Secret);

        // Verify in DB
        var saved = await db.Merchants.FirstOrDefaultAsync(m => m.Id == result.Id);
        Assert.NotNull(saved);
        Assert.Equal("be-tech", saved.MerchantId);
    }

    [Fact]
    public async Task CreateMerchantHandler_ShouldThrow_WhenMerchantIdAlreadyExists()
    {
        using var db = CreateInMemoryDbContext();
        var existing = new Merchant("duplicate-id", "Duplicate Merchant", "key", "secret");
        db.Merchants.Add(existing);
        await db.SaveChangesAsync();

        var handler = new CreateMerchantHandler(db);
        var command = new CreateMerchantCommand("duplicate-id", "Another Merchant", null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("MerchantId đã tồn tại", ex.Message);
    }

    [Fact]
    public void CreateMerchantCommandValidator_ShouldValidateFields()
    {
        var validator = new CreateMerchantCommandValidator();

        // Valid
        var validCmd = new CreateMerchantCommand("merchant-1", "Valid Name", "https://example.com/webhook");
        var validResult = validator.Validate(validCmd);
        Assert.True(validResult.IsValid);

        // Invalid empty fields
        var emptyCmd = new CreateMerchantCommand("", "", "invalid-url");
        var invalidResult = validator.Validate(emptyCmd);
        Assert.False(invalidResult.IsValid);
        Assert.Contains(invalidResult.Errors, e => e.PropertyName == "MerchantId");
        Assert.Contains(invalidResult.Errors, e => e.PropertyName == "Name");
        Assert.Contains(invalidResult.Errors, e => e.PropertyName == "WebhookUrl");
    }

    [Fact]
    public async Task UpdateMerchantHandler_ShouldUpdateDetails_WhenExists()
    {
        using var db = CreateInMemoryDbContext();
        var merchant = new Merchant("zalo-pay", "ZaloPay Old", "key", "secret", "https://old.com/webhook");
        db.Merchants.Add(merchant);
        await db.SaveChangesAsync();

        var handler = new UpdateMerchantHandler(db);
        var command = new UpdateMerchantCommand(merchant.Id.ToString(), "ZaloPay Updated", "https://new.com/webhook", false);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("ZaloPay Updated", result.Name);
        Assert.Equal("https://new.com/webhook", result.WebhookUrl);
        Assert.False(result.IsActive);
        Assert.Equal("SUSPENDED", result.Status);
        Assert.NotNull(result.UpdatedAt);

        var updated = await db.Merchants.FirstOrDefaultAsync(m => m.Id == merchant.Id);
        Assert.NotNull(updated);
        Assert.Equal("ZaloPay Updated", updated.Name);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task UpdateMerchantHandler_ShouldThrow_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var handler = new UpdateMerchantHandler(db);
        var command = new UpdateMerchantCommand("non-existent", "Name", null, true);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public void UpdateMerchantCommandValidator_ShouldValidateFields()
    {
        var validator = new UpdateMerchantCommandValidator();

        var invalidCmd = new UpdateMerchantCommand("", "", "invalid-uri", true);
        var res = validator.Validate(invalidCmd);

        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == "Id");
        Assert.Contains(res.Errors, e => e.PropertyName == "Name");
        Assert.Contains(res.Errors, e => e.PropertyName == "WebhookUrl");
    }

    [Fact]
    public async Task DeactivateMerchantHandler_ShouldDeactivateMerchant_WhenExists()
    {
        using var db = CreateInMemoryDbContext();
        var merchant = new Merchant("momo-vn", "MoMo Vietnam", "key", "secret");
        db.Merchants.Add(merchant);
        await db.SaveChangesAsync();

        var handler = new DeactivateMerchantHandler(db);
        var command = new DeactivateMerchantCommand(merchant.MerchantId);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(merchant.Id, result.Id);
        Assert.Equal("momo-vn", result.MerchantId);
        Assert.False(result.IsActive);
        Assert.Equal("SUSPENDED", result.Status);

        var dbMerchant = await db.Merchants.FirstOrDefaultAsync(m => m.Id == merchant.Id);
        Assert.NotNull(dbMerchant);
        Assert.False(dbMerchant.IsActive);
    }

    [Fact]
    public async Task DeactivateMerchantHandler_ShouldThrow_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var handler = new DeactivateMerchantHandler(db);
        var command = new DeactivateMerchantCommand("non-existent");

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public void DeactivateMerchantCommandValidator_ShouldValidateId()
    {
        var validator = new DeactivateMerchantCommandValidator();
        var res = validator.Validate(new DeactivateMerchantCommand(""));
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == "Id");
    }

    [Fact]
    public async Task RegenerateMerchantKeysHandler_ShouldRotateApiKeyAndSecret()
    {
        using var db = CreateInMemoryDbContext();
        var merchant = new Merchant("vnpay-qr", "VNPay QR", "oldApiKey", "oldSecret");
        db.Merchants.Add(merchant);
        await db.SaveChangesAsync();

        var handler = new RegenerateMerchantKeysHandler(db);
        var command = new RegenerateMerchantKeysCommand(merchant.Id.ToString());

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(merchant.Id, result.Id);
        Assert.Equal("vnpay-qr", result.MerchantId);
        Assert.NotEqual("oldApiKey", result.ApiKey);
        Assert.NotEqual("oldSecret", result.Secret);
        Assert.StartsWith("mb_live_", result.ApiKey);
        Assert.NotEmpty(result.Secret);
        Assert.NotNull(result.UpdatedAt);

        var updated = await db.Merchants.FirstOrDefaultAsync(m => m.Id == merchant.Id);
        Assert.NotNull(updated);
        Assert.Equal(result.ApiKey, updated.ApiKey);
        Assert.Equal(result.Secret, updated.Secret);
    }

    [Fact]
    public async Task RegenerateMerchantKeysHandler_ShouldThrow_WhenNotFound()
    {
        using var db = CreateInMemoryDbContext();
        var handler = new RegenerateMerchantKeysHandler(db);
        var command = new RegenerateMerchantKeysCommand("non-existent");

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public void RegenerateMerchantKeysCommandValidator_ShouldValidateId()
    {
        var validator = new RegenerateMerchantKeysCommandValidator();
        var res = validator.Validate(new RegenerateMerchantKeysCommand(""));
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == "Id");
    }

    [Fact]
    public void Merchant_DomainEntity_GuardsAndMethodsWorkCorrectly()
    {
        // Constructor validation
        Assert.Throws<ArgumentException>(() => new Merchant("", "Name", "key", "secret"));
        Assert.Throws<ArgumentException>(() => new Merchant("id", "", "key", "secret"));
        Assert.Throws<ArgumentException>(() => new Merchant("id", "Name", "", "secret"));
        Assert.Throws<ArgumentException>(() => new Merchant("id", "Name", "key", ""));

        var merchant = new Merchant("test-id", "Test Name", "key1", "secret1", "https://example.com");
        Assert.True(merchant.IsActive);

        // UpdateDetails
        merchant.UpdateDetails("Updated Name", "https://example2.com", true);
        Assert.Equal("Updated Name", merchant.Name);
        Assert.Equal("https://example2.com", merchant.WebhookUrl);
        Assert.Throws<ArgumentException>(() => merchant.UpdateDetails("", null, true));

        // Deactivate & Activate
        merchant.Deactivate();
        Assert.False(merchant.IsActive);
        merchant.Activate();
        Assert.True(merchant.IsActive);

        // RegenerateCredentials
        merchant.RegenerateCredentials("newKey", "newSecret");
        Assert.Equal("newKey", merchant.ApiKey);
        Assert.Equal("newSecret", merchant.Secret);
        Assert.Throws<ArgumentException>(() => merchant.RegenerateCredentials("", "sec"));
        Assert.Throws<ArgumentException>(() => merchant.RegenerateCredentials("key", ""));
    }
}
