using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Projects;

public sealed record ProjectListItemResponse(
    Guid Id,
    string Name,
    ProjectStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    decimal Budget,
    Guid ClientId,
    string ClientName,
    int TotalTasks,
    int CompletedTasks,
    decimal ProgressPercentage,
    DateTimeOffset CreatedAt);
