using NexInvoice.Application.Features.Reports;

namespace NexInvoice.Application.Interfaces;

public interface IReportService
{
    Task<RevenueReportResponse> GetRevenueAsync(ReportQueryParameters query, CancellationToken cancellationToken = default);

    Task<InvoiceStatusReportResponse> GetInvoiceStatusAsync(ReportQueryParameters query, CancellationToken cancellationToken = default);

    Task<ProjectProgressReportResponse> GetProjectProgressAsync(ReportQueryParameters query, CancellationToken cancellationToken = default);

    Task<CustomerRevenueReportResponse> GetCustomerRevenueAsync(ReportQueryParameters query, CancellationToken cancellationToken = default);
}
