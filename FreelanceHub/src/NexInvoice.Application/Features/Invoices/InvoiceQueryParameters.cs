using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Invoices;

public sealed class InvoiceQueryParameters
{
    public string? Search { get; set; }

    public InvoiceStatus? Status { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
