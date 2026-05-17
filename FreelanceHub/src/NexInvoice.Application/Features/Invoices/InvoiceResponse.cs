using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Invoices;

public sealed record InvoiceResponse(
    Guid Id,
    string InvoiceNumber,
    InvoiceStatus Status,
    DateOnly IssueDate,
    DateOnly? DueDate,
    decimal Subtotal,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    Guid ClientId,
    string ClientName,
    Guid? ProjectId,
    string? ProjectName,
    IReadOnlyCollection<InvoiceItemResponse> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
