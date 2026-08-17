# Naming Conventions

## 1. Namespace

Sử dụng namespace theo cấu trúc module:

```csharp
namespace MiniBanking.Modules.Accounts.Domain;
namespace MiniBanking.Modules.Ledger.Domain;
namespace MiniBanking.SharedKernel;
namespace MiniBanking.Infrastructure.Persistence;
```

## 2. Lớp và interface

- Class và interface sử dụng `PascalCase`.
- Interface bắt đầu bằng chữ `I`.

```csharp
public class BankingCustomer { }
public interface ICustomerRepository { }
```

## 3. Thuộc tính và method

- Property và method sử dụng `PascalCase`.
- Private field sử dụng `_camelCase`.

```csharp
public class WalletAccount : Entity
{
    private BankingCustomer? _customer;
    public BankingCustomer Customer => _customer ?? throw new InvalidOperationException("...");
}
```

## 4. Tên bảng trong cơ sở dữ liệu

Sử dụng snake_case và số nhiều:

```csharp
entity.ToTable("banking_customers");
entity.ToTable("wallet_accounts");
entity.ToTable("ledger_transactions");
```

## 5. Constants và enums

- Enum sử dụng `PascalCase` cho tên và giá trị.
- Constants sử dụng `PascalCase` hoặc `ALL_CAPS` nếu là compile-time constant.

```csharp
public enum LedgerTransactionType
{
    TopUp = 1,
    Payment = 2
}
```

## 6. Tên file

- Tên file trùng với tên class chính.
- Mỗi file nên chứa một class chính.

```
BankingCustomer.cs
WalletAccount.cs
LedgerTransaction.cs
```

## 7. Lưu ý

- Không sử dụng tiếng Việt trong tên biến, tên lớp hay tên method.
- Tiếng Việt chỉ sử dụng trong message API và tài liệu.
