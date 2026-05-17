using NexInvoice.Application.Common.Exceptions;
using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Invoices;
using NexInvoice.Application.Interfaces;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;
using NexInvoice.Infrastructure.Caching;
using NexInvoice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace NexInvoice.Infrastructure.Services;

internal sealed class InvoiceService : IInvoiceService
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext _dbContext;
    private readonly IDistributedCache _cache;

    public InvoiceService(AppDbContext dbContext, IDistributedCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<PagedResult<InvoiceListItemResponse>> GetPagedAsync(
        InvoiceQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var page = queryParameters.Page < 1 ? 1 : queryParameters.Page;
        var pageSize = queryParameters.PageSize < 1 ? 10 : Math.Min(queryParameters.PageSize, MaxPageSize);

        var query = _dbContext.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Client)
            .Include(invoice => invoice.Project)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParameters.Search))
        {
            var keyword = queryParameters.Search.Trim();

            query = query.Where(invoice =>
                invoice.InvoiceNumber.Contains(keyword)
                || (invoice.Client != null && invoice.Client.Name.Contains(keyword))
                || (invoice.Project != null && invoice.Project.Name.Contains(keyword)));
        }

        if (queryParameters.Status.HasValue)
        {
            query = query.Where(invoice => invoice.Status == queryParameters.Status.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(invoice => invoice.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(invoice => new InvoiceListItemResponse(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.Status,
                invoice.IssueDate,
                invoice.DueDate,
                invoice.TotalAmount,
                invoice.ClientId,
                invoice.Client != null ? invoice.Client.Name : string.Empty,
                invoice.ProjectId,
                invoice.Project != null ? invoice.Project.Name : null,
                invoice.CreatedAt))
            .ToArrayAsync(cancellationToken);

        return PagedResult<InvoiceListItemResponse>.Create(items, page, pageSize, totalItems);
    }

    public async Task<InvoiceResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await GetInvoiceWithDetailsAsync(id, asNoTracking: true, cancellationToken);

        if (invoice is null)
        {
            throw new NotFoundException("Không tìm thấy hóa đơn.");
        }

        return MapToResponse(invoice);
    }

    public async Task<InvoiceResponse> CreateAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateInvoiceRequest(
            request.InvoiceNumber,
            request.IssueDate,
            request.DueDate,
            request.TaxAmount,
            request.DiscountAmount,
            request.Items);

        var project = await _dbContext.Projects
            .Include(currentProject => currentProject.Client)
            .FirstOrDefaultAsync(currentProject => currentProject.Id == request.ProjectId, cancellationToken);

        if (project is null)
        {
            throw new NotFoundException("Không tìm thấy dự án.");
        }

        await EnsureInvoiceNumberIsUniqueAsync(request.InvoiceNumber, null, cancellationToken);

        var items = CreateInvoiceItems(request.Items);
        var subtotal = CalculateSubtotal(items);
        var totalAmount = CalculateTotal(subtotal, request.TaxAmount, request.DiscountAmount);

        var invoice = new Invoice
        {
            InvoiceNumber = request.InvoiceNumber.Trim(),
            IssueDate = request.IssueDate,
            DueDate = request.DueDate,
            ClientId = project.ClientId,
            Client = project.Client,
            ProjectId = project.Id,
            Project = project,
            Status = request.Status,
            TaxAmount = request.TaxAmount,
            DiscountAmount = request.DiscountAmount,
            Subtotal = subtotal,
            TotalAmount = totalAmount,
            Items = items
        };

        EnsureCanSetStatus(invoice, request.Status);

        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await InvalidateDashboardCacheAsync(cancellationToken);

        return await GetByIdAsync(invoice.Id, cancellationToken);
    }

    public async Task<InvoiceResponse> UpdateAsync(
        Guid id,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateInvoiceRequest(
            request.InvoiceNumber,
            request.IssueDate,
            request.DueDate,
            request.TaxAmount,
            request.DiscountAmount,
            request.Items);

        var invoice = await GetInvoiceWithDetailsAsync(id, asNoTracking: false, cancellationToken);

        if (invoice is null)
        {
            throw new NotFoundException("Không tìm thấy hóa đơn.");
        }

        await EnsureInvoiceNumberIsUniqueAsync(request.InvoiceNumber, id, cancellationToken);

        var items = CreateInvoiceItems(request.Items);
        var subtotal = CalculateSubtotal(items);
        var totalAmount = CalculateTotal(subtotal, request.TaxAmount, request.DiscountAmount);

        invoice.InvoiceNumber = request.InvoiceNumber.Trim();
        invoice.IssueDate = request.IssueDate;
        invoice.DueDate = request.DueDate;
        invoice.TaxAmount = request.TaxAmount;
        invoice.DiscountAmount = request.DiscountAmount;
        invoice.Subtotal = subtotal;
        invoice.TotalAmount = totalAmount;
        invoice.Status = request.Status;

        EnsureCanSetStatus(invoice, request.Status);

        _dbContext.InvoiceItems.RemoveRange(invoice.Items);
        invoice.Items = items;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await InvalidateDashboardCacheAsync(cancellationToken);

        return await GetByIdAsync(invoice.Id, cancellationToken);
    }

    public async Task<InvoiceResponse> SendAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await GetInvoiceWithDetailsAsync(id, asNoTracking: false, cancellationToken);

        if (invoice is null)
        {
            throw new NotFoundException("Không tìm thấy hóa đơn.");
        }

        if (invoice.TotalAmount <= 0)
        {
            throw new BadRequestException("Không thể gửi hóa đơn có tổng tiền nhỏ hơn hoặc bằng 0.");
        }

        invoice.Status = InvoiceStatus.Sent;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await InvalidateDashboardCacheAsync(cancellationToken);

        return MapToResponse(invoice);
    }

    public async Task<InvoiceResponse> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await GetInvoiceWithDetailsAsync(id, asNoTracking: false, cancellationToken);

        if (invoice is null)
        {
            throw new NotFoundException("Không tìm thấy hóa đơn.");
        }

        invoice.Status = InvoiceStatus.Cancelled;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await InvalidateDashboardCacheAsync(cancellationToken);

        return MapToResponse(invoice);
    }

    public async Task<InvoiceResponse> MarkPaidAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await GetInvoiceWithDetailsAsync(id, asNoTracking: false, cancellationToken);

        if (invoice is null)
        {
            throw new NotFoundException("Không tìm thấy hóa đơn.");
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new BadRequestException("Không thể đánh dấu đã thanh toán cho hóa đơn đã hủy.");
        }

        invoice.Status = InvoiceStatus.Paid;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await InvalidateDashboardCacheAsync(cancellationToken);

        return MapToResponse(invoice);
    }

    private async Task<Invoice?> GetInvoiceWithDetailsAsync(
        Guid id,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Invoices
            .Include(invoice => invoice.Client)
            .Include(invoice => invoice.Project)
            .Include(invoice => invoice.Items)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(invoice => invoice.Id == id, cancellationToken);
    }

    private async Task EnsureInvoiceNumberIsUniqueAsync(
        string invoiceNumber,
        Guid? currentInvoiceId,
        CancellationToken cancellationToken)
    {
        var normalizedInvoiceNumber = invoiceNumber.Trim();
        var exists = await _dbContext.Invoices.AnyAsync(invoice =>
            invoice.InvoiceNumber == normalizedInvoiceNumber
            && (!currentInvoiceId.HasValue || invoice.Id != currentInvoiceId.Value),
            cancellationToken);

        if (exists)
        {
            throw new BadRequestException("Số hóa đơn đã được sử dụng.");
        }
    }

    private static void ValidateInvoiceRequest(
        string invoiceNumber,
        DateOnly issueDate,
        DateOnly? dueDate,
        decimal taxAmount,
        decimal discountAmount,
        IReadOnlyCollection<InvoiceItemRequest> items)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            errors.Add("Số hóa đơn là bắt buộc.");
        }

        if (dueDate.HasValue && dueDate.Value < issueDate)
        {
            errors.Add("Ngày đến hạn phải lớn hơn hoặc bằng ngày phát hành.");
        }

        if (taxAmount < 0)
        {
            errors.Add("Tiền thuế phải lớn hơn hoặc bằng 0.");
        }

        if (discountAmount < 0)
        {
            errors.Add("Tiền giảm giá phải lớn hơn hoặc bằng 0.");
        }

        if (items.Count == 0)
        {
            errors.Add("Hóa đơn phải có ít nhất một dòng chi tiết.");
        }

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Description))
            {
                errors.Add("Mô tả dòng chi tiết là bắt buộc.");
            }

            if (item.Quantity <= 0)
            {
                errors.Add("Số lượng phải lớn hơn 0.");
            }

            if (item.UnitPrice < 0)
            {
                errors.Add("Đơn giá phải lớn hơn hoặc bằng 0.");
            }
        }

        if (errors.Count > 0)
        {
            throw new BadRequestException("Dữ liệu hóa đơn không hợp lệ.", errors);
        }
    }

    private static List<InvoiceItem> CreateInvoiceItems(IEnumerable<InvoiceItemRequest> itemRequests)
    {
        return itemRequests
            .Select(item => new InvoiceItem
            {
                Description = item.Description.Trim(),
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Quantity * item.UnitPrice
            })
            .ToList();
    }

    private static decimal CalculateSubtotal(IEnumerable<InvoiceItem> items)
    {
        return items.Sum(item => item.Amount);
    }

    private static decimal CalculateTotal(decimal subtotal, decimal taxAmount, decimal discountAmount)
    {
        return subtotal + taxAmount - discountAmount;
    }

    private static void EnsureCanSetStatus(Invoice invoice, InvoiceStatus status)
    {
        if (status == InvoiceStatus.Paid && invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new BadRequestException("Không thể đánh dấu đã thanh toán cho hóa đơn đã hủy.");
        }

        if (status == InvoiceStatus.Sent && invoice.TotalAmount <= 0)
        {
            throw new BadRequestException("Không thể gửi hóa đơn có tổng tiền nhỏ hơn hoặc bằng 0.");
        }
    }

    private static InvoiceResponse MapToResponse(Invoice invoice)
    {
        return new InvoiceResponse(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.Status,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.Subtotal,
            invoice.TaxAmount,
            invoice.DiscountAmount,
            invoice.TotalAmount,
            invoice.ClientId,
            invoice.Client?.Name ?? string.Empty,
            invoice.ProjectId,
            invoice.Project?.Name,
            invoice.Items
                .OrderBy(item => item.CreatedAt)
                .Select(item => new InvoiceItemResponse(
                    item.Id,
                    item.Description,
                    item.Quantity,
                    item.UnitPrice,
                    item.Amount))
                .ToArray(),
            invoice.CreatedAt,
            invoice.UpdatedAt);
    }

    private Task InvalidateDashboardCacheAsync(CancellationToken cancellationToken)
    {
        return _cache.RemoveAsync(DashboardCacheKeys.Summary, cancellationToken);
    }
}
