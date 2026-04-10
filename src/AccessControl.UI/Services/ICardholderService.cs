using AccessControl.UI.Models;

namespace AccessControl.UI.Services;

public interface ICardholderService
{
    Task<List<CardholderItem>> GetCardholdersAsync();
    Task<CardholderItem> GetCardholderAsync(Guid id);
    Task CreateCardholderAsync(string firstName, string lastName, string? email, string? phoneNumber);
    Task UpdateCardholderAsync(Guid id, string firstName, string lastName, string? email, string? phoneNumber, List<Guid>? accessProfileIds = null);
    Task DeleteCardholderAsync(Guid id);
}
