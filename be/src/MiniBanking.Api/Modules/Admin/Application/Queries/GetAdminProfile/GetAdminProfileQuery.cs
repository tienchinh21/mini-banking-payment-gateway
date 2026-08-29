using System.Security.Claims;
using MediatR;

namespace MiniBanking.Modules.Admin.Application.Queries.GetAdminProfile;

public sealed record AdminProfileDto(
    string? Id,
    string? Email,
    string? FullName,
    string? Role);

public sealed record GetAdminProfileQuery(ClaimsPrincipal User) : IRequest<AdminProfileDto>;
