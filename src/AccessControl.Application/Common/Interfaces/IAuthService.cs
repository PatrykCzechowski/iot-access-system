using AccessControl.Application.Auth.DTOs;

namespace AccessControl.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken);
}
