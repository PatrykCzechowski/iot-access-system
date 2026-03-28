using AccessControl.Application.Auth.DTOs;
using MediatR;

namespace AccessControl.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResult>
{
    public override string ToString() => $"LoginCommand {{ Email = {Email} }}";
}
