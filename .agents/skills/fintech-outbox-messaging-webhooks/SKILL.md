---
name: fintech-outbox-messaging-webhooks
description: "Rules and design standards for Transactional Outbox pattern, RabbitMQ message dispatch, and resilient Webhook delivery with exponential backoff."
---

# Fintech Messaging: Transactional Outbox & Webhook Delivery Engine

Ensures 100% reliable event distribution between the Core Banking Database and External E-commerce/Merchant systems without two-phase commit (2PC).

## 1. The Transactional Outbox Pattern

### The Problem:
Publishing directly to RabbitMQ inside an active DB transaction risks:
- Message published, but DB transaction rolls back $\rightarrow$ Ghost event sent!
- DB transaction commits, but network to RabbitMQ fails $\rightarrow$ Event lost forever!

### The Outbox Solution:
1. When money moves, insert an `OutboxMessage` record **inside the exact same Database Transaction** as the ledger entries.
2. Commit DB transaction atomically. Both ledger changes and the outbox message are guaranteed to persist together.

```csharp
// Inside CreatePaymentHandler (Atomic Unit of Work)
var outboxMessage = new OutboxMessage(
    "PaymentSucceeded",
    JsonSerializer.Serialize(new
    {
        PaymentId = payment.Id,
        MerchantId = payment.MerchantId,
        Amount = payment.Amount,
        Timestamp = DateTime.UtcNow
    }));

_context.OutboxMessages.Add(outboxMessage);
await _context.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
```

## 2. Background Outbox Dispatcher

- An independent `BackgroundService` (`OutboxPublisher.cs`) polls pending messages with batching (`Take(100)`).
- Publishes messages to RabbitMQ Topic Exchange (`payments.exchange`) with persistent delivery mode.
- Marks messages as `ProcessedAt = UtcNow` upon successful RabbitMQ acknowledgment.

## 3. Resilient Webhook Delivery with Retry & DLQ

- **Webhook Consumer**: Consumes messages from RabbitMQ queues and issues HTTP POST to Merchant `WebhookUrl`.
- **Exponential Backoff Policy**:
  - Attempt 1: Immediate
  - Attempt 2: 1 minute
  - Attempt 3: 5 minutes
  - Attempt 4: 15 minutes
  - Attempt 5: 1 hour
- **Dead Letter Queue (DLQ)**:
  After 5 failed attempts, route the message to `webhooks.dlq` for administrative review and alerting. Never drop a financial notification silently.
- **HMAC Webhook Signing**: Outgoing webhook payloads must be signed using the Merchant's shared secret so merchants can verify payload authenticity.
