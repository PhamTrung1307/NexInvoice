namespace NexInvoice.Application.Features.Dashboard;

public sealed record DashboardSummaryResponse(
    int TotalClients,
    int TotalProjects,
    int TotalInvoices,
    decimal TotalRevenue,
    int OverdueInvoicesCount,
    int PendingPaymentsCount,
    IReadOnlyCollection<MonthlyRevenueResponse> MonthlyRevenue,
    IReadOnlyCollection<ProjectStatusStatisticResponse> ProjectStatusStatistics);
