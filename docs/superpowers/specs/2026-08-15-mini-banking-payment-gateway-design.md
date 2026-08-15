# Mini Banking Ledger & Payment Gateway Backend

## Section 1: Scope Of Features

### Mục tiêu

Dự án là một backend .NET 8 độc lập, đóng vai trò ví nguồn tiền và payment gateway cho hệ thống E-commerce. E-commerce Backend chỉ tích hợp qua public APIs và webhook; toàn bộ dữ liệu tiền, ledger, balance, audit, idempotency và settlement thuộc sở hữu của Mini Banking.

Trọng tâm của v1 không phải nhiều màn hình hoặc nhiều tính năng CRUD. Trọng tâm là chứng minh năng lực backend quan trọng trong ngân hàng, fintech, payment, billing và marketplace:

- ACID transaction và data consistency.
- Double-entry bookkeeping.
- Idempotency chống duplicate payment/refund.
- Concurrency control khi nhiều giao dịch trừ cùng một ví.
- Outbox + message broker cho async consistency.
- Webhook retry và failure handling.
- HMAC signature, JWT/RBAC và audit trail.

### Ranh giới hệ thống

Mini Banking là nguồn sự thật về tiền.

E-commerce Backend sở hữu:

- Customer profile theo domain thương mại.
- Product, cart, checkout và order.
- Order payment status ở góc nhìn merchant.
- Mapping tới Mini Banking, ví dụ `bankingCustomerId` hoặc `walletAccountId`.

Mini Banking sở hữu:

- Banking customer.
- Wallet account.
- Available balance.
- Ledger transaction và ledger entries.
- Payment, refund, settlement.
- Merchant credentials.
- Webhook delivery.
- Audit logs.

E-commerce không đọc/ghi trực tiếp DB của Mini Banking. Mọi tương tác đi qua public merchant APIs hoặc webhook callbacks.

### Module 1: Ledger & Wallet Core

Module này là lõi ngân hàng của hệ thống.

Trách nhiệm chính:

- Quản lý `BankingCustomer`.
- Quản lý `WalletAccount`.
- Lưu số dư bằng minor unit `long`, currency mặc định `VND`.
- Duy trì `BalanceSnapshot` hoặc balance projection để query nhanh.
- Ghi nhận mọi biến động tiền bằng double-entry ledger.
- Đảm bảo invariant: mỗi ledger transaction phải cân bằng, tổng debit bằng tổng credit.

Các tài khoản ledger quan trọng:

- User wallet account: ví của người mua.
- Merchant settlement account: tài khoản nhận tiền của merchant.
- Platform clearing account: tài khoản trung gian của hệ thống.
- Platform fee account: nếu sau này muốn mô phỏng phí.

Các use case v1:

- Tạo banking customer và wallet account.
- Seed demo users/accounts để chạy demo nhanh.
- Nạp tiền giả lập bằng admin API.
- Xem balance.
- Xem lịch sử ledger của một wallet.

Điểm ăn tiền khi phỏng vấn:

- Không cập nhật số dư tùy tiện như CRUD.
- Mọi thay đổi tiền đều trace được về ledger transaction.
- Balance có thể rebuild từ ledger entries nếu cần reconciliation.
- Có test chứng minh ledger transaction luôn cân bằng.

### Module 2: Payment, Refund & Settlement

Module này mô phỏng vai trò payment gateway cho E-commerce.

Trách nhiệm chính:

- Nhận yêu cầu thanh toán từ E-commerce.
- Kiểm tra merchant credentials và request signature.
- Áp dụng idempotency key.
- Kiểm tra số dư ví.
- Lock account an toàn trong transaction.
- Ghi double-entry ledger cho payment/refund/settlement.
- Phát domain events ra outbox.

Public APIs v1:

- `POST /api/v1/merchant/payments`
- `GET /api/v1/merchant/payments/{paymentId}`
- `POST /api/v1/merchant/refunds`
- `GET /api/v1/merchant/refunds/{refundId}`

Payment flow v1 dùng Direct Debit:

1. E-commerce tạo order.
2. E-commerce gọi Mini Banking tạo payment.
3. Mini Banking verify HMAC signature.
4. Mini Banking kiểm tra idempotency key.
5. Mini Banking lock wallet account.
6. Mini Banking kiểm tra available balance.
7. Mini Banking ghi ledger entries để debit ví user và credit clearing account.
8. Mini Banking lưu payment status.
9. Mini Banking ghi outbox event `payment.succeeded` hoặc `payment.failed`.
10. Worker gửi webhook về E-commerce.

Refund flow:

1. E-commerce gọi refund API cho một payment đã thành công.
2. Mini Banking kiểm tra refund amount không vượt quá amount còn refund được.
3. Mini Banking áp dụng idempotency key riêng cho refund.
4. Mini Banking ghi ledger đảo chiều từ clearing/merchant về ví user.
5. Mini Banking ghi outbox event `refund.succeeded`.

Settlement flow:

1. Job hoặc admin command chọn các payment đủ điều kiện settle.
2. Mini Banking chuyển tiền từ clearing account sang merchant settlement account.
3. Mini Banking ghi settlement batch.
4. Mini Banking phát event `settlement.completed`.

