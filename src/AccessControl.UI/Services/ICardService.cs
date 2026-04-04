using AccessControl.UI.Models;

namespace AccessControl.UI.Services;

public interface ICardService
{
    Task<List<CardItem>> GetCardsAsync();
    Task CreateCardAsync(string cardUid, Guid zoneId, string? label);
    Task UpdateCardAsync(Guid id, Guid zoneId, string? userId, string? label, bool isActive);
    Task DeleteCardAsync(Guid id);
}
