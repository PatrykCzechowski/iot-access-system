using AccessControl.UI.Models;
using Flurl.Http;

namespace AccessControl.UI.Services;

public sealed class CardholderService(IFlurlClient flurlClient) : ICardholderService
{
    public Task<List<CardholderItem>> GetCardholdersAsync()
        => flurlClient.Request("api/cardholders").GetJsonAsync<List<CardholderItem>>();

    public Task CreateCardholderAsync(string firstName, string lastName, string? email, string? phoneNumber)
        => flurlClient.Request("api/cardholders")
            .PostJsonAsync(new { FirstName = firstName, LastName = lastName, Email = email, PhoneNumber = phoneNumber });

    public Task UpdateCardholderAsync(Guid id, string firstName, string lastName, string? email, string? phoneNumber)
        => flurlClient.Request("api/cardholders", id)
            .PutJsonAsync(new { FirstName = firstName, LastName = lastName, Email = email, PhoneNumber = phoneNumber });

    public Task DeleteCardholderAsync(Guid id)
        => flurlClient.Request("api/cardholders", id).DeleteAsync();
}
