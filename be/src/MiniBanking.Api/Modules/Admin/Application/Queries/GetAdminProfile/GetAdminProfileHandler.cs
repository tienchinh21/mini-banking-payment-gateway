using System.Security.Claims;
using MediatR;

namespace MiniBanking.Modules.Admin.Application.Queries.GetAdminProfile;

public sealed class GetAdminProfileHandler : IRequestHandler<GetAdminProfileQuery, AdminProfileDto>
{
    public Task<AdminProfileDto> Handle(GetAdminProfileQuery request, CancellationToken cancellationToken)
    {
        var user = request.User;
        var profile = new AdminProfileDto(
            user.FindFirstValue(ClaimTypes.NameIdentifier),
            user.FindFirstValue(ClaimTypes.Email),
            user.FindFirstValue(ClaimTypes.Name),
            user.FindFirstValue(ClaimTypes.Role));

        return Task.FromResult(profile);
    }
}
