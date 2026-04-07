using AccessControl.Application.AccessProfiles.DTOs;
using MediatR;

namespace AccessControl.Application.AccessProfiles.Queries;

public record GetAccessProfilesQuery : IRequest<IReadOnlyCollection<AccessProfileDto>>;
