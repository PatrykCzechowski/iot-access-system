using AccessControl.Infrastructure.Persistence;

namespace AccessControl.Api.Endpoints;

public static class DevSeedEndpoints
{
    public static IEndpointRouteBuilder MapDevSeedEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dev")
            .RequireAuthorization(p => p.RequireRole("Admin"))
            .WithTags("Dev");

        group.MapPost("/seed", async (DevDataSeeder seeder, CancellationToken ct) =>
            {
                var result = await seeder.SeedAsync(ct);
                return Results.Ok(result);
            })
            .WithName("DevSeed")
            .Produces<DevDataSeeder.SeedResult>();

        return app;
    }
}
