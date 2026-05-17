namespace NexInvoice.Application.Features.Reports;

public sealed record RevenueReportResponse(
    decimal TotalRevenue,
    decimal PaidRevenue,
    decimal PendingRevenue,
    int OverdueInvoiceCount,
    IReadOnlyCollection<MonthlyRevenueItem> MonthlyRevenue);

public sealed record MonthlyRevenueItem(string Month, decimal Revenue);
