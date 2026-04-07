using AccessControl.UI.Models;
using Flurl.Http;

namespace AccessControl.UI.Services;

public sealed class AccessProfileService(IFlurlClient flurlClient) : IAccessProfileService
{
    public Task<List<AccessProfileItem>> GetProfilesAsync()
        => flurlClient.Request("api/access-profiles").GetJsonAsync<List<AccessProfileItem>>();

    public Task CreateProfileAsync(string name, string? description, List<Guid> zoneIds)
        => flurlClient.Request("api/access-profiles")
            .PostJsonAsync(new { Name = name, Description = description, ZoneIds = zoneIds });

    public Task UpdateProfileAsync(Guid id, string name, string? description, List<Guid> zoneIds)
        => flurlClient.Request("api/access-profiles", id)
            .PutJsonAsync(new { Name = name, Description = description, ZoneIds = zoneIds });

    public Task DeleteProfileAsync(Guid id)
        => flurlClient.Request("api/access-profiles", id).DeleteAsync();
}
