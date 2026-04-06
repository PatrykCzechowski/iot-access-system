using MediatR;

namespace AccessControl.Api.Endpoints;

public static class CrudEndpointExtensions
{
    public static RouteGroupBuilder WithGetAll<TQuery, TDto>(
        this RouteGroupBuilder group, string endpointName)
        where TQuery : IRequest<IReadOnlyCollection<TDto>>, new()
    {
        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new TQuery(), ct);
                return Results.Ok(result);
            })
            .WithName(endpointName)
            .Produces<IReadOnlyCollection<TDto>>();

        return group;
    }

    public static RouteGroupBuilder WithGetById<TDto>(
        this RouteGroupBuilder group, string endpointName,
        Func<Guid, IRequest<TDto>> queryFactory)
    {
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(queryFactory(id), ct);
                return Results.Ok(result);
            })
            .WithName(endpointName)
            .Produces<TDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    public static RouteGroupBuilder WithCreate<TCommand>(
        this RouteGroupBuilder group, string endpointName, string getByIdEndpointName)
        where TCommand : IRequest<Guid>
    {
        group.MapPost("/", async (TCommand command, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(command, ct);
                return Results.CreatedAtRoute(getByIdEndpointName, new { id });
            })
            .WithName(endpointName)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        return group;
    }

    public static RouteGroupBuilder WithUpdate<TBody, TCommand>(
        this RouteGroupBuilder group, string endpointName,
        Func<Guid, TBody, TCommand> commandFactory)
        where TCommand : IRequest
    {
        group.MapPut("/{id:guid}", async (Guid id, TBody body, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(commandFactory(id, body), ct);
                return Results.NoContent();
            })
            .WithName(endpointName)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return group;
    }

    public static RouteGroupBuilder WithDelete<TCommand>(
        this RouteGroupBuilder group, string endpointName,
        Func<Guid, TCommand> commandFactory)
        where TCommand : IRequest
    {
        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(commandFactory(id), ct);
                return Results.NoContent();
            })
            .WithName(endpointName)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}
