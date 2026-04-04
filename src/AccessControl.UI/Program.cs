using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AccessControl.UI;
using AccessControl.UI.Auth;
using AccessControl.UI.Services;
using Blazored.LocalStorage;
using Flurl.Http;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7157/";

builder.Services.AddScoped<IFlurlClient>(sp =>
{
    var localStorage = sp.GetRequiredService<ILocalStorageService>();
    return new FlurlClient(apiBaseUrl)
        .BeforeCall(async call =>
        {
            var token = await localStorage.GetItemAsync<string>(AuthConstants.TokenKey);
            if (!string.IsNullOrWhiteSpace(token))
            {
                call.Request.WithOAuthBearerToken(token);
            }
        });
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IZoneService, ZoneService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddSingleton<IAccessHubService, AccessHubService>();

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<IAuthNotifier>(sp => sp.GetRequiredService<CustomAuthStateProvider>());

await builder.Build().RunAsync();
