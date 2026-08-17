# API Conventions

## 1. Wrapper phản hồi

Mọi phản hồi từ API đều sử dụng `ApiResponse` từ namespace `MiniBanking.SharedKernel`.

```csharp
public class ApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? Data { get; init; }
}
```

## 2. Các factory method

- `ApiResponse.Ok(string message)` - Thành công không kèm dữ liệu.
- `ApiResponse.Ok<T>(string message, T data)` - Thành công kèm dữ liệu.
- `ApiResponse.Fail(string message)` - Thất bại.

## 3. Mẫu thông báo tiếng Việt

Thông báo API nên ngắn gọn, rõ ràng và sử dụng tiếng Việt.

### Thành công

- `ApiResponse.Ok("Lấy thông tin thành công")`
- `ApiResponse.Ok("Tạo tài khoản thành công", newWalletAccount)`
- `ApiResponse.Ok("Cập nhật số dư thành công")`

### Thất bại

- `ApiResponse.Fail("Không tìm thấy khách hàng")`
- `ApiResponse.Fail("Số dư không đủ để thực hiện giao dịch")`
- `ApiResponse.Fail("Mã tài khoản đã tồn tại")`
- `ApiResponse.Fail("Dữ liệu đầu vào không hợp lệ")`

## 4. Ví dụ sử dụng trong controller

```csharp
[HttpGet("{id:guid}")]
public IActionResult GetCustomer(Guid id)
{
    var customer = _customerService.FindById(id);

    if (customer is null)
        return NotFound(ApiResponse.Fail("Không tìm thấy khách hàng"));

    return Ok(ApiResponse.Ok("Lấy thông tin khách hàng thành công", customer));
}
```

## 5. Lưu ý

- Không trả về dữ liệu thô (raw object) trực tiếp từ controller.
- `Message` luôn là chuỗi không null.
- `Data` có thể null khi không cần trả về dữ liệu.
