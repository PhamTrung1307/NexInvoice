using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Projects;

public sealed record CreateProjectRequest(
    string Name,
    string? Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    decimal Budget,
    Guid ClientId,
    ProjectStatus Status = ProjectStatus.Draft);
