# Naming Conventions

Quy chuẩn đặt tên (Naming Conventions) thống nhất cho toàn bộ hệ thống Mini Banking & Payment Gateway (.NET 8).

---

## 1. Cấu trúc Thư mục & File theo Tầng (Layer & File Conventions)

Mọi module trong `Modules/{ModuleName}/` phải tuân thủ nghiêm ngặt cấu trúc 4 tầng con:

```text
Modules/{ModuleName}/
├── Domain/
│   ├── Entities/             # PascalCase.cs (Danh từ số ít)
│   └── Services/             # I{Name}Service.cs và {Name}Service.cs (Domain Services)
├── Application/
│   ├── {FeatureName}/        # Gom nhóm theo Feature / Use-case (Vertical Slice)
│   │   ├── {Verb}{Noun}Command.cs
│   │   ├── {Verb}{Noun}Handler.cs
│   │   ├── {Verb}{Noun}Request.cs
│   │   └── {Verb}{Noun}Response.cs
│   └── Services/             # Application Services thực thi contracts
├── Infrastructure/           # Module-specific database configurations & clients
└── Endpoints/
    └── {ModuleName}Endpoints.cs # Extension method Map{ModuleName}Endpoints()
```

---

## 2. Quy tắc Đặt tên Thành phần (Component Conventions)

| Thành phần | Quy tắc đặt tên | Thư mục lưu trữ | Ví dụ |
|---|---|---|---|
| **Domain Entity** | `PascalCase` (Danh từ số ít) | `Modules/{Module}/Domain/` | `WalletAccount.cs`, `Payment.cs` |
| **Domain Service Interface** | `I{Name}Service` | `SharedKernel/Contracts/` hoặc `Domain/Services/` | `ILedgerPostingService.cs` |
| **Domain Service Implementation** | `{Name}Service` | `Modules/{Module}/Application/Services/` | `LedgerPostingService.cs` |
| **CQRS Command** | `{Verb}{Noun}Command` | `Modules/{Module}/Application/{Feature}/` | `CreatePaymentCommand.cs` |
| **CQRS Command Handler** | `{Verb}{Noun}Handler` | `Modules/{Module}/Application/{Feature}/` | `CreatePaymentHandler.cs` |
| **CQRS Query** | `Get{Noun}Query` | `Modules/{Module}/Application/{Feature}/` | `GetBalanceQuery.cs` |
| **CQRS Query Handler** | `Get{Noun}Handler` | `Modules/{Module}/Application/{Feature}/` | `GetBalanceHandler.cs` |
| **DTO Request** | `{Verb}{Noun}Request` | `Modules/{Module}/Application/{Feature}/` | `CreatePaymentRequest.cs` |
| **DTO Response** | `{Noun}Response` | `Modules/{Module}/Application/{Feature}/` | `PaymentResponse.cs` |
| **Minimal API Endpoints** | `{ModuleName}Endpoints` | `Modules/{Module}/Endpoints/` | `PaymentEndpoints.cs`, `AdminEndpoints.cs` |
| **Middleware** | `{Name}Middleware` | `Infrastructure/Security/` | `HmacVerificationMiddleware.cs` |
| **Hosted / Background Worker** | `{Name}Publisher` / `{Name}Consumer` | `Infrastructure/Messaging/` | `OutboxPublisher.cs`, `WebhookConsumer.cs` |

---

## 3. Quy chuẩn Namespace

Namespace phải phản ánh chính xác cấu trúc thư mục thực tế:

```csharp
namespace MiniBanking.Modules.Accounts.Domain;
namespace MiniBanking.Modules.Accounts.Application.Services;
namespace MiniBanking.Modules.Accounts.Endpoints;

namespace MiniBanking.Modules.Payments.Domain;
namespace MiniBanking.Modules.Payments.Application.CreatePayment;
namespace MiniBanking.Modules.Payments.Endpoints;

namespace MiniBanking.SharedKernel;
namespace MiniBanking.SharedKernel.Behaviors;
namespace MiniBanking.SharedKernel.Contracts;

namespace MiniBanking.Infrastructure.Persistence;
namespace MiniBanking.Infrastructure.Security;
namespace MiniBanking.Infrastructure.Messaging;
```

---

## 4. Quy chuẩn Biến, Thuộc tính & Cơ sở dữ liệu

- **Property, Method, Class, Enum:** `PascalCase`.
- **Private Fields:** `_camelCase` (ví dụ `_dbContext`, `_idempotencyService`).
- **Database Tables:** `snake_case` và số nhiều (ví dụ `wallet_accounts`, `ledger_transactions`, `outbox_messages`).
- **Cấm viết tắt:** Không đặt tên biến kiểu `acc`, `tx`, `svc` trong public API/Property; dùng `account`, `transaction`, `service`.
- **Ngôn ngữ:** 100% mã nguồn (tên biến, class, method, comments, log messages) dùng tiếng Anh. Tiếng Việt chỉ xuất hiện trong tài liệu và `ApiResponse.Message` trả về cho người dùng cuối.
