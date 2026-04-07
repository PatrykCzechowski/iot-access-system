using AccessControl.Application.AccessProfiles.DTOs;
using MediatR;

namespace AccessControl.Application.AccessProfiles.Queries;

public record GetAccessProfileByIdQuery(Guid Id) : IRequest<AccessProfileDto>;
