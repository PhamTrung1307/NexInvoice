using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Projects;

namespace NexInvoice.Application.Interfaces;

public interface IProjectService
{
    Task<PagedResult<ProjectListItemResponse>> GetPagedAsync(
        ProjectQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<ProjectResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ProjectResponse> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken = default);

    Task<ProjectResponse> UpdateAsync(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken = default);

    Task<ProjectResponse> UpdateStatusAsync(
        Guid id,
        UpdateProjectStatusRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
