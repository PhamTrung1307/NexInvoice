using NexInvoice.API.Authorization;
using NexInvoice.Application.Common.Authorization;
using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Dashboard;
using NexInvoice.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NexInvoice.API.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    [HasPermission(AppPermissions.DashboardView)]
    public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> GetSummary(
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetSummaryAsync(cancellationToken);

        return Ok(ApiResponse<DashboardSummaryResponse>.Ok(
            result,
            "Lấy dữ liệu tổng quan thành công."));
    }
}
