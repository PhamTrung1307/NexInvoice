using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Projects;

public sealed record UpdateProjectStatusRequest(ProjectStatus Status);
