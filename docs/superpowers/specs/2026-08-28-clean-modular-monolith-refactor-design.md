# Design Spec: Refactor Backend Mini Banking & Payment Gateway sang Clean Modular Monolith

**Ngày tạo:** 2026-08-28  
**Trạng thái:** Approved  
**Mục tiêu:** Tái cấu trúc (Refactor) toàn diện backend .NET 8 theo kiến trúc Clean Modular Monolith + Vertical Slice Architecture, dọn dẹp routing trong `Program.cs`, bẻ nhỏ các God Handlers, bổ sung Domain Services và MediatR Pipeline Behaviors.

---

## 1. Bối cảnh & Vấn đề cần giải quyết

### Hiện trạng codebase:
1. **Routing bị nhồi vào `Program.cs`:** Hơn 70 dòng route liên quan đến `payments` và `refunds` (đọc stream, parse header, bắt lỗi) bị viết trực tiếp vào `Program.cs`.
2. **God Handlers:** `CreatePaymentHandler.cs`, `CreateRefundHandler.cs`, `CreateSettlementHandler.cs` dài 150-200 dòng, ôm đồm quá nhiều trách nhiệm (tự băm SHA256, tự viết raw SQL `SELECT FOR UPDATE`, tự ghi ledger, tự serialize outbox message, tự quản lý transaction thủ công).
3. **Phá vỡ ranh giới Module:** Module `Payments` chọc trực tiếp vào bảng nội bộ của `Accounts` và `Ledger` thay vì gọi qua Contract/Domain Service.

---

## 2. Kiến trúc Mục tiêu (Target Architecture)

Hệ thống tuân thủ 4 bộ quy chuẩn kỹ thuật đã thiết lập:
1. `dotnet-modular-monolith-endpoints`: Mỗi module tự quản lý Endpoint trong thư mục `Endpoints/` của mình. `Program.cs` chỉ đăng ký theo extension method.
2. `dotnet-cqrs-clean-services`: Tách biệt Handler (Orchestrator) với Domain Services và Pipeline Behaviors.
3. `fintech-ledger-concurrency-engine`: Đảm bảo bất biến sổ cái kép (Nợ = Có) và khóa bi quan (`SELECT FOR UPDATE`).
4. `fintech-outbox-messaging-webhooks`: Transactional Outbox atomic với thay đổi tiền tệ.

---

## 3. Cấu trúc Thư mục Chi tiết

```text
be/src/MiniBanking.Api/
├── Program.cs                                  # Composition Root (DI + Pipeline + Module Map)
├── SharedKernel/
│   ├── Behaviors/
│   │   ├── ITransactionalRequest.cs           # Marker interface cho Command cần DB Transaction
│   │   └── TransactionBehavior.cs             # MediatR Pipeline tự động Begin/Commit/Rollback Transaction
│   ├── Contracts/
│   │   ├── IAccountLockService.cs             # Interface quản lý khóa dòng ví và trừ/cộng tiền
│   │   ├── ILedgerPostingService.cs           # Interface quản lý ghi sổ cái kép cân bằng
│   │   └── IIdempotencyService.cs             # Interface quản lý Idempotency & SHA256 payload hash
│   ├── ApiResponse.cs
│   ├── Money.cs
│   ├── Entity.cs
│   └── SystemAccountIds.cs
├── Modules/
│   ├── Accounts/
│   │   ├── Domain/ (BankingCustomer, WalletAccount, BalanceSnapshot)
│   │   ├── Application/Services/ (AccountLockService.cs)
│   │   └── Endpoints/ (AccountEndpoints.cs)
│   ├── Ledger/
│   │   ├── Domain/ (LedgerTransaction, LedgerEntry, Enums)
│   │   ├── Application/Services/ (LedgerPostingService.cs)
│   │   └── Endpoints/ (LedgerEndpoints.cs)
│   ├── Payments/
│   │   ├── Domain/ (Payment, Refund, Settlement, OutboxMessage, IdempotencyRecord)
│   │   ├── Application/
│   │   │   ├── Services/ (IdempotencyService.cs)
│   │   │   ├── CreatePayment.cs               # Handler ~35 dòng
│   │   │   ├── CreateRefund.cs                # Handler ~35 dòng
│   │   │   └── CreateSettlement.cs            # Handler ~35 dòng
│   │   └── Endpoints/
│   │       └── PaymentEndpoints.cs            # MapGroup /api/v1/merchant
│   ├── Merchants/
│   │   ├── Domain/ (Merchant)
│   │   └── Endpoints/ (MerchantEndpoints.cs)
│   └── Admin/
│       ├── Domain/ (AdminUser, AuditLog)
│       └── AdminEndpoints.cs
└── Infrastructure/
    ├── Persistence/ (MiniBankingDbContext, Migrations, DataSeeder)
    ├── Messaging/ (OutboxPublisher, WebhookConsumer, RabbitMqConnection)
    └── Security/ (HmacVerificationMiddleware, HmacSignatureService, JwtTokenService)
```