Điểm ăn tiền khi phỏng vấn:

- Payment không chỉ là đổi status order.
- Refund không chỉ là update một dòng payment.
- Settlement thể hiện hiểu biết về clearing money trước khi merchant nhận tiền.
- Có thể giải thích failure mode khi payment thành công nhưng webhook thất bại.

### Module 3: Merchant API & Webhook Delivery

Module này thể hiện năng lực tích hợp hệ thống bên ngoài.

Trách nhiệm chính:

- Quản lý merchant app, API key và HMAC secret.
- Verify HMAC signature cho public merchant APIs.
- Kiểm tra timestamp để chống replay attack.
- Kiểm tra nonce/idempotency key để chống duplicate request.
- Gửi webhook callbacks về E-commerce.
- Retry webhook khi E-commerce tạm thời lỗi.
- Ghi lại webhook attempts để audit và debug.

Merchant API security:

- Mỗi merchant có `merchantId`, `apiKey`, `secret`.
- Request gửi kèm `X-Merchant-Id`, `X-Api-Key`, `X-Timestamp`, `X-Nonce`, `X-Signature`, `Idempotency-Key`.
- Signature được tính từ method, path, body hash, timestamp, nonce và idempotency key.
- Request quá thời gian cho phép bị từ chối.
- Nonce đã dùng rồi bị từ chối.
- Failed signature vẫn được audit.

Webhook delivery:

- Webhook không gửi trực tiếp trong DB transaction chính.
- Payment/refund/settlement ghi event vào outbox.
- Worker publish event sang RabbitMQ.
- Webhook worker consume message và gọi endpoint của E-commerce.
- Retry theo exponential backoff.
- Sau số lần retry tối đa, đánh dấu failed hoặc đưa vào DLQ.

Webhook events v1:

- `payment.succeeded`
- `payment.failed`
- `refund.succeeded`
- `settlement.completed`

Điểm ăn tiền khi phỏng vấn:

- Tách transaction xử lý tiền khỏi network call ra bên ngoài.
- Có retry nhưng vẫn giữ idempotency.
- Có audit từng lần gửi webhook.
- Có thể demo case E-commerce callback fail rồi retry thành công.

### Module 4: Security, Audit & Operations

Module này làm hệ thống trông giống một backend nghiêm túc thay vì demo CRUD.

Trách nhiệm chính:

- JWT authentication cho admin/internal APIs.
- RBAC cho các quyền tối thiểu.
- Audit logging cho hành động nhạy cảm.
- Structured logging bằng Serilog.
- Correlation ID xuyên suốt request, DB transaction, outbox, RabbitMQ và webhook.
- OpenTelemetry traces/metrics ở mức cơ bản.

Admin/internal APIs v1:

- Tạo banking customer.
- Tạo/link wallet account.
- Nạp tiền giả lập.
- Xem balance.
- Xem ledger history.
- Xem audit logs.
- Xem webhook attempts.

Audit events quan trọng:

- Admin tạo ví.
- Admin nạp tiền giả lập.
- Merchant gọi payment/refund.
- Signature verification failed.
- Payment succeeded/failed.
- Refund succeeded/failed.
- Settlement completed/failed.
- Webhook delivery succeeded/failed.

Điểm ăn tiền khi phỏng vấn:

- Có thể truy vết ai làm gì, lúc nào, request nào.
- Có correlation ID để debug flow bất đồng bộ.
- Có log/audit riêng cho sự kiện bảo mật.
- Có boundary rõ giữa admin API và merchant API.

### Ngoài Scope V1

Các phần sau không làm trong v1 để tránh loãng scope:

- Frontend lớn.
- Mobile wallet app.
- Multi-currency và FX.
- Interbank transfer thật.
- Card tokenization thật.
- PCI-DSS thật.
- AML/KYC thật.
- Core banking đầy đủ như loan, deposit, interest.
- Authorize/Capture ngay từ đầu.

Authorize/Capture có thể là milestone sau khi Direct Debit, refund, settlement, idempotency và concurrency đã chắc.

### Success Criteria Cho Section 1

Khi hoàn thành v1, dự án phải demo được các kịch bản sau:

- E-commerce gọi payment API và nhận kết quả thanh toán.
- Payment thành công tạo đúng double-entry ledger entries.
- Gửi cùng một idempotency key nhiều lần không tạo duplicate ledger.
- Hai payment đồng thời trên cùng wallet không làm âm tiền sai.
- Refund tạo ledger đảo chiều đúng.
- Settlement chuyển tiền từ clearing sang merchant settlement account.
- Webhook fail tạm thời rồi retry thành công.
- Failed signature bị từ chối và có audit log.
- Có logs/traces đủ để lần theo toàn bộ payment flow.

## Section 2: Architecture & Tech Stack

### Kiến trúc tổng thể

Hệ thống dùng mô hình Modular Monolith trong một monorepo.

Điều này có nghĩa là:

