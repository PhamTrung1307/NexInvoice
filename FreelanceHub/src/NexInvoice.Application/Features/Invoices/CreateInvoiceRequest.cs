using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Invoices;

public sealed record CreateInvoiceRequest(
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly? DueDate,
    Guid ProjectId,
    decimal TaxAmount,
    decimal DiscountAmount,
    IReadOnlyCollection<InvoiceItemRequest> Items,
    InvoiceStatus Status = InvoiceStatus.Draft);
