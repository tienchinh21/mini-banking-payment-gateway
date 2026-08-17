# Entity Conventions

## 1. Lớp cơ sở `Entity`

Tất cả các entity trong tầng Domain đều kế thừa từ `MiniBanking.SharedKernel.Entity`.

```csharp
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public string CreatedBy { get; protected set; } = string.Empty;
    public string UpdatedBy { get; protected set; } = string.Empty;

    private List<DomainEvent> _domainEvents = new();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(DomainEvent domainEvent)
    {
        if (domainEvent is null)
            throw new ArgumentNullException(nameof(domainEvent));

        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(DomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();

    public void MarkUpdated(string? updatedBy = null)
    {
        UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(updatedBy))
            UpdatedBy = updatedBy;
    }
}
```

## 2. Các trường audit

- `CreatedAt`: Thời điểm tạo entity, tự động gán `DateTime.UtcNow`.
- `UpdatedAt`: Thời điểm cập nhật cuối, nullable.
- `CreatedBy`: Người tạo entity.
- `UpdatedBy`: Người cập nhật entity cuối cùng.

Không khai báo lại các trường audit trong entity con.

## 3. Cập nhật entity

Sử dụng `MarkUpdated()` thay vì gán `UpdatedAt` trực tiếp.

```csharp
public void Credit(Money amount)
{
    // ... kiểm tra và cập nhật số dư
    MarkUpdated();
}
```

Có thể truyền tên người cập nhật:

```csharp
MarkUpdated("system");
```

## 4. Domain events

Entity có thể phát ra domain event thông qua `AddDomainEvent`. Các event cần được xóa sau khi xử lý bằng `ClearDomainEvents()`.

```csharp
public class Customer : Entity
{
    public Customer(string name)
    {
        // ... logic khởi tạo
        AddDomainEvent(new CustomerCreatedEvent(Id));
    }
}
```

## 5. Lưu ý

- Không để setter public cho các trường audit.
- `Id` được khởi tạo tự động khi tạo entity.
- EF Core vẫn yêu cầu constructor không tham số `private` để materialize entity.
