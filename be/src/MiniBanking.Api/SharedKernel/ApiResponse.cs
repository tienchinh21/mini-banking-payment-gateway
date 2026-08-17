namespace MiniBanking.SharedKernel;

public class ApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? Data { get; init; }

    private ApiResponse(bool success, string message, object? data = null)
    {
        Success = success;
        Message = message;
        Data = data;
    }

    public static ApiResponse Ok(string message) => new(true, message);
    public static ApiResponse Ok<T>(string message, T data) => new(true, message, data);
    public static ApiResponse Fail(string message) => new(false, message);
}
