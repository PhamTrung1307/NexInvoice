using DomainTaskStatus = NexInvoice.Domain.Enums.TaskStatus;
using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.WorkItems;

public sealed record TaskListItemResponse(
    Guid Id,
    string Title,
    DomainTaskStatus Status,
    TaskPriority Priority,
    DateOnly? DueDate,
    Guid? AssignedToId,
    string? AssignedToName,
    DateTimeOffset CreatedAt);
