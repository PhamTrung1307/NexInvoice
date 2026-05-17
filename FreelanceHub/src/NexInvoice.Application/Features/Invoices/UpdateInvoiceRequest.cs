using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Invoices;

public sealed record UpdateInvoiceRequest(
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly? DueDate,
    decimal TaxAmount,
    decimal DiscountAmount,
    IReadOnlyCollection<InvoiceItemRequest> Items,
    InvoiceStatus Status);
