using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Payments;

public sealed record PaymentResponse(
    Guid Id,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    DateOnly PaymentDate,
    string? TransactionReference,
    string? ProofFileName,
    string? ProofFileUrl,
    string? ProofContentType,
    long? ProofSizeInBytes,
    DateTimeOffset? ConfirmedAt,
    Guid? ConfirmedBy,
    DateTimeOffset? RejectedAt,
    string? RejectReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
