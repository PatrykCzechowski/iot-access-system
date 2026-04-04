using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AccessControl.Api.Hubs;

[Authorize(Roles = "Admin")]
public sealed class AccessControlHub : Hub;
