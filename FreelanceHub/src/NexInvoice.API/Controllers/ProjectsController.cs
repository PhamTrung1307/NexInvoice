using NexInvoice.API.Authorization;
using NexInvoice.Application.Common.Authorization;
using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Projects;
using NexInvoice.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NexInvoice.API.Controllers;

[ApiController]
[Route("api/v1/projects")]
[Authorize]
public sealed class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    [HasPermission(AppPermissions.ProjectView)]
    public async Task<ActionResult<ApiResponse<PagedResult<ProjectListItemResponse>>>> GetProjects(
        [FromQuery] ProjectQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var result = await _projectService.GetPagedAsync(queryParameters, cancellationToken);

        return Ok(ApiResponse<PagedResult<ProjectListItemResponse>>.Ok(
            result,
            "Lấy danh sách dự án thành công."));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AppPermissions.ProjectView)]
    public async Task<ActionResult<ApiResponse<ProjectResponse>>> GetProjectById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _projectService.GetByIdAsync(id, cancellationToken);

        return Ok(ApiResponse<ProjectResponse>.Ok(
            result,
            "Lấy thông tin dự án thành công."));
    }

    [HttpPost]
    [HasPermission(AppPermissions.ProjectCreate)]
    public async Task<ActionResult<ApiResponse<ProjectResponse>>> CreateProject(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _projectService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetProjectById),
            new { id = result.Id },
            ApiResponse<ProjectResponse>.Ok(result, "Tạo dự án thành công."));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(AppPermissions.ProjectUpdate)]
    public async Task<ActionResult<ApiResponse<ProjectResponse>>> UpdateProject(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _projectService.UpdateAsync(id, request, cancellationToken);

        return Ok(ApiResponse<ProjectResponse>.Ok(
            result,
            "Cập nhật dự án thành công."));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(AppPermissions.ProjectDelete)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteProject(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _projectService.DeleteAsync(id, cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, "Xóa dự án thành công."));
    }

    [HttpPatch("{id:guid}/status")]
    [HasPermission(AppPermissions.ProjectUpdate)]
    public async Task<ActionResult<ApiResponse<ProjectResponse>>> UpdateProjectStatus(
        Guid id,
        UpdateProjectStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _projectService.UpdateStatusAsync(id, request, cancellationToken);

        return Ok(ApiResponse<ProjectResponse>.Ok(
            result,
            "Cập nhật trạng thái dự án thành công."));
    }
}
