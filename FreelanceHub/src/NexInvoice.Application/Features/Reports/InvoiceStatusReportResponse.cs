namespace NexInvoice.Application.Features.Reports;

public sealed record InvoiceStatusReportResponse(
    IReadOnlyCollection<StatusCountItem> Items);

public sealed record StatusCountItem(string Status, int Count, decimal Amount);
