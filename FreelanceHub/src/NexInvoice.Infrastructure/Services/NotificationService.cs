using NexInvoice.Application.Common.Exceptions;
using NexInvoice.Application.Features.Notifications;
using NexInvoice.Application.Interfaces;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;
using NexInvoice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NexInvoice.Infrastructure.Services;

internal sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _dbContext;
    private readonly IRealtimeNotificationService _realtimeNotificationService;

    public NotificationService(
        AppDbContext dbContext,
        IRealtimeNotificationService realtimeNotificationService)
    {
        _dbContext = dbContext;
        _realtimeNotificationService = realtimeNotificationService;
    }

    public async Task<IReadOnlyCollection<NotificationResponse>> GetCurrentUserNotificationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await CreateOverdueInvoiceNotificationsAsync(cancellationToken);

        return await _dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderBy(notification => notification.IsRead)
            .ThenByDescending(notification => notification.CreatedAt)
            .Select(notification => new NotificationResponse(
                notification.Id,
                notification.Title,
                notification.Message,
                notification.Type,
                notification.IsRead,
                notification.ReadAt,
                notification.CreatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<NotificationResponse> MarkAsReadAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(currentNotification =>
                currentNotification.Id == id
                && currentNotification.UserId == userId,
                cancellationToken);

        if (notification is null)
        {
            throw new NotFoundException("Không tìm thấy thông báo.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapToResponse(notification);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _dbContext.Notifications
            .Where(notification => notification.UserId == userId && !notification.IsRead)
            .ToArrayAsync(cancellationToken);

        var utcNow = DateTimeOffset.UtcNow;

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = utcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateOverdueInvoiceNotificationsAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var createdNotifications = new List<Notification>();
        var overdueInvoices = await _dbContext.Invoices
            .Include(invoice => invoice.Project)
            .Where(invoice =>
                invoice.DueDate.HasValue
                && invoice.DueDate.Value < today
                && invoice.Status == InvoiceStatus.Sent)
            .ToArrayAsync(cancellationToken);

        foreach (var invoice in overdueInvoices)
        {
            invoice.Status = InvoiceStatus.Overdue;

            if (invoice.Project?.OwnerId is null)
            {
                continue;
            }

            var title = "Hóa đơn đã quá hạn";
            var message = $"Hóa đơn {invoice.InvoiceNumber} đã quá hạn thanh toán.";
            var exists = await _dbContext.Notifications.AnyAsync(notification =>
                notification.UserId == invoice.Project.OwnerId.Value
                && notification.Title == title
                && notification.Message == message,
                cancellationToken);

            if (!exists)
            {
                var notification = new Notification
                {
                    UserId = invoice.Project.OwnerId.Value,
                    Title = title,
                    Message = message,
                    Type = NotificationType.Invoice
                };

                _dbContext.Notifications.Add(notification);
                createdNotifications.Add(notification);
            }
        }

        if (overdueInvoices.Length > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        foreach (var notification in createdNotifications)
        {
            await _realtimeNotificationService.SendToUserAsync(
                notification.UserId,
                MapToResponse(notification),
                cancellationToken);
        }
    }

    private static NotificationResponse MapToResponse(Notification notification)
    {
        return new NotificationResponse(
            notification.Id,
            notification.Title,
            notification.Message,
            notification.Type,
            notification.IsRead,
            notification.ReadAt,
            notification.CreatedAt);
    }
}