- Một Git repository.
- Một .NET solution.
- Một deployable chính là ASP.NET Core API.
- Nhiều module nội bộ được tách theo business capability.
- Một PostgreSQL database là source of truth.
- Một Docker Compose stack cho local development và demo.

V1 không dùng microservices. Lý do là mục tiêu portfolio cần chứng minh chiều sâu về money movement, ledger, consistency, idempotency, concurrency và audit. Nếu tách microservices quá sớm, dự án dễ mất trọng tâm vào network, service discovery và deployment ceremony.

Tuy vậy, module boundary, event contracts và outbox sẽ được thiết kế đủ sạch để sau này có thể tách Payments, Ledger hoặc Webhooks thành service riêng.

### Runtime & Framework

Runtime chính:

- .NET 8.
- ASP.NET Core Web API.
- C#.

Lý do chọn .NET 8:

- Phổ biến trong môi trường doanh nghiệp.
- Dễ gặp trong job backend .NET hiện tại.
- Tài liệu, thư viện và ecosystem ổn định.
- Phù hợp mục tiêu phỏng vấn hơn là chạy theo version mới nhất.

### Project Structure

Solution đề xuất:

```text
src/
  MiniBanking.Api/
  MiniBanking.Modules.Accounts/
  MiniBanking.Modules.Ledger/
  MiniBanking.Modules.Payments/
  MiniBanking.Modules.Merchants/
  MiniBanking.Modules.Webhooks/
  MiniBanking.Modules.Audit/
  MiniBanking.SharedKernel/
  MiniBanking.Infrastructure/

tests/
  MiniBanking.UnitTests/
  MiniBanking.IntegrationTests/
  MiniBanking.ConcurrencyTests/
```

Vai trò từng project:

- `MiniBanking.Api`: HTTP endpoints, authentication middleware, request pipeline, OpenAPI.
- `MiniBanking.Modules.Accounts`: customer, wallet account, balance query.
- `MiniBanking.Modules.Ledger`: ledger transaction, ledger entries, balance invariant.
- `MiniBanking.Modules.Payments`: payment, refund, settlement orchestration.
- `MiniBanking.Modules.Merchants`: merchant credentials, HMAC verification, API access rules.
- `MiniBanking.Modules.Webhooks`: webhook subscription, delivery, retry attempts.
- `MiniBanking.Modules.Audit`: audit events, security events, operational history.
- `MiniBanking.SharedKernel`: shared domain primitives such as `Money`, `Entity`, `DomainEvent`, `Result`.
- `MiniBanking.Infrastructure`: EF Core, PostgreSQL, Redis, RabbitMQ, background workers, Serilog, OpenTelemetry.

### Application Pattern

Pattern chính:

- Clean Architecture nhẹ theo module.
- CQRS cho các use case quan trọng.
- MediatR cho command/query dispatching.
- Domain events cho business events nội bộ.
- Transactional Outbox cho event publishing.

Các command quan trọng:

- `CreateBankingCustomerCommand`
- `CreateWalletAccountCommand`
- `TopUpWalletCommand`
- `CreatePaymentCommand`
- `CreateRefundCommand`
- `SettleMerchantPaymentsCommand`
- `DeliverWebhookCommand`

Các query quan trọng:

- `GetWalletBalanceQuery`
- `GetLedgerHistoryQuery`
- `GetPaymentStatusQuery`
- `GetRefundStatusQuery`
- `GetWebhookAttemptsQuery`
- `GetAuditLogsQuery`

Nguyên tắc quan trọng:

- Payments không tự sửa balance trực tiếp.
- Payments phải đi qua Ledger contract để ghi money movement.
- Ledger chịu trách nhiệm đảm bảo double-entry invariant.
- Webhook không được gửi trực tiếp trong transaction xử lý payment.
- Audit/security events phải được ghi kể cả khi request bị từ chối.

### Database

Database chính:

- PostgreSQL.
- EF Core cho phần lớn persistence.
- Raw SQL có kiểm soát cho các đoạn concurrency-critical như row lock.

Các nhóm bảng chính:

- `banking_customers`
- `wallet_accounts`
- `balance_snapshots`
- `ledger_transactions`
- `ledger_entries`
- `payments`
- `refunds`
- `settlements`
- `merchants`
- `idempotency_records`
- `outbox_messages`
- `webhook_endpoints`
- `webhook_attempts`
- `audit_logs`

PostgreSQL là source of truth cho tiền. Redis và RabbitMQ chỉ hỗ trợ tốc độ, bảo vệ hoặc xử lý bất đồng bộ; không thay thế ledger.

### Money Representation

V1 dùng single currency:

- Currency mặc định: `VND`.
- Amount lưu bằng minor unit `long`.
- Ví dụ `100000` nghĩa là 100,000 VND.

Lý do:

- Tránh lỗi floating point.
- Dễ test ledger.
- Dễ query và so sánh.
- Vẫn có cột `currency` để mở rộng multi-currency sau.

### Redis

Redis dùng cho các dữ liệu ngắn hạn và tốc độ cao:

- Rate limiting merchant API.
- Nonce replay protection.
- Cache trạng thái ngắn hạn nếu cần.
- Optional distributed lock cho job như settlement batch.

