using AccessControl.UI.Models;
using Flurl.Http;

namespace AccessControl.UI.Services;

public sealed class CardService(IFlurlClient flurlClient) : ICardService
{
    public Task<List<CardItem>> GetCardsAsync()
        => flurlClient.Request("api/cards").GetJsonAsync<List<CardItem>>();

    public Task CreateCardAsync(string cardUid, Guid? cardholderId, string? label)
        => flurlClient.Request("api/cards")
            .PostJsonAsync(new { CardUid = cardUid, CardholderId = cardholderId, Label = label });

    public Task UpdateCardAsync(Guid id, Guid? cardholderId, string? label, bool isActive)
        => flurlClient.Request("api/cards", id)
            .PutJsonAsync(new { CardholderId = cardholderId, Label = label, IsActive = isActive });

    public Task DeleteCardAsync(Guid id)
        => flurlClient.Request("api/cards", id).DeleteAsync();

    public Task<CardItem> GetCardAsync(Guid id)
        => flurlClient.Request("api/cards", id)
            .GetJsonAsync<CardItem>();
}
