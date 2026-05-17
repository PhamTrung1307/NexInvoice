using DomainTaskStatus = NexInvoice.Domain.Enums.TaskStatus;
using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.WorkItems;

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    DomainTaskStatus Status,
    TaskPriority Priority,
    DateOnly? DueDate,
    Guid ProjectId,
    string ProjectName,
    Guid? AssignedToId,
    string? AssignedToName,
    IReadOnlyCollection<TaskCommentResponse> Comments,
    IReadOnlyCollection<TaskAttachmentResponse> Attachments,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
