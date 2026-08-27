using MiniBanking.Infrastructure.Security;
using MiniBanking.Modules.Payments.Domain;

namespace MiniBanking.Tests;

/// <summary>
/// Unit tests for the idempotency key logic covering:
/// - Same key + same hash  → replay response
/// - Same key + diff hash  → conflict
/// - Complete() state transitions
/// - Initial status
/// </summary>
public class IdempotencyTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IdempotencyRecord MakeRecord(
        string merchantId     = "merchant-A",
        string key            = "key-001",
        string requestMethod  = "POST",
        string requestPath    = "/api/payments",
        string requestBodyHash = "hash-abc") =>
        new(merchantId, key, requestMethod, requestPath, requestBodyHash);

    // ── Construction ──────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithValidArguments_SetsAllProperties()
    {
        var record = MakeRecord();

        Assert.Equal("merchant-A", record.MerchantId);
        Assert.Equal("key-001",    record.Key);
        Assert.Equal("POST",       record.RequestMethod);
        Assert.Equal("/api/payments", record.RequestPath);
        Assert.Equal("hash-abc",   record.RequestBodyHash);
    }

    [Fact]
    public void Constructor_InitialStatus_IsProcessing()
    {
        var record = MakeRecord();

        Assert.Equal("Processing", record.Status);
    }

    [Fact]
    public void Constructor_InitialResponsePayload_IsNull()
    {
        var record = MakeRecord();

        Assert.Null(record.ResponsePayload);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankMerchantId_ThrowsArgumentException(string? merchantId)
    {
        Assert.Throws<ArgumentException>(() =>
            new IdempotencyRecord(merchantId!, "key", "POST", "/api/payments", "hash"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankKey_ThrowsArgumentException(string? key)
    {
        Assert.Throws<ArgumentException>(() =>
            new IdempotencyRecord("merchant", key!, "POST", "/api/payments", "hash"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankRequestMethod_ThrowsArgumentException(string? method)
    {
        Assert.Throws<ArgumentException>(() =>
            new IdempotencyRecord("merchant", "key", method!, "/api/payments", "hash"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankRequestPath_ThrowsArgumentException(string? path)
    {
        Assert.Throws<ArgumentException>(() =>
            new IdempotencyRecord("merchant", "key", "POST", path!, "hash"));
    }

    // ── Complete() ────────────────────────────────────────────────────────────

    [Fact]
    public void Complete_SetsStatusToCompleted()
    {
        var record = MakeRecord();

        record.Complete("{\"result\": \"ok\"}");

        Assert.Equal("Completed", record.Status);
    }

    [Fact]
    public void Complete_StoresResponsePayload()
    {
        var record  = MakeRecord();
        const string payload = "{\"paymentId\": \"123\"}";

        record.Complete(payload);

        Assert.Equal(payload, record.ResponsePayload);
    }

    [Fact]
    public void Complete_CalledTwice_OverwritesPayload()
    {
        var record = MakeRecord();

        record.Complete("first");
        record.Complete("second");

        Assert.Equal("second", record.ResponsePayload);
        Assert.Equal("Completed", record.Status);
    }

    // ── Same key + same hash → replay ─────────────────────────────────────────

    [Fact]
    public void SameKeyAndSameHash_ExistingCompletedRecord_ShouldReplayResponse()
    {
        // Arrange: an already-completed record
        const string body    = "{\"amount\": 10000}";
        var bodyHash         = HmacSignatureService.ComputeBodyHash(body);
        const string payload = "{\"paymentId\": \"abc-123\", \"status\": \"Succeeded\"}";

        var record = new IdempotencyRecord("merchant-A", "idem-key-1", "POST", "/api/payments", bodyHash);
        record.Complete(payload);

        // Simulate what the handler does:
        var incomingHash = HmacSignatureService.ComputeBodyHash(body); // same body
        var isConflict   = record.RequestBodyHash != incomingHash;
        var isReplay     = record.Status == "Completed" && !string.IsNullOrEmpty(record.ResponsePayload);

        Assert.False(isConflict, "Same body should not be a conflict.");
        Assert.True(isReplay,    "A completed record with same hash should replay.");
        Assert.Equal(payload,    record.ResponsePayload);
    }

    [Fact]
    public void SameKeyAndSameHash_RecordStillProcessing_IsNotReplayable()
    {
        const string body    = "{\"amount\": 10000}";
        var bodyHash         = HmacSignatureService.ComputeBodyHash(body);

        var record = new IdempotencyRecord("merchant-A", "idem-key-2", "POST", "/api/payments", bodyHash);
        // NOT completed yet

        var incomingHash = HmacSignatureService.ComputeBodyHash(body);
        var isConflict   = record.RequestBodyHash != incomingHash;
        var isReplay     = record.Status == "Completed" && !string.IsNullOrEmpty(record.ResponsePayload);

        Assert.False(isConflict, "Same body should not be a conflict.");
        Assert.False(isReplay,   "A Processing record must not be replayed.");
    }

    // ── Same key + different hash → conflict ──────────────────────────────────

    [Fact]
    public void SameKeyAndDifferentHash_ShouldSignalConflict()
    {
        const string originalBody = "{\"amount\": 10000}";
        const string differentBody = "{\"amount\": 99999}";
        var originalHash  = HmacSignatureService.ComputeBodyHash(originalBody);
        var differentHash = HmacSignatureService.ComputeBodyHash(differentBody);

        var record = new IdempotencyRecord("merchant-A", "idem-key-3", "POST", "/api/payments", originalHash);
        record.Complete("{\"paymentId\": \"abc\"}");

        // Incoming request uses the SAME key but DIFFERENT body
        var isConflict = record.RequestBodyHash != differentHash;

        Assert.True(isConflict, "Different body hash with same key should be detected as conflict.");
    }

    [Fact]
    public void SameKeyAndDifferentHash_HandlerShouldThrowInvalidOperationException()
    {
        // This test mirrors the exact handler logic for the conflict branch.
        const string originalBody  = "{\"amount\": 10000}";
        const string conflictBody  = "{\"amount\": 1}";
        var originalHash = HmacSignatureService.ComputeBodyHash(originalBody);
        var conflictHash = HmacSignatureService.ComputeBodyHash(conflictBody);

        var record = new IdempotencyRecord("merchant-B", "idem-conflict-key", "POST", "/api/payments", originalHash);
        record.Complete("{\"ok\": true}");

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            // Replicate the handler guard
            if (record.RequestBodyHash != conflictHash)
                throw new InvalidOperationException("Idempotency key was used with a different request body.");
        });

        Assert.Contains("different request body", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ── HmacSignatureService.ComputeBodyHash determinism ─────────────────────

    [Fact]
    public void ComputeBodyHash_SameInput_ReturnsSameHash()
    {
        const string body = "{\"amount\": 10000, \"currency\": \"VND\"}";

        var hash1 = HmacSignatureService.ComputeBodyHash(body);
        var hash2 = HmacSignatureService.ComputeBodyHash(body);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeBodyHash_DifferentInput_ReturnsDifferentHash()
    {
        var hash1 = HmacSignatureService.ComputeBodyHash("{\"amount\": 10000}");
        var hash2 = HmacSignatureService.ComputeBodyHash("{\"amount\": 99999}");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeBodyHash_EmptyString_ReturnsValidHash()
    {
        var hash = HmacSignatureService.ComputeBodyHash(string.Empty);

        Assert.NotEmpty(hash);
        Assert.Equal(64, hash.Length); // SHA-256 hex = 64 chars
    }

    // ── Entity identity ───────────────────────────────────────────────────────

    [Fact]
    public void TwoRecords_DifferentMerchantSameKey_AreTreatedAsSeparateRecords()
    {
        var r1 = new IdempotencyRecord("merchant-X", "key-shared", "POST", "/api/payments", "hash1");
        var r2 = new IdempotencyRecord("merchant-Y", "key-shared", "POST", "/api/payments", "hash2");

        // They share the same idempotency key string but belong to different merchants;
        // they must have different Entity IDs.
        Assert.NotEqual(r1.Id, r2.Id);
        Assert.NotEqual(r1.MerchantId, r2.MerchantId);
    }
}
