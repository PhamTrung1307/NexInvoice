using DomainTaskStatus = NexInvoice.Domain.Enums.TaskStatus;

namespace NexInvoice.Application.Features.WorkItems;

public sealed record UpdateTaskStatusRequest(DomainTaskStatus Status);
