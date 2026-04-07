using AccessControl.UI.Models;

namespace AccessControl.UI.Services;

public interface ICardService
{
    Task<List<CardItem>> GetCardsAsync();
    Task CreateCardAsync(string cardUid, Guid? cardholderId, string? label);
    Task UpdateCardAsync(Guid id, Guid? cardholderId, string? label, bool isActive);
    Task DeleteCardAsync(Guid id);
    Task<CardItem> GetCardAsync(Guid id);
}
