namespace NexInvoice.Application.Features.Settings;

public sealed record SystemPreferenceResponse(
    Guid Id,
    string Currency,
    string DateFormat,
    string TimeZone,
    string InvoicePrefix,
    decimal DefaultTaxRate,
    int PaymentReminderDays);
