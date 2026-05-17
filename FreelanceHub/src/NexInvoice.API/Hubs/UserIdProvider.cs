using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace NexInvoice.API.Hubs;

public sealed class UserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirstValue("UserId")
            ?? connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
