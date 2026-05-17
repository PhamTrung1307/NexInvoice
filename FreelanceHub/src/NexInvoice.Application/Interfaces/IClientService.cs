using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Customers;

namespace NexInvoice.Application.Interfaces;

public interface IClientService
{
    Task<PagedResult<ClientListItemResponse>> GetPagedAsync(
        ClientQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<ClientResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ClientResponse> CreateAsync(
        CreateClientRequest request,
        CancellationToken cancellationToken = default);

    Task<ClientResponse> UpdateAsync(
        Guid id,
        UpdateClientRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
