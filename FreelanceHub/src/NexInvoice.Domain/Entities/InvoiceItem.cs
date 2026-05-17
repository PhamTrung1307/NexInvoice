using NexInvoice.Domain.Common;

namespace NexInvoice.Domain.Entities;

public class InvoiceItem : BaseEntity
{
    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public Guid InvoiceId { get; set; }

    public Invoice? Invoice { get; set; }
}
