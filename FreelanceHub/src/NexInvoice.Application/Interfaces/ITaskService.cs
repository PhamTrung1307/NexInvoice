using NexInvoice.Application.Features.WorkItems;

namespace NexInvoice.Application.Interfaces;

public interface ITaskService
{
    Task<IReadOnlyCollection<TaskListItemResponse>> GetByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TaskResponse> CreateAsync(
        Guid projectId,
        CreateTaskRequest request,
        CancellationToken cancellationToken = default);

    Task<TaskResponse> UpdateAsync(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TaskResponse> UpdateStatusAsync(
        Guid id,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<TaskResponse> UpdatePriorityAsync(
        Guid id,
        UpdateTaskPriorityRequest request,
        CancellationToken cancellationToken = default);

    Task<TaskResponse> AssignAsync(
        Guid id,
        AssignTaskRequest request,
        CancellationToken cancellationToken = default);

    Task<TaskCommentResponse> AddCommentAsync(
        Guid id,
        AddTaskCommentRequest request,
        Guid authorId,
        CancellationToken cancellationToken = default);

    Task<TaskAttachmentResponse> UploadAttachmentAsync(
        Guid id,
        UploadTaskAttachmentRequest request,
        CancellationToken cancellationToken = default);
}
