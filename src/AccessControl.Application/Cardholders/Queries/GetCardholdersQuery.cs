using AccessControl.Application.Cardholders.DTOs;
using MediatR;

namespace AccessControl.Application.Cardholders.Queries;

public record GetCardholdersQuery : IRequest<IReadOnlyCollection<CardholderDto>>;
