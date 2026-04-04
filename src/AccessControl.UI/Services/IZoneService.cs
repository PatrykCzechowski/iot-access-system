using AccessControl.UI.Models;

namespace AccessControl.UI.Services;

public interface IZoneService
{
    Task<List<ZoneItem>> GetZonesAsync();
    Task CreateZoneAsync(string name, string? description);
    Task UpdateZoneAsync(Guid id, string name, string? description);
    Task DeleteZoneAsync(Guid id);
}
