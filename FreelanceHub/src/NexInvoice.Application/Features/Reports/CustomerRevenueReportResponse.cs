namespace NexInvoice.Application.Features.Reports;

public sealed record CustomerRevenueReportResponse(
    IReadOnlyCollection<CustomerRevenueItem> TopCustomers);

public sealed record CustomerRevenueItem(
    Guid CustomerId,
    string CustomerName,
    decimal Revenue,
    int InvoiceCount);
