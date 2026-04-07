using AccessControl.Application.Cards.Commands;
using AccessControl.Application.Cards.DTOs;
using AccessControl.Application.Cards.Queries;

namespace AccessControl.Api.Endpoints;

public static class CardEndpoints
{
    public static IEndpointRouteBuilder MapCardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/cards")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithTags("Cards")
            .WithGetAll<GetAccessCardsQuery, AccessCardDto>("GetCards")
            .WithGetById<AccessCardDto>("GetCardById", id => new GetAccessCardByIdQuery(id))
            .WithCreate<CreateAccessCardCommand>("CreateCard", "GetCardById")
            .WithUpdate("UpdateCard", (Guid id, UpdateCardRequest body) =>
                new UpdateAccessCardCommand(id, body.CardholderId, body.Label, body.IsActive))
            .WithDelete("DeleteCard", id => new DeleteAccessCardCommand(id));

        return app;
    }

    private record UpdateCardRequest(Guid? CardholderId, string? Label, bool IsActive);
}
