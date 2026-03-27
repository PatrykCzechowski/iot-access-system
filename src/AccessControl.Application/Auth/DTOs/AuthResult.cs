namespace AccessControl.Application.Auth.DTOs;

public abstract record AuthResult
{
    public sealed record Success(string AccessToken, DateTimeOffset ExpiresAt, bool MustChangePassword) : AuthResult;
    public sealed record Failure(string Error) : AuthResult;
}
