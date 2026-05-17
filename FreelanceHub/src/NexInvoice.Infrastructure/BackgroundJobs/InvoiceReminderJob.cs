using NexInvoice.Application.Interfaces;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;
using NexInvoice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NexInvoice.Infrastructure.BackgroundJobs;

public sealed class InvoiceReminderJob
{
    private readonly AppDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IRealtimeNotificationService _realtimeNotificationService;

    public InvoiceReminderJob(
        AppDbContext dbContext,
        IEmailService emailService,
        IRealtimeNotificationService realtimeNotificationService)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _realtimeNotificationService = realtimeNotificationService;
    }

    public async Task RunAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var invoices = await _dbContext.Invoices
            .Include(invoice => invoice.Client)
            .Include(invoice => invoice.Project)
            .Where(invoice =>
                invoice.Status == InvoiceStatus.Sent
                && invoice.DueDate.HasValue
                && invoice.DueDate.Value < today)
            .ToArrayAsync();

        foreach (var invoice in invoices)
        {
            invoice.Status = InvoiceStatus.Overdue;

            var notification = CreateNotification(invoice);
            await SendReminderEmailAsync(invoice);

            await _dbContext.SaveChangesAsync();

            if (notification is not null)
            {
                await _realtimeNotificationService.SendToUserAsync(
                    notification.UserId,
                    new Application.Features.Notifications.NotificationResponse(
                        notification.Id,
                        notification.Title,
                        notification.Message,
                        notification.Type,
                        notification.IsRead,
                        notification.ReadAt,
                        notification.CreatedAt));
            }
        }
    }

    private Notification? CreateNotification(Invoice invoice)
    {
        if (invoice.Project?.OwnerId is null)
        {
            return null;
        }

        var notification = new Notification
        {
            UserId = invoice.Project.OwnerId.Value,
            Title = "Hóa đơn đã quá hạn",
            Message = $"Hóa đơn {invoice.InvoiceNumber} đã quá hạn thanh toán.",
            Type = NotificationType.Invoice
        };

        _dbContext.Notifications.Add(notification);

        return notification;
    }

    private Task SendReminderEmailAsync(Invoice invoice)
    {
        var dueDate = invoice.DueDate?.ToString("dd/MM/yyyy") ?? "không xác định";
        var subject = $"Nhắc thanh toán hóa đơn {invoice.InvoiceNumber}";
        var body = $"""
            <p>Xin chào {invoice.Client?.Name},</p>
            <p>Hóa đơn <strong>{invoice.InvoiceNumber}</strong> cho dự án <strong>{invoice.Project?.Name}</strong> đã quá hạn thanh toán từ ngày {dueDate}.</p>
            <p>Tổng số tiền cần thanh toán: <strong>{invoice.TotalAmount:N0}</strong>.</p>
            <p>Vui lòng kiểm tra và hoàn tất thanh toán trong thời gian sớm nhất.</p>
            <p>Trân trọng,<br/>NexInvoice</p>
            """;

        return _emailService.SendAsync(invoice.Client?.Email ?? string.Empty, subject, body);
    }
}