Redis không dùng làm source of truth cho balance.

Với trừ tiền ví, cơ chế chính vẫn là PostgreSQL transaction và row-level locking. Redis lock chỉ là công cụ phụ cho một số background jobs hoặc scale-out scenario.

### RabbitMQ

RabbitMQ dùng để xử lý message bất đồng bộ:

- Payment event.
- Refund event.
- Settlement event.
- Webhook delivery command.
- Retry hoặc DLQ cho các message lỗi nhiều lần.

RabbitMQ giúp tách xử lý business transaction khỏi network call bên ngoài. Payment có thể thành công ngay cả khi E-commerce webhook endpoint đang tạm thời lỗi.

### Transactional Outbox

Transactional Outbox được dùng để tránh lỗi dual-write giữa database và message broker.

Thay vì vừa commit DB vừa publish RabbitMQ trong cùng request theo kiểu rời rạc, hệ thống sẽ ghi outbox message vào PostgreSQL trong cùng transaction với business data.

Ví dụ payment thành công:

```text
DB transaction:
  - insert/update payment succeeded
  - insert ledger transaction
  - insert ledger entries
  - update balance snapshot
  - insert outbox_messages(payment.succeeded)
commit
```

Sau đó `OutboxPublisherWorker` đọc bảng `outbox_messages`, publish sang RabbitMQ và đánh dấu message đã publish.

Nếu API crash sau khi commit DB nhưng trước khi publish, outbox row vẫn còn trong DB. Worker chạy lại sẽ publish tiếp.

### Background Workers

Các worker v1:

- `OutboxPublisherWorker`: đọc outbox DB và publish RabbitMQ.
- `WebhookDeliveryWorker`: consume event và gọi webhook E-commerce.
- `SettlementWorker`: chạy settlement batch theo command hoặc schedule.
- `RetryWorker` hoặc retry mechanism tích hợp trong webhook worker.

Worker phải có:

- Correlation ID.
- Retry policy.
- Error logging.
- Audit/webhook attempt record.
- Idempotency khi xử lý lại message.

### Security

Merchant APIs dùng HMAC signature:

- `X-Merchant-Id`
- `X-Api-Key`
- `X-Timestamp`
- `X-Nonce`
- `Idempotency-Key`
- `X-Signature`

Mini Banking verify:

- Merchant tồn tại và active.
- API key hợp lệ.
- Timestamp còn trong allowed window.
- Nonce chưa bị dùng lại.
- Signature khớp với method, path, body hash, timestamp, nonce và idempotency key.

Admin/internal APIs dùng:

- JWT authentication.
- RBAC.
- Role tối thiểu như `Admin`, `FinanceOps`, `Auditor`.

### Observability

Observability stack:

- Serilog structured logs.
- OpenTelemetry traces.
- Correlation ID middleware.
- Health checks cho PostgreSQL, Redis, RabbitMQ.
- Optional Jaeger/Grafana trong Docker Compose.

Mỗi payment flow phải trace được:

```text
HTTP request
  -> HMAC verification
  -> idempotency check
  -> DB transaction
  -> ledger write
  -> outbox insert
  -> RabbitMQ publish
  -> webhook delivery
```

### Local Development Stack

Docker Compose v1:

- MiniBanking API.
- PostgreSQL.
- Redis.
- RabbitMQ Management UI.
- Optional Jaeger hoặc Grafana stack.

Repo nên có:

- `.env.example`
- `docker-compose.yml`
- database migration commands
- seed demo command
- README demo script
- Postman/HTTP collection hoặc `.http` files

### Testing Strategy

Test projects:

- Unit tests cho domain logic và ledger invariants.
- Integration tests với PostgreSQL test container.
- Concurrency tests cho race condition khi nhiều payment trừ cùng wallet.
- Idempotency tests cho duplicate payment/refund.
- Webhook retry tests.
- HMAC verification tests.

Các test ăn điểm:

- Same `Idempotency-Key` gửi nhiều lần chỉ tạo một payment.
- Hai payment đồng thời trên cùng account không làm balance âm sai.
- Ledger transaction luôn có tổng debit bằng tổng credit.
- Payment succeeded nhưng RabbitMQ publish bị delay vẫn có outbox row để xử lý lại.
- Webhook fail tạm thời rồi retry thành công.

## Section 3: E-commerce Integration Flow

### Mục tiêu tích hợp

Mini Banking là một backend độc lập. E-commerce Backend tích hợp như một merchant bên ngoài thông qua public merchant APIs và webhook callbacks.

Nguyên tắc tích hợp:

- E-commerce không đọc/ghi trực tiếp database của Mini Banking.
- E-commerce không tự giữ source of truth về số dư ví.
- Mini Banking không sở hữu order/cart/product của E-commerce.
- Hai hệ thống trao đổi bằng API contract, idempotency key và webhook event.

### Luồng thanh toán tổng thể

