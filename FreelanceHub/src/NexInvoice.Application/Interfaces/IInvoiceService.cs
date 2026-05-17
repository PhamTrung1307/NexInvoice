using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Invoices;

namespace NexInvoice.Application.Interfaces;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListItemResponse>> GetPagedAsync(
        InvoiceQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<InvoiceResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InvoiceResponse> CreateAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default);

    Task<InvoiceResponse> UpdateAsync(
        Guid id,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken = default);

    Task<InvoiceResponse> SendAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InvoiceResponse> CancelAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InvoiceResponse> MarkPaidAsync(Guid id, CancellationToken cancellationToken = default);
}
