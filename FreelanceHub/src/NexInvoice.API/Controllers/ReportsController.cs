using NexInvoice.API.Authorization;
using NexInvoice.Application.Common.Authorization;
using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Reports;
using NexInvoice.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NexInvoice.API.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("revenue")]
    [HasPermission(AppPermissions.ReportView)]
    public async Task<ActionResult<ApiResponse<RevenueReportResponse>>> GetRevenue(
        [FromQuery] ReportQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetRevenueAsync(query, cancellationToken);
        return Ok(ApiResponse<RevenueReportResponse>.Ok(result, "Lấy báo cáo doanh thu thành công"));
    }

    [HttpGet("invoice-status")]
    [HasPermission(AppPermissions.ReportView)]
    public async Task<ActionResult<ApiResponse<InvoiceStatusReportResponse>>> GetInvoiceStatus(
        [FromQuery] ReportQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetInvoiceStatusAsync(query, cancellationToken);
        return Ok(ApiResponse<InvoiceStatusReportResponse>.Ok(result, "Lấy báo cáo hóa đơn thành công"));
    }

    [HttpGet("project-progress")]
    [HasPermission(AppPermissions.ReportView)]
    public async Task<ActionResult<ApiResponse<ProjectProgressReportResponse>>> GetProjectProgress(
        [FromQuery] ReportQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetProjectProgressAsync(query, cancellationToken);
        return Ok(ApiResponse<ProjectProgressReportResponse>.Ok(result, "Lấy báo cáo tiến độ dự án thành công"));
    }

    [HttpGet("customer-revenue")]
    [HasPermission(AppPermissions.ReportView)]
    public async Task<ActionResult<ApiResponse<CustomerRevenueReportResponse>>> GetCustomerRevenue(
        [FromQuery] ReportQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _reportService.GetCustomerRevenueAsync(query, cancellationToken);
        return Ok(ApiResponse<CustomerRevenueReportResponse>.Ok(result, "Lấy báo cáo khách hàng thành công"));
    }
}
