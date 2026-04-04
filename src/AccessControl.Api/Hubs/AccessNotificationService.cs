using AccessControl.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AccessControl.Api.Hubs;

public sealed class AccessNotificationService(
    IHubContext<AccessControlHub> hubContext) : IAccessNotificationService
{
    public Task NotifyCardScannedAsync(AccessLogNotification notification, CancellationToken cancellationToken)
    {
        return hubContext.Clients.All.SendAsync("CardScanned", notification, cancellationToken);
    }

    public Task NotifyCardEnrolledAsync(CardEnrolledNotification notification, CancellationToken cancellationToken)
    {
        return hubContext.Clients.All.SendAsync("CardEnrolled", notification, cancellationToken);
    }
}
