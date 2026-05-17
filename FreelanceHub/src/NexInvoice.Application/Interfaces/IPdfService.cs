using NexInvoice.Application.Features.Invoices.Pdfs;

namespace NexInvoice.Application.Interfaces;

public interface IPdfService
{
    Task<PdfFileResponse> GenerateInvoicePdfAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);
}
