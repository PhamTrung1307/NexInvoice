using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Projects;

public sealed record ProjectResponse(
    Guid Id,
    string Name,
    string? Description,
    ProjectStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    decimal Budget,
    Guid ClientId,
    string ClientName,
    int TotalTasks,
    int CompletedTasks,
    decimal ProgressPercentage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