```text
E-commerce Backend
  -> tạo order PENDING_PAYMENT
  -> gọi Mini Banking payment API

Mini Banking API
  -> verify HMAC signature
  -> check timestamp, nonce, rate limit
  -> check idempotency key
  -> lock wallet account
  -> check available balance
  -> write payment
  -> write double-entry ledger
  -> write outbox message
  -> return payment result

Outbox Publisher
  -> publish payment event to RabbitMQ

Webhook Worker
  -> call E-commerce callback endpoint

E-commerce Backend
  -> update order PAID or PAYMENT_FAILED
```

### Payment API

Endpoint:

```http
POST /api/v1/merchant/payments
```

Headers:

```http
X-Merchant-Id: ecommerce-demo
X-Api-Key: merchant-api-key
X-Timestamp: 2026-08-15T10:00:00Z
X-Nonce: unique-random-value
Idempotency-Key: order-123-payment
X-Signature: hmac-sha256-signature
```

Request body:

```json
{
  "merchantOrderId": "ORDER-123",
  "walletAccountId": "wallet_user_001",
  "amount": 250000,
  "currency": "VND",
  "description": "Payment for order ORDER-123",
  "callbackUrl": "https://ecommerce.local/api/payment-callbacks"
}
```

Successful response:

```json
{
  "paymentId": "pay_01H...",
  "merchantOrderId": "ORDER-123",
  "status": "Succeeded",
  "amount": 250000,
  "currency": "VND"
}
```

Failed response when insufficient balance:

```json
{
  "paymentId": "pay_01H...",
  "merchantOrderId": "ORDER-123",
  "status": "Failed",
  "failureCode": "INSUFFICIENT_FUNDS",
  "message": "Wallet balance is not enough for this payment."
}
```

### E-commerce Order State Mapping

E-commerce chỉ cần lưu trạng thái thanh toán ở góc nhìn order:

```text
PENDING_PAYMENT
  -> PAID
  -> PAYMENT_FAILED
  -> REFUND_PENDING
  -> REFUNDED
```

Mapping đề xuất:

- Mini Banking `Payment.Succeeded` -> E-commerce `Order.PAID`.
- Mini Banking `Payment.Failed` -> E-commerce `Order.PAYMENT_FAILED`.
- Mini Banking `Refund.Succeeded` -> E-commerce `Order.REFUNDED` hoặc cập nhật refunded amount.

E-commerce nên lưu:

- `merchantOrderId`
- `paymentId`
- `paymentStatus`
- `paidAmount`
- `refundedAmount`
- `lastPaymentEventId`

E-commerce không nên lưu:

- Ledger entries.
- Balance thật.
- Merchant settlement state chi tiết.

### Webhook Callback

Webhook event từ Mini Banking về E-commerce:

```json
{
  "eventId": "evt_01H...",
  "eventType": "payment.succeeded",
  "paymentId": "pay_01H...",
  "merchantOrderId": "ORDER-123",
  "amount": 250000,
  "currency": "VND",
  "occurredAt": "2026-08-15T10:00:00Z"
}
```

Webhook cũng nên được ký bằng HMAC để E-commerce xác minh event thật sự đến từ Mini Banking.

Webhook headers:

```http
X-Webhook-Event-Id: evt_01H...
X-Webhook-Timestamp: 2026-08-15T10:00:00Z
X-Webhook-Signature: hmac-sha256-signature
```

E-commerce cần xử lý webhook theo idempotency:

- Nếu `eventId` chưa xử lý, update order.
- Nếu `eventId` đã xử lý, trả `200 OK` nhưng không update lại.
- Không trả lỗi cho duplicate event đã xử lý thành công.

Lý do: webhook có thể retry nhiều lần. Receiver phải idempotent.

### Refund API

Endpoint:

```http
POST /api/v1/merchant/refunds
```

Request body:

```json
{
  "paymentId": "pay_01H...",
  "merchantRefundId": "ORDER-123-REFUND-1",
  "amount": 250000,
  "currency": "VND",
  "reason": "Customer requested refund"
}
```

Nguyên tắc:

- Refund phải tham chiếu payment đã succeeded.
- Tổng refund amount không được vượt quá captured/paid amount.
- Refund dùng idempotency key riêng.
- Refund tạo ledger transaction đảo chiều.
- Refund thành công phát webhook `refund.succeeded`.

### Query Payment Status

Endpoint:

```http
GET /api/v1/merchant/payments/{paymentId}
```

Mục đích:

- E-commerce có thể query trạng thái nếu webhook chậm.
- Hỗ trợ reconciliation.
- Hỗ trợ debug khi callback fail.

E-commerce không nên polling liên tục. Query status là fallback, còn luồng chính vẫn là webhook.

### Failure Cases Cần Demo

Case 1: Duplicate payment request.

```text
E-commerce gửi cùng Idempotency-Key 3 lần
  -> Mini Banking trả cùng payment result
  -> chỉ có một payment
  -> chỉ có một ledger transaction
```

Case 2: Payment succeeded nhưng webhook fail.

```text
Mini Banking đã commit payment + ledger + outbox
  -> E-commerce callback endpoint trả 500
  -> webhook worker retry
  -> E-commerce hồi phục
  -> webhook thành công
```

Case 3: E-commerce nhận duplicate webhook.

