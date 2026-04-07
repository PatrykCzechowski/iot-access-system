using AccessControl.Application.Cardholders.Commands;
using AccessControl.Application.Cardholders.DTOs;
using AccessControl.Application.Cardholders.Queries;

namespace AccessControl.Api.Endpoints;

public static class CardholderEndpoints
{
    public static IEndpointRouteBuilder MapCardholderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/cardholders")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithTags("Cardholders")
            .WithGetAll<GetCardholdersQuery, CardholderDto>("GetCardholders")
            .WithGetById<CardholderDto>("GetCardholderById", id => new GetCardholderByIdQuery(id))
            .WithCreate<CreateCardholderCommand>("CreateCardholder", "GetCardholderById")
            .WithUpdate("UpdateCardholder", (Guid id, UpdateCardholderRequest body) =>
                new UpdateCardholderCommand(id, body.FirstName, body.LastName, body.Email, body.PhoneNumber))
            .WithDelete("DeleteCardholder", id => new DeleteCardholderCommand(id));

        return app;
    }

    private record UpdateCardholderRequest(string FirstName, string LastName, string? Email, string? PhoneNumber);
}
