using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace NexInvoice.API.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
}