```text
Mini Banking retry cùng eventId
  -> E-commerce thấy eventId đã xử lý
  -> trả 200 OK
  -> order không bị update sai lần hai
```

Case 4: Payment request bị giả mạo.

```text
Request sai HMAC signature
  -> Mini Banking reject 401/403
  -> không tạo payment
  -> không tạo ledger
  -> ghi audit log security event
```

### Contract Boundary Quan Trọng

Mini Banking public contract phải ổn định:

- Payment API nhận `merchantOrderId`, không cần biết toàn bộ order details.
- Payment API nhận `walletAccountId`, không cần biết cart/product.
- Webhook trả `merchantOrderId` để E-commerce map về order nội bộ.
- Payment/refund status là enum rõ ràng.
- Error code phải machine-readable, ví dụ `INSUFFICIENT_FUNDS`, `INVALID_SIGNATURE`, `DUPLICATE_NONCE`.

Thiết kế này giúp E-commerce có thể đổi domain nội bộ mà không ảnh hưởng Mini Banking, và Mini Banking có thể nâng cấp ledger/payment logic mà không làm vỡ checkout flow.

## Section 4: Bài Toán Kỹ Thuật Ăn Điểm

### 1. Double-entry Bookkeeping

Mọi biến động tiền phải đi qua ledger kép. Không có chuyện payment chỉ update `wallet_accounts.balance` rồi đổi status.

Mô hình dữ liệu lõi:

- `ledger_transactions`: header của một nghiệp vụ tiền.
- `ledger_entries`: từng dòng debit/credit trong transaction.
- `balance_snapshots`: projection để query nhanh số dư hiện tại.

Invariant bắt buộc:

```text
sum(debit entries) = sum(credit entries)
```

Ví dụ user thanh toán 250,000 VND:

```text
Ledger transaction: PAYMENT pay_123

Debit:
  User wallet account               250000

Credit:
  Platform clearing account         250000
```

Ví dụ refund 250,000 VND:

```text
Ledger transaction: REFUND ref_123

Debit:
  Platform clearing account         250000

Credit:
  User wallet account               250000
```

Ví dụ settlement cho merchant:

```text
Ledger transaction: SETTLEMENT stl_123

Debit:
  Platform clearing account         250000

Credit:
  Merchant settlement account       250000
```

Điểm cần nói khi phỏng vấn:

- Ledger là lịch sử bất biến của tiền.
- Balance là projection để đọc nhanh, không phải nguồn sự thật duy nhất.
- Nếu balance snapshot lệch, có thể rebuild/reconcile từ ledger entries.
- Ledger transaction phải reject nếu không cân bằng.

Test cần có:

- Tạo payment thì ledger có đúng debit/credit.
- Tạo refund thì ledger đảo chiều đúng.
- Tạo settlement thì clearing giảm, merchant settlement tăng.
- Cố tạo ledger không cân bằng thì bị reject.

### 2. Idempotency Key

Payment gateway phải chống duplicate request. Trong thực tế, E-commerce có thể retry vì timeout, user double-click, gateway chậm hoặc network lỗi.

Merchant gửi:

```http
Idempotency-Key: order-123-payment
```

Mini Banking lưu `idempotency_records` theo key:

- `merchant_id`
- `idempotency_key`
- `request_method`
- `request_path`
- `request_body_hash`
- `status`
- `response_payload`
- `locked_until` hoặc trạng thái `Processing`

Behavior:

- Cùng merchant + cùng endpoint + cùng key + cùng body: trả lại response cũ.
- Cùng key nhưng body khác: reject `409 IDEMPOTENCY_CONFLICT`.
- Request đầu đang xử lý: request sau có thể chờ ngắn hoặc trả `409 PROCESSING`.
- Payment/refund chỉ được tạo một lần.
- Ledger transaction chỉ được tạo một lần.

Điểm cần nói khi phỏng vấn:

- Idempotency không chỉ là unique constraint trên payment.
- Phải lưu request hash để phát hiện cùng key nhưng payload khác.
- Phải lưu response để retry trả cùng kết quả.
- Idempotency phải bao quanh cả payment và ledger write.

Test cần có:

- Gửi cùng payment request 3 lần chỉ tạo một payment.
- Gửi cùng key nhưng amount khác bị reject.
- Retry sau timeout trả lại kết quả payment đã commit.

### 3. Concurrency Control

Bài toán:

```text
Wallet balance = 300000

Request A debit 250000
Request B debit 250000
```

Nếu xử lý sai, cả hai request cùng đọc balance 300000 và đều thành công, làm hệ thống âm tiền.

V1 dùng PostgreSQL pessimistic locking cho debit path:

```sql
SELECT *
FROM wallet_accounts
WHERE id = @walletAccountId
FOR UPDATE;
```

Trong cùng transaction:

1. Lock wallet account row.
2. Đọc balance hiện tại.
3. Kiểm tra đủ tiền.
4. Ghi ledger transaction.
5. Ghi ledger entries.
6. Cập nhật balance snapshot.
7. Ghi payment status.
8. Ghi outbox message.
9. Commit.

