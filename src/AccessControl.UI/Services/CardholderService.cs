using AccessControl.UI.Models;
using Flurl.Http;

namespace AccessControl.UI.Services;

public sealed class CardholderService(IFlurlClient flurlClient) : ICardholderService
{
    public Task<List<CardholderItem>> GetCardholdersAsync()
        => flurlClient.Request("api/cardholders").GetJsonAsync<List<CardholderItem>>();

    public Task<CardholderItem> GetCardholderAsync(Guid id)
        => flurlClient.Request("api/cardholders", id).GetJsonAsync<CardholderItem>();

    public Task CreateCardholderAsync(string firstName, string lastName, string? email, string? phoneNumber)
        => flurlClient.Request("api/cardholders")
            .PostJsonAsync(new { FirstName = firstName, LastName = lastName, Email = email, PhoneNumber = phoneNumber });

    public Task UpdateCardholderAsync(Guid id, string firstName, string lastName, string? email, string? phoneNumber, List<Guid>? accessProfileIds = null)
        => flurlClient.Request("api/cardholders", id)
            .PutJsonAsync(new { FirstName = firstName, LastName = lastName, Email = email, PhoneNumber = phoneNumber, AccessProfileIds = accessProfileIds });

    public Task DeleteCardholderAsync(Guid id)
        => flurlClient.Request("api/cardholders", id).DeleteAsync();
}
