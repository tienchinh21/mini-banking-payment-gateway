namespace MiniBanking.Modules.Admin.Application.Commands.AdminLogin;

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string FullName,
    string Role);

public sealed record AdminLoginResult(
    string Token,
    AdminUserDto User);
