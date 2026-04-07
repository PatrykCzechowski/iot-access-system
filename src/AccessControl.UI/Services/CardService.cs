using AccessControl.UI.Models;
using Flurl.Http;

namespace AccessControl.UI.Services;

public sealed class CardService(IFlurlClient flurlClient) : ICardService
{
    public Task<List<CardItem>> GetCardsAsync()
        => flurlClient.Request("api/cards").GetJsonAsync<List<CardItem>>();

    public Task CreateCardAsync(string cardUid, Guid zoneId, string? label)
        => flurlClient.Request("api/cards")
            .PostJsonAsync(new { CardUid = cardUid, ZoneId = zoneId, Label = label });

    public Task UpdateCardAsync(Guid id, Guid zoneId, string? userId, string? label, bool isActive)
        => flurlClient.Request("api/cards", id)
            .PutJsonAsync(new { ZoneId = zoneId, UserId = userId, Label = label, IsActive = isActive });

    public Task DeleteCardAsync(Guid id)
        => flurlClient.Request("api/cards", id).DeleteAsync();

    public Task<CardItem> GetCardAsync(Guid id)
        => flurlClient.Request("api/cards", id)
            .GetJsonAsync<CardItem>();
}
