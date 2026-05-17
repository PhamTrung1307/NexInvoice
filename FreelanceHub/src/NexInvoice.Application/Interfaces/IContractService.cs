using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Contracts;

namespace NexInvoice.Application.Interfaces;

public interface IContractService
{
    Task<PagedResult<ContractResponse>> GetPagedAsync(ContractQueryParameters query, CancellationToken cancellationToken = default);

    Task<ContractResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ContractResponse> CreateAsync(CreateContractRequest request, CancellationToken cancellationToken = default);

    Task<ContractResponse> UpdateAsync(Guid id, UpdateContractRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ContractResponse> UploadAsync(Guid id, UploadContractFileRequest request, CancellationToken cancellationToken = default);

    Task<ContractFileResult> DownloadAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ContractResponse> ApproveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ContractResponse> RejectAsync(Guid id, RejectContractRequest request, CancellationToken cancellationToken = default);
}
