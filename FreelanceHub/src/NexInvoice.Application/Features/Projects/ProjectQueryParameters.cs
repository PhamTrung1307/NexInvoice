using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Projects;

public sealed class ProjectQueryParameters
{
    public string? Search { get; set; }

    public ProjectStatus? Status { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
