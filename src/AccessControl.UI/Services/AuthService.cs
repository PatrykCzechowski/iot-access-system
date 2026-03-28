using AccessControl.UI.Auth;
using AccessControl.UI.Models;
using Blazored.LocalStorage;
using Flurl.Http;

namespace AccessControl.UI.Services;

public class AuthService(
    IFlurlClient flurlClient,
    ILocalStorageService localStorage,
    IAuthNotifier authNotifier) : IAuthService
{

    public async Task<LoginResponse> LoginAsync(LoginModel loginModel)
    {
        try
        {
            var request = new LoginRequest
            {
                Email = loginModel.Email,
                Password = loginModel.Password
            };

            var successResult = await flurlClient
                .Request("api/auth/login")
                .PostJsonAsync(request)
                .ReceiveJson<LoginResponse>();

            if (successResult == null || string.IsNullOrEmpty(successResult.AccessToken))
            {
                return successResult ?? new LoginResponse { ErrorMessage = "Brak danych z serwera." };
            }

            await localStorage.SetItemAsync(AuthConstants.TokenKey, successResult.AccessToken);

            authNotifier.NotifyUserLoggedIn(successResult.AccessToken);

            return successResult;
        }
        catch (FlurlHttpException ex)
        {
            var problem = await ex.GetResponseJsonAsync<ProblemDetailsDto>();
            return new LoginResponse { ErrorMessage = problem?.Detail ?? "Nieprawidłowe dane logowania." };
        }
        catch (Exception)
        {
            return new LoginResponse { ErrorMessage = "Błąd połączenia z serwerem." };
        }
    }

    public async Task LogoutAsync()
    {
        await localStorage.RemoveItemAsync(AuthConstants.TokenKey);
        authNotifier.NotifyUserLoggedOut();
    }
}
