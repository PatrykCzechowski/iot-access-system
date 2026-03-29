using AccessControl.Application.Cards.DTOs;
using MediatR;

namespace AccessControl.Application.Cards.Queries;

public record GetAccessCardByIdQuery(Guid Id) : IRequest<AccessCardDto>;
