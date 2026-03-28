using AccessControl.Application.Auth.DTOs;
using AccessControl.Application.Common.Interfaces;
using MediatR;

namespace AccessControl.Application.Auth.Commands;

public class LoginCommandHandler(IAuthService authService)
    : IRequestHandler<LoginCommand, AuthResult>
{
    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken ct) =>
        await authService.LoginAsync(request.Email, request.Password, ct);
}
