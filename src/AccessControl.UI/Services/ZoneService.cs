using AccessControl.UI.Models;
using Flurl.Http;

namespace AccessControl.UI.Services;

public sealed class ZoneService(IFlurlClient flurlClient) : IZoneService
{
    public Task<List<ZoneItem>> GetZonesAsync()
        => flurlClient.Request("api/zones").GetJsonAsync<List<ZoneItem>>();

    public Task CreateZoneAsync(string name, string? description)
        => flurlClient.Request("api/zones")
            .PostJsonAsync(new { Name = name, Description = description });

    public Task UpdateZoneAsync(Guid id, string name, string? description)
        => flurlClient.Request("api/zones", id)
            .PutJsonAsync(new { Name = name, Description = description });

    public Task DeleteZoneAsync(Guid id)
        => flurlClient.Request("api/zones", id).DeleteAsync();
}
