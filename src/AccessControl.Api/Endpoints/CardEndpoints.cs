using AccessControl.Application.Cards.Commands;
using AccessControl.Application.Cards.DTOs;
using AccessControl.Application.Cards.Queries;
using MediatR;

namespace AccessControl.Api.Endpoints;

public static class CardEndpoints
{
    public static IEndpointRouteBuilder MapCardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cards")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithTags("Cards");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetAccessCardsQuery(), ct);
                return Results.Ok(result);
            })
            .WithName("GetCards")
            .Produces<IReadOnlyCollection<AccessCardDto>>();

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetAccessCardByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .WithName("GetCardById")
            .Produces<AccessCardDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreateAccessCardCommand command, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(command, ct);
                return Results.Created($"/api/cards/{id}", new { id });
            })
            .WithName("CreateCard")
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}", async (Guid id, UpdateCardRequest body, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new UpdateAccessCardCommand(id, body.ZoneId, body.UserId, body.Label, body.IsActive), ct);
                return Results.NoContent();
            })
            .WithName("UpdateCard")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeleteAccessCardCommand(id), ct);
                return Results.NoContent();
            })
            .WithName("DeleteCard")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private record UpdateCardRequest(Guid ZoneId, string? UserId, string? Label, bool IsActive);
}
