using AccessControl.UI.Models;

namespace AccessControl.UI.Services;

public interface IAccessProfileService
{
    Task<List<AccessProfileItem>> GetProfilesAsync();
    Task<AccessProfileItem> GetProfileAsync(Guid id);
    Task CreateProfileAsync(string name, string? description);
    Task UpdateProfileAsync(Guid id, string name, string? description, List<Guid> zoneIds);
    Task DeleteProfileAsync(Guid id);
}
