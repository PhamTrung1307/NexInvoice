using NexInvoice.API.Authorization;
using NexInvoice.Application.Common.Authorization;
using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.WorkItems;
using NexInvoice.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace NexInvoice.API.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/tasks")]
public sealed class ProjectTasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public ProjectTasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    [HasPermission(AppPermissions.TaskView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TaskListItemResponse>>>> GetTasksByProject(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetByProjectAsync(projectId, cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<TaskListItemResponse>>.Ok(
            result,
            "Lấy danh sách công việc thành công."));
    }

    [HttpPost]
    [HasPermission(AppPermissions.TaskCreate)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> CreateTask(
        Guid projectId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.CreateAsync(projectId, request, cancellationToken);

        return CreatedAtAction(
            "GetTaskById",
            "Tasks",
            new { id = result.Id },
            ApiResponse<TaskResponse>.Ok(result, "Tạo công việc thành công."));
    }
}
