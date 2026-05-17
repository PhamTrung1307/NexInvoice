using System.Security.Claims;
using NexInvoice.Application.Common.Exceptions;
using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Notifications;
using NexInvoice.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NexInvoice.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<NotificationResponse>>>> GetNotifications(
        CancellationToken cancellationToken)
    {
        var result = await _notificationService.GetCurrentUserNotificationsAsync(
            GetCurrentUserId(),
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<NotificationResponse>>.Ok(
            result,
            "Lấy danh sách thông báo thành công."));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<NotificationResponse>>> MarkAsRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _notificationService.MarkAsReadAsync(
            id,
            GetCurrentUserId(),
            cancellationToken);

        return Ok(ApiResponse<NotificationResponse>.Ok(
            result,
            "Đánh dấu thông báo đã đọc thành công."));
    }

    [HttpPatch("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllAsReadAsync(GetCurrentUserId(), cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, "Đánh dấu tất cả thông báo đã đọc thành công."));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue("UserId")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            throw new UnauthorizedException("Bạn cần đăng nhập để thực hiện hành động này.");
        }

        return parsedUserId;
    }
}
