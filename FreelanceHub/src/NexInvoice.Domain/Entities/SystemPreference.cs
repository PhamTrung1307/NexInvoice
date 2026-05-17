using NexInvoice.Domain.Common;

namespace NexInvoice.Domain.Entities;

public class SystemPreference : BaseEntity
{
    public string Currency { get; set; } = "VND";

    public string DateFormat { get; set; } = "dd/MM/yyyy";

    public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";

    public string InvoicePrefix { get; set; } = "INV";

    public decimal DefaultTaxRate { get; set; } = 10m;

    public int PaymentReminderDays { get; set; } = 3;
}
