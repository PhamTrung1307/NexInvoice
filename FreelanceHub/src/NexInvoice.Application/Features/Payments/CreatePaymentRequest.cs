using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Payments;

public sealed record CreatePaymentRequest(
    Guid InvoiceId,
    decimal Amount,
    PaymentMethod Method,
    DateOnly PaymentDate,
    string? TransactionReference);