Vì row bị lock, request thứ hai phải chờ request thứ nhất commit/rollback rồi mới đọc số dư mới.

Trade-off:

- Pessimistic lock dễ hiểu, rất hợp debit money path.
- Optimistic concurrency cũng tốt, nhưng phải retry khi version conflict.
- Redis distributed lock không thay thế DB transaction cho tiền.

Deadlock handling:

- Luôn lock account theo thứ tự ổn định nếu một transaction đụng nhiều account.
- Giữ transaction ngắn.
- Không gọi network trong DB transaction.
- Nếu PostgreSQL báo deadlock/serialization failure, retry có giới hạn cho command an toàn.

Test cần có:

- Hai payment đồng thời, chỉ một thành công nếu balance không đủ cho cả hai.
- Nhiều request song song không làm balance âm.
- Ledger vẫn cân bằng sau concurrency test.

### 4. Outbox Pattern Và Saga-lite

Bài toán dual-write:

```text
Commit DB payment succeeded
Publish RabbitMQ payment.succeeded
```

Nếu app crash sau khi commit DB nhưng trước khi publish RabbitMQ, hệ thống sẽ có payment thành công nhưng không có event/webhook.

Transactional Outbox giải quyết:

```text
Same DB transaction:
  - payment succeeded
  - ledger entries
  - balance snapshot update
  - outbox_messages(payment.succeeded)
commit
```

Sau commit:

```text
OutboxPublisherWorker
  -> read unpublished outbox messages
  -> publish to RabbitMQ
  -> mark as published
```

Nếu publish lỗi, outbox row vẫn còn để retry.

Saga-lite trong v1:

- Payment local transaction là atomic trong PostgreSQL.
- Webhook delivery là bước async có retry.
- Refund là compensating action nghiệp vụ, không phải rollback DB transaction cũ.
- Settlement là process riêng gom payment đã eligible.

Điểm cần nói khi phỏng vấn:

- Không rollback ledger đã commit chỉ vì webhook fail.
- Failure bên ngoài được xử lý bằng retry/compensation, không làm mất lịch sử tiền.
- Outbox tránh mất event khi app crash.
- Saga-lite đủ cho portfolio v1; chưa cần orchestration engine phức tạp.

Test cần có:

- Payment commit xong có outbox message.
- Worker publish lỗi thì message vẫn pending.
- Worker chạy lại publish thành công.
- Webhook fail rồi retry thành công.

### 5. Security Và Audit Trail

Merchant API dùng HMAC signature để xác minh request từ E-commerce.

Admin/internal API dùng JWT/RBAC:

- `Admin`: quản lý user/wallet demo.
- `FinanceOps`: top-up giả lập, settlement.
- `Auditor`: xem ledger/audit, không sửa dữ liệu.

Audit log cần ghi:

- Actor: admin user hoặc merchant.
- Action: payment requested, refund requested, top-up, settlement, failed signature.
- Resource: payment id, wallet id, merchant order id.
- Result: succeeded, failed, rejected.
- Correlation ID.
- IP/user agent nếu có.
- Timestamp.

Security cases cần demo:

- Sai HMAC signature bị reject.
- Timestamp quá cũ bị reject.
- Nonce dùng lại bị reject.
- User thiếu role không gọi được admin top-up.
- Failed signature vẫn có audit log.

Điểm cần nói khi phỏng vấn:

- Audit log không phải application log thông thường.
- Audit log là business/security trail có cấu trúc.
- Không lộ HMAC secret trong log.
- Không log raw sensitive payload nếu không cần.

## Section 5: Milestones Triển Khai

### Milestone 1: Foundation

Mục tiêu:

- Dựng nền .NET solution và local development stack.
- Chưa xử lý nghiệp vụ tiền phức tạp.
- Tạo bộ khung đủ sạch để các milestone sau không phải sửa kiến trúc nhiều.

Deliverables:

- `.NET 8` solution.
- ASP.NET Core Web API project.
- Module projects theo boundary đã chốt.
- PostgreSQL connection.
- EF Core migration baseline.
- Docker Compose cho API, PostgreSQL, Redis, RabbitMQ.
- Serilog structured logging.
- OpenAPI/Swagger.
- Health checks cho API, PostgreSQL, Redis, RabbitMQ.
- `.env.example`.
- README local setup ban đầu.

Demo cuối milestone:

- Chạy `docker compose up`.
- API start thành công.
- Health endpoint báo healthy.
- Swagger mở được.
- PostgreSQL/Redis/RabbitMQ kết nối được.

### Milestone 2: Ledger & Wallet Core

Mục tiêu:

- Xây lõi tiền trước khi làm payment.
- Chứng minh hệ thống không phải CRUD balance đơn giản.

Deliverables:

- `BankingCustomer`.
- `WalletAccount`.
- `LedgerTransaction`.
- `LedgerEntry`.
- `BalanceSnapshot`.
- Admin API tạo customer/wallet.
- Admin API top-up giả lập.
- Query balance.
- Query ledger history.
- Domain validation cho double-entry invariant.
- Unit tests cho ledger.
- Integration tests với PostgreSQL.

