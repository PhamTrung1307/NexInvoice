using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Settings;
using NexInvoice.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NexInvoice.API.Controllers;

[ApiController]
[Route("api/v1/settings")]
[Authorize]
public sealed class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet("company")]
    public async Task<ActionResult<ApiResponse<CompanyProfileResponse>>> GetCompany(CancellationToken cancellationToken)
    {
        var result = await _settingsService.GetCompanyProfileAsync(cancellationToken);
        return Ok(ApiResponse<CompanyProfileResponse>.Ok(result, "Lấy thông tin công ty thành công."));
    }

    [HttpPut("company")]
    public async Task<ActionResult<ApiResponse<CompanyProfileResponse>>> UpdateCompany(
        UpdateCompanyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _settingsService.UpdateCompanyProfileAsync(request, cancellationToken);
        return Ok(ApiResponse<CompanyProfileResponse>.Ok(result, "Cập nhật thông tin công ty thành công"));
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<ApiResponse<SystemPreferenceResponse>>> GetPreferences(CancellationToken cancellationToken)
    {
        var result = await _settingsService.GetSystemPreferencesAsync(cancellationToken);
        return Ok(ApiResponse<SystemPreferenceResponse>.Ok(result, "Lấy cấu hình hệ thống thành công."));
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<ApiResponse<SystemPreferenceResponse>>> UpdatePreferences(
        UpdateSystemPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _settingsService.UpdateSystemPreferencesAsync(request, cancellationToken);
        return Ok(ApiResponse<SystemPreferenceResponse>.Ok(result, "Cập nhật cấu hình hệ thống thành công"));
    }
}
