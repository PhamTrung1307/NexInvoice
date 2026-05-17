namespace NexInvoice.Application.Features.Settings;

public sealed record UpdateSystemPreferenceRequest(
    string Currency,
    string DateFormat,
    string TimeZone,
    string InvoicePrefix,
    decimal DefaultTaxRate,
    int PaymentReminderDays);
