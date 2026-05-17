using NexInvoice.Application.Common.Exceptions;
using NexInvoice.Application.Features.Payments;
using NexInvoice.Application.Interfaces;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;
using NexInvoice.Infrastructure.Caching;
using NexInvoice.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace NexInvoice.Infrastructure.Services;

internal sealed class PaymentService : IPaymentService
{
    private const long MaxFileSizeInBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".pdf",
        ".docx",
        ".xlsx"
    };

    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IRealtimeNotificationService _realtimeNotificationService;
    private readonly IDistributedCache _cache;

    public PaymentService(
        AppDbContext dbContext,
        IWebHostEnvironment webHostEnvironment,
        IRealtimeNotificationService realtimeNotificationService,
        IDistributedCache cache)
    {
        _dbContext = dbContext;
        _webHostEnvironment = webHostEnvironment;
        _realtimeNotificationService = realtimeNotificationService;
        _cache = cache;
    }

    public async Task<PaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new BadRequestException("Số tiền thanh toán phải lớn hơn 0.");
        }

        var invoice = await _dbContext.Invoices
            .FirstOrDefaultAsync(currentInvoice => currentInvoice.Id == request.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            throw new NotFoundException("Không tìm thấy hóa đơn.");
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new BadRequestException("Không thể tạo thanh toán cho hóa đơn đã hủy.");
        }

        var payment = new Payment
        {
            InvoiceId = request.InvoiceId,
            Amount = request.Amount,
            Method = request.Method,
            Status = PaymentStatus.Pending,
            PaymentDate = request.PaymentDate,
            TransactionReference = NormalizeOptional(request.TransactionReference)
        };

        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await InvalidateDashboardCacheAsync(cancellationToken);

        return await GetPaymentResponseAsync(payment.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PaymentResponse>> GetByInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoiceExists = await _dbContext.Invoices
            .AnyAsync(invoice => invoice.Id == invoiceId, cancellationToken);

        if (!invoiceExists)
        {
            throw new NotFoundException("Không tìm thấy hóa đơn.");
        }

        var payments = await _dbContext.Payments
            .AsNoTracking()
            .Include(payment => payment.Invoice)
            .Where(payment => payment.InvoiceId == invoiceId)
            .OrderByDescending(payment => payment.PaymentDate)
            .ThenByDescending(payment => payment.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return payments.Select(MapToResponse).ToArray();
    }

    public async Task<PaymentResponse> UploadProofAsync(
        Guid id,
        UploadPaymentProofRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateProof(request);

        var payment = await _dbContext.Payments
            .Include(currentPayment => currentPayment.Invoice)
            .FirstOrDefaultAsync(currentPayment => currentPayment.Id == id, cancellationToken);

        if (payment is null)
        {
            throw new NotFoundException("Không tìm thấy thanh toán.");
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new BadRequestException("Thanh toán đã được xử lý, không thể cập nhật minh chứng.");
        }

        var uploadsRoot = GetPaymentUploadsRoot();
        Directory.CreateDirectory(uploadsRoot);

        var originalFileName = Path.GetFileName(request.FileName);
        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var storedFilePath = Path.Combine(uploadsRoot, storedFileName);

        await using (var fileStream = File.Create(storedFilePath))
        {
            await request.Content.CopyToAsync(fileStream, cancellationToken);
        }

        payment.ProofFileName = originalFileName;
        payment.ProofFileUrl = $"/uploads/payments/{storedFileName}";
        payment.ProofContentType = request.ContentType;
        payment.ProofSizeInBytes = request.SizeInBytes;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await InvalidateDashboardCacheAsync(cancellationToken);

        return MapToResponse(payment);
    }

    public async Task<PaymentResponse> ConfirmAsync(
        Guid id,
        Guid confirmedBy,
        CancellationToken cancellationToken = default)
    {
        var payment = await GetPaymentWithInvoiceAsync(id, cancellationToken);

        if (payment is null)
        {
            throw new NotFoundException("Không tìm thấy thanh toán.");
        }

        if (payment.Status == PaymentStatus.Confirmed)
        {
            throw new BadRequestException("Thanh toán đã được xác nhận trước đó");
        }

        if (payment.Status == PaymentStatus.Rejected)
        {
            throw new BadRequestException("Không thể xác nhận thanh toán đã bị từ chối.");
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new BadRequestException("Chỉ có thể xác nhận thanh toán đang chờ xác nhận.");
        }

        payment.Status = PaymentStatus.Confirmed;
        payment.ConfirmedAt = DateTimeOffset.UtcNow;
        payment.ConfirmedBy = confirmedBy;
        payment.RejectedAt = null;
        payment.RejectReason = null;
        AddAuditLog(payment, "ConfirmPayment", confirmedBy, null);

        await UpdateInvoiceStatusAfterConfirmAsync(payment.InvoiceId, cancellationToken);
        var notification = CreatePaymentNotificationIfPossible(
            payment,
            "Thanh toán đã được xác nhận",
            $"Thanh toán cho hóa đơn {payment.Invoice?.InvoiceNumber} đã được xác nhận.");
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SendNotificationIfNeededAsync(notification, cancellationToken);
        await InvalidateDashboardCacheAsync(cancellationToken);

        return MapToResponse(payment);
    }

    public async Task<PaymentResponse> RejectAsync(
        Guid id,
        RejectPaymentRequest request,
        Guid rejectedBy,
        CancellationToken cancellationToken = default)
    {
        var reason = NormalizeOptional(request.Reason);
        if (reason is null)
        {
            throw new BadRequestException("Lý do từ chối thanh toán là bắt buộc.");
        }

        var payment = await GetPaymentWithInvoiceAsync(id, cancellationToken);

        if (payment is null)
        {
            throw new NotFoundException("Không tìm thấy thanh toán.");
        }

        if (payment.Status == PaymentStatus.Confirmed)
        {
            throw new BadRequestException("Không thể từ chối thanh toán đã xác nhận.");
        }

        if (payment.Status == PaymentStatus.Rejected)
        {
            throw new BadRequestException("Thanh toán đã bị từ chối trước đó.");
        }

        if (payment.Status != PaymentStatus.Pending)
        {
            throw new BadRequestException("Chỉ có thể từ chối thanh toán đang chờ xác nhận.");
        }

        payment.Status = PaymentStatus.Rejected;
        payment.RejectedAt = DateTimeOffset.UtcNow;
        payment.ConfirmedAt = null;
        payment.ConfirmedBy = null;
        payment.RejectReason = reason;
        AddAuditLog(payment, "RejectPayment", rejectedBy, reason);

        var notification = CreatePaymentNotificationIfPossible(
            payment,
            "Thanh toán đã bị từ chối",
            $"Thanh toán cho hóa đơn {payment.Invoice?.InvoiceNumber} đã bị từ chối.");
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SendNotificationIfNeededAsync(notification, cancellationToken);
        await InvalidateDashboardCacheAsync(cancellationToken);

        return MapToResponse(payment);
    }

    private async Task<Payment?> GetPaymentWithInvoiceAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Payments
            .Include(payment => payment.Invoice)
            .ThenInclude(invoice => invoice!.Project)
            .FirstOrDefaultAsync(payment => payment.Id == id, cancellationToken);
    }

    private async Task<PaymentResponse> GetPaymentResponseAsync(Guid id, CancellationToken cancellationToken)
    {
        var payment = await _dbContext.Payments
            .AsNoTracking()
            .Include(currentPayment => currentPayment.Invoice)
            .FirstAsync(currentPayment => currentPayment.Id == id, cancellationToken);

        return MapToResponse(payment);
    }

    private async Task UpdateInvoiceStatusAfterConfirmAsync(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var invoice = await _dbContext.Invoices
            .Include(currentInvoice => currentInvoice.Payments)
            .FirstAsync(currentInvoice => currentInvoice.Id == invoiceId, cancellationToken);

        var totalConfirmed = invoice.Payments
            .Where(payment => payment.Status == PaymentStatus.Confirmed)
            .Sum(payment => payment.Amount);

        if (totalConfirmed >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
        }
        else if (totalConfirmed > 0 && invoice.Status != InvoiceStatus.Cancelled)
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
        }
    }

    private static PaymentResponse MapToResponse(Payment payment)
    {
        return new PaymentResponse(
            payment.Id,
            payment.InvoiceId,
            payment.Invoice?.InvoiceNumber ?? string.Empty,
            payment.Amount,
            payment.Method,
            payment.Status,
            payment.PaymentDate,
            payment.TransactionReference,
            payment.ProofFileName,
            payment.ProofFileUrl,
            payment.ProofContentType,
            payment.ProofSizeInBytes,
            payment.ConfirmedAt,
            payment.ConfirmedBy,
            payment.RejectedAt,
            payment.RejectReason,
            payment.CreatedAt,
            payment.UpdatedAt);
    }

    private Notification? CreatePaymentNotificationIfPossible(
        Payment payment,
        string title,
        string message)
    {
        if (payment.Invoice?.Project?.OwnerId is null)
        {
            return null;
        }

        var notification = new Notification
        {
            UserId = payment.Invoice.Project.OwnerId.Value,
            Title = title,
            Message = message,
            Type = NotificationType.Payment
        };

        _dbContext.Notifications.Add(notification);

        return notification;
    }

    private void AddAuditLog(
        Payment payment,
        string action,
        Guid userId,
        string? reason)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(Payment),
            EntityId = payment.Id,
            Action = action,
            UserId = userId,
            NewValues = reason is null
                ? $"Status={payment.Status}; ConfirmedAt={payment.ConfirmedAt:O}; ConfirmedBy={payment.ConfirmedBy}"
                : $"Status={payment.Status}; RejectedAt={payment.RejectedAt:O}; RejectReason={reason}"
        });
    }

    private async Task SendNotificationIfNeededAsync(
        Notification? notification,
        CancellationToken cancellationToken)
    {
        if (notification is null)
        {
            return;
        }

        await _realtimeNotificationService.SendToUserAsync(
            notification.UserId,
            new Application.Features.Notifications.NotificationResponse(
                notification.Id,
                notification.Title,
                notification.Message,
                notification.Type,
                notification.IsRead,
                notification.ReadAt,
                notification.CreatedAt),
            cancellationToken);
    }

    private static void ValidateProof(UploadPaymentProofRequest request)
    {
        if (request.Content.Length == 0 || request.SizeInBytes <= 0)
        {
            throw new BadRequestException("Tệp minh chứng thanh toán là bắt buộc.");
        }

        if (request.SizeInBytes > MaxFileSizeInBytes)
        {
            throw new BadRequestException("Kích thước tệp không được vượt quá 10MB.");
        }

        var extension = Path.GetExtension(request.FileName);

        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new BadRequestException("Định dạng tệp minh chứng không được hỗ trợ.");
        }
    }

    private string GetPaymentUploadsRoot()
    {
        var webRootPath = _webHostEnvironment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
        }

        return Path.Combine(webRootPath, "uploads", "payments");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private Task InvalidateDashboardCacheAsync(CancellationToken cancellationToken)
    {
        return _cache.RemoveAsync(DashboardCacheKeys.Summary, cancellationToken);
    }
}
