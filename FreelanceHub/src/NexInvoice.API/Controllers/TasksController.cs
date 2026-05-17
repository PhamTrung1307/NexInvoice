using System.Security.Claims;
using NexInvoice.API.Authorization;
using NexInvoice.Application.Common.Authorization;
using NexInvoice.Application.Common.Exceptions;
using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.WorkItems;
using NexInvoice.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace NexInvoice.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet("{id:guid}", Name = "GetTaskById")]
    [HasPermission(AppPermissions.TaskView)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> GetTaskById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetByIdAsync(id, cancellationToken);

        return Ok(ApiResponse<TaskResponse>.Ok(result, "Lấy thông tin công việc thành công."));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(AppPermissions.TaskUpdate)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> UpdateTask(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.UpdateAsync(id, request, cancellationToken);

        return Ok(ApiResponse<TaskResponse>.Ok(result, "Cập nhật công việc thành công."));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(AppPermissions.TaskUpdate)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteTask(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _taskService.DeleteAsync(id, cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, "Xóa công việc thành công."));
    }

    [HttpPatch("{id:guid}/status")]
    [HasPermission(AppPermissions.TaskUpdate)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> UpdateTaskStatus(
        Guid id,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.UpdateStatusAsync(id, request, cancellationToken);

        return Ok(ApiResponse<TaskResponse>.Ok(result, "Cập nhật trạng thái công việc thành công."));
    }

    [HttpPatch("{id:guid}/priority")]
    [HasPermission(AppPermissions.TaskUpdate)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> UpdateTaskPriority(
        Guid id,
        UpdateTaskPriorityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.UpdatePriorityAsync(id, request, cancellationToken);

        return Ok(ApiResponse<TaskResponse>.Ok(result, "Cập nhật độ ưu tiên công việc thành công."));
    }

    [HttpPatch("{id:guid}/assignee")]
    [HasPermission(AppPermissions.TaskUpdate)]
    public async Task<ActionResult<ApiResponse<TaskResponse>>> AssignTask(
        Guid id,
        AssignTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.AssignAsync(id, request, cancellationToken);

        return Ok(ApiResponse<TaskResponse>.Ok(result, "Giao công việc thành công."));
    }

    [HttpPost("{id:guid}/comments")]
    [HasPermission(AppPermissions.TaskUpdate)]
    public async Task<ActionResult<ApiResponse<TaskCommentResponse>>> AddTaskComment(
        Guid id,
        AddTaskCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.AddCommentAsync(
            id,
            request,
            GetCurrentUserId(),
            cancellationToken);

        return Ok(ApiResponse<TaskCommentResponse>.Ok(result, "Thêm bình luận thành công."));
    }

    [HttpPost("{id:guid}/attachments")]
    [HasPermission(AppPermissions.TaskUpdate)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<TaskAttachmentResponse>>> UploadTaskAttachment(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new BadRequestException("Tệp tải lên là bắt buộc.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _taskService.UploadAttachmentAsync(
            id,
            new UploadTaskAttachmentRequest(
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                GetCurrentUserId()),
            cancellationToken);

        return Ok(ApiResponse<TaskAttachmentResponse>.Ok(result, "Tải tệp đính kèm thành công."));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue("UserId")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            throw new UnauthorizedException("Bạn cần đăng nhập để thực hiện hành động này.");
        }

        return parsedUserId;
    }
}
