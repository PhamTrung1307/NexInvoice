using NexInvoice.Application.Features.Payments;

namespace NexInvoice.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PaymentResponse>> GetByInvoiceAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse> UploadProofAsync(
        Guid id,
        UploadPaymentProofRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse> ConfirmAsync(
        Guid id,
        Guid confirmedBy,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse> RejectAsync(
        Guid id,
        RejectPaymentRequest request,
        Guid rejectedBy,
        CancellationToken cancellationToken = default);
}
