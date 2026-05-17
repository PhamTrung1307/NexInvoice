using DomainTaskStatus = NexInvoice.Domain.Enums.TaskStatus;
using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.WorkItems;

public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    DomainTaskStatus Status,
    TaskPriority Priority,
    DateOnly? DueDate,
    Guid? AssignedToId);