Demo cuối milestone:

```text
1. Tạo banking customer.
2. Tạo wallet account.
3. Top-up 500,000 VND.
4. Query balance thấy 500,000 VND.
5. Query ledger history thấy debit/credit cân bằng.
```

### Milestone 3: Merchant Payment API

Mục tiêu:

- E-commerce Backend có thể gọi public API để thanh toán order.
- Tập trung vào HMAC, idempotency và concurrency-safe debit.

Deliverables:

- Merchant entity và API credentials.
- HMAC verification middleware/filter.
- Timestamp validation.
- Nonce replay protection bằng Redis.
- Rate limiting cơ bản bằng Redis.
- `IdempotencyRecord`.
- `POST /api/v1/merchant/payments`.
- `GET /api/v1/merchant/payments/{paymentId}`.
- Payment direct debit flow.
- PostgreSQL row-level lock cho wallet debit path.
- Payment status model.
- Tests cho HMAC.
- Tests cho idempotency.
- Concurrency test cơ bản.

Demo cuối milestone:

```text
1. Merchant gọi payment API đúng HMAC.
2. Wallet bị trừ tiền đúng.
3. Ledger payment cân bằng.
4. Gửi lại cùng Idempotency-Key không bị trừ lần hai.
5. Hai payment đồng thời không làm âm tiền sai.
6. Sai signature bị reject và có audit log.
```

### Milestone 4: Refund & Settlement

Mục tiêu:

- Làm payment gateway trông thật hơn.
- Thể hiện hiểu biết về reverse money movement và merchant settlement.

Deliverables:

- `Refund`.
- `Settlement`.
- `SettlementBatch`.
- `POST /api/v1/merchant/refunds`.
- `GET /api/v1/merchant/refunds/{refundId}`.
- Full refund.
- Partial refund nếu không làm tăng scope quá nhiều.
- Refund idempotency.
- Settlement command/job.
- Query merchant settlement balance.
- Ledger entries cho refund và settlement.
- Tests cho refund over amount.
- Tests cho settlement ledger.

Demo cuối milestone:

```text
1. Payment thành công 250,000 VND.
2. Refund 100,000 VND.
3. Wallet user nhận lại 100,000 VND.
4. Ledger refund đảo chiều đúng.
5. Settlement phần còn lại sang merchant settlement account.
6. Clearing account giảm đúng.
```

### Milestone 5: Outbox, RabbitMQ & Webhooks

Mục tiêu:

- Tách transaction tiền khỏi network call bên ngoài.
- Chứng minh async consistency và failure handling.

Deliverables:

- `OutboxMessage`.
- `OutboxPublisherWorker`.
- RabbitMQ exchange/queue setup.
- `WebhookEndpoint`.
- `WebhookAttempt`.
- `WebhookDeliveryWorker`.
- Webhook HMAC signing.
- Retry policy.
- Failed/DLQ state.
- Webhook event contracts:
  - `payment.succeeded`
  - `payment.failed`
  - `refund.succeeded`
  - `settlement.completed`
- Tests cho outbox.
- Tests cho webhook retry.

Demo cuối milestone:

```text
1. Payment thành công tạo outbox message.
2. Outbox worker publish RabbitMQ.
3. Webhook worker callback E-commerce endpoint.
4. Cho endpoint trả 500.
5. Worker retry.
6. Endpoint hồi phục.
7. Webhook attempt chuyển sang succeeded.
```

### Milestone 6: Interview-grade Hardening

Mục tiêu:

- Làm dự án đủ sắc để đưa vào CV, GitHub và phỏng vấn.
- Tập trung vào bằng chứng chạy được và tài liệu giải thích trade-off.

Deliverables:

- JWT authentication cho admin APIs.
- RBAC roles: `Admin`, `FinanceOps`, `Auditor`.
- Audit log hoàn chỉnh.
- OpenTelemetry traces.
- Correlation ID middleware.
- Concurrency test suite.
- Failure simulation tests.
- README architecture explanation.
- README demo script.
- API collection bằng `.http` files hoặc Postman collection.
- Diagram luồng payment/outbox/webhook.
- Docker Compose profile cho full demo.

Demo cuối milestone:

```text
1. Seed user wallet with 500,000 VND.
2. Create E-commerce order ORDER-123.
3. Call payment API for 250,000 VND.
4. Verify payment succeeded.
5. Verify ledger debit/credit balanced.
6. Retry same idempotency key and verify no duplicate charge.
7. Run concurrent payments and verify no negative balance.
8. Force webhook failure and verify retry.
9. Refund payment.
10. Settle merchant balance.
11. Inspect audit logs and traces for the whole flow.
```

### Milestone Priorities

Nếu thời gian hạn chế, ưu tiên theo thứ tự:

1. Ledger & Wallet Core.
2. Payment API với idempotency.
3. Concurrency-safe debit.
4. Outbox + webhook retry.
5. Refund.
6. Settlement.
7. Observability và polish.

Không nên làm frontend trước khi các phần trên chạy chắc. Một README tốt, Swagger, `.http` files và logs/traces rõ ràng đủ để demo backend portfolio.
