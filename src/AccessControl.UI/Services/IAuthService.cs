using AccessControl.UI.Models;

namespace AccessControl.UI.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginModel loginModel);
    Task LogoutAsync();
}
