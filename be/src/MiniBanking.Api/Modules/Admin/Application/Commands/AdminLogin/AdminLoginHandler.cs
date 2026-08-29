using MediatR;
using Microsoft.EntityFrameworkCore;
using MiniBanking.Infrastructure.Persistence;
using MiniBanking.Infrastructure.Security;

namespace MiniBanking.Modules.Admin.Application.Commands.AdminLogin;

public sealed class AdminLoginHandler : IRequestHandler<AdminLoginCommand, AdminLoginResult?>
{
    private readonly MiniBankingDbContext _dbContext;
    private readonly IJwtTokenService _tokenService;

    public AdminLoginHandler(MiniBankingDbContext dbContext, IJwtTokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    public async Task<AdminLoginResult?> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
    {
        var admin = await _dbContext.AdminUsers
            .FirstOrDefaultAsync(a => a.Email == request.Email && a.IsActive, cancellationToken);

        if (admin is null || !PasswordHasher.Verify(request.Password, admin.PasswordHash))
        {
            return null;
        }

        var token = _tokenService.GenerateToken(
            admin.Id.ToString(),
            admin.Email,
            admin.FullName,
            new[] { admin.Role });

        return new AdminLoginResult(
            token,
            new AdminUserDto(admin.Id, admin.Email, admin.FullName, admin.Role));
    }
}