---

## 4. Đặc tả Kỹ thuật Từng Thành phần

### 4.1. SharedKernel & Pipeline Behaviors
- **`ITransactionalRequest`**: Marker interface áp dụng cho các Command ghi dữ liệu (`CreatePaymentCommand`, `CreateRefundCommand`, `CreateSettlementCommand`).
- **`TransactionBehavior<TRequest, TResponse>`**:
  - Tự động mở `BeginTransactionAsync()`.
  - Thực thi Handler qua `next()`.
  - Tự động gọi `SaveChangesAsync()` và `CommitAsync()`.
  - Rollback khi có Exception.

### 4.2. Domain & Application Services
- **`IAccountLockService` / `AccountLockService`**:
  - Thực hiện query `SELECT * FROM public.balance_snapshots WHERE "WalletAccountId" = @id FOR UPDATE`.
  - Gọi `balance.Debit(amount)` hoặc `balance.Credit(amount)`.
  - Kiểm tra tồn tại ví, tiền tệ hợp lệ, số dư khả dụng.
- **`ILedgerPostingService` / `LedgerPostingService`**:
  - Tạo `LedgerTransaction` với đúng kiểu (`Payment`, `Refund`, `Settlement`).
  - Thêm cặp bút toán Nợ (Debit) và Có (Credit).
  - Gọi `ValidateInvariant()` để đảm bảo $\sum \text{Debit} == \sum \text{Credit}$.
- **`IIdempotencyService` / `IdempotencyService`**:
  - Băm SHA-256 Request Body.
  - Kiểm tra record đã tồn tại: nếu đã hoàn thành trả về cached payload; nếu trùng key khác body hash thì báo lỗi gian lận (`InvalidOperationException`).
  - Cập nhật trạng thái `Complete` hoặc `Fail`.

### 4.3. Payments Module Handlers
- **`CreatePaymentHandler`**, **`CreateRefundHandler`**, **`CreateSettlementHandler`**:
  - Không còn chứa raw SQL hay thủ công transaction.
  - Nhận Command $\rightarrow$ Gọi `IIdempotencyService` $\rightarrow$ Gọi `IAccountLockService` $\rightarrow$ Gọi `ILedgerPostingService` $\rightarrow$ Thêm `Payment` và `OutboxMessage` $\rightarrow$ Hoàn tất.

### 4.4. Endpoints & Program.cs
- **`PaymentEndpoints.cs`**:
  - Khai báo routes `/api/v1/merchant/payments`, `/api/v1/merchant/refunds`, `/api/v1/merchant/settlements`.
  - Nhận HttpContext, lấy `MerchantId` và `IdempotencyKey` từ Items do `HmacVerificationMiddleware` cung cấp.
  - Gửi Command qua MediatR và format `ApiResponse.Ok` hoặc `ApiResponse.Fail`.
- **`Program.cs`**:
  - Xóa bỏ toàn bộ routing inline.
  - Đăng ký DI cho các Service mới (`IAccountLockService`, `ILedgerPostingService`, `IIdempotencyService`, `TransactionBehavior`).
  - Gọi `app.MapAdminEndpoints()`, `app.MapPaymentEndpoints()`, `app.MapAccountEndpoints()`, `app.MapLedgerEndpoints()`.

---

## 5. Kế hoạch Kiểm thử & Xác minh (Verification)

1. `dotnet build be/MiniBanking.sln`: Đảm bảo 0 warning / 0 error.
2. `dotnet test be/MiniBanking.sln`: Tất cả unit tests hiện tại tiếp tục pass.
3. Bổ sung thêm Unit Tests kiểm tra:
   - `LedgerPostingService` đảm bảo luôn cân bằng Nợ/Có.
   - `IdempotencyService` bắt đúng conflict khi trùng key khác body hash.
   - `TransactionBehavior` tự động commit khi thành công và rollback khi thất bại.
