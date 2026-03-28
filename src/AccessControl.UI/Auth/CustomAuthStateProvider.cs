using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace AccessControl.UI.Auth;

public class CustomAuthStateProvider(
    ILocalStorageService localStorage) : AuthenticationStateProvider, IAuthNotifier
{

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await localStorage.GetItemAsync<string>(AuthConstants.TokenKey);

        if (string.IsNullOrWhiteSpace(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = ParseClaimsFromJwt(token).ToList();

        var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
        if (expClaim != null
            && long.TryParse(expClaim.Value, out var expUnix)
            && DateTimeOffset.FromUnixTimeSeconds(expUnix) <= DateTimeOffset.UtcNow)
        {
            await localStorage.RemoveItemAsync(AuthConstants.TokenKey);
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    public void NotifyUserLoggedIn(string token)
    {
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        
        var authState = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        NotifyAuthenticationStateChanged(authState);
    }

    public void NotifyUserLoggedOut()
    {
        var authState = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        NotifyAuthenticationStateChanged(authState);
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
        {
            return [];
        }

        try
        {
            var jsonBytes = ParseBase64WithoutPadding(parts[1]);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs is null)
            {
                return [];
            }

            return keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()!));
        }
        catch
        {
            return [];
        }
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
