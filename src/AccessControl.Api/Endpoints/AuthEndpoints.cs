using AccessControl.Application.Auth.Commands;
using AccessControl.Application.Auth.DTOs;
using MediatR;

namespace AccessControl.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .RequireRateLimiting("auth");

        group.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result switch
                {
                    AuthResult.Success success => Results.Ok(success),
                    AuthResult.Failure failure => Results.Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Authentication failed",
                        detail: failure.Error),
                    _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
                };
            })
            .AllowAnonymous()
            .WithName("Login");
    }
}
