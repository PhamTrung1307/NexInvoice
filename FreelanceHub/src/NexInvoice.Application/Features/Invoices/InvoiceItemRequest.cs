namespace NexInvoice.Application.Features.Invoices;

public sealed record InvoiceItemRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice);
