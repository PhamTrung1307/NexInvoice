using NexInvoice.Application.Features.Dashboard;

namespace NexInvoice.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
}
