using MediatR;

namespace MiniBanking.Modules.Admin.Application.Commands.AdminLogin;

public sealed record AdminLoginCommand(string Email, string Password) : IRequest<AdminLoginResult?>;
