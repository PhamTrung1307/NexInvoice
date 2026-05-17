using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Invoices;

public sealed record InvoiceListItemResponse(
    Guid Id,
    string InvoiceNumber,
    InvoiceStatus Status,
    DateOnly IssueDate,
    DateOnly? DueDate,
    decimal TotalAmount,
    Guid ClientId,
    string ClientName,
    Guid? ProjectId,
    string? ProjectName,
    DateTimeOffset CreatedAt);
