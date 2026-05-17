namespace NexInvoice.Application.Features.Invoices;

public sealed record InvoiceItemResponse(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Amount);
