using AccessControl.Application.AccessProfiles.Commands;
using AccessControl.Application.AccessProfiles.DTOs;
using AccessControl.Application.AccessProfiles.Queries;

namespace AccessControl.Api.Endpoints;

public static class AccessProfileEndpoints
{
    public static IEndpointRouteBuilder MapAccessProfileEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/access-profiles")
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithTags("AccessProfiles")
            .WithGetAll<GetAccessProfilesQuery, AccessProfileDto>("GetAccessProfiles")
            .WithGetById<AccessProfileDto>("GetAccessProfileById", id => new GetAccessProfileByIdQuery(id))
            .WithCreate<CreateAccessProfileCommand>("CreateAccessProfile", "GetAccessProfileById")
            .WithUpdate("UpdateAccessProfile", (Guid id, UpdateAccessProfileRequest body) =>
                new UpdateAccessProfileCommand(id, body.Name, body.Description, body.ZoneIds))
            .WithDelete("DeleteAccessProfile", id => new DeleteAccessProfileCommand(id));

        return app;
    }

    private record UpdateAccessProfileRequest(string Name, string? Description, List<Guid> ZoneIds);
}
