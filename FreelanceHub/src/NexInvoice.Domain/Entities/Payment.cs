using NexInvoice.Domain.Common;
using NexInvoice.Domain.Enums;

namespace NexInvoice.Domain.Entities;

public class Payment : BaseEntity
{
    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;

    public DateOnly PaymentDate { get; set; }

    public string? TransactionReference { get; set; }

    public string? ProofFileName { get; set; }

    public string? ProofFileUrl { get; set; }

    public string? ProofContentType { get; set; }

    public long? ProofSizeInBytes { get; set; }

    public DateTimeOffset? ConfirmedAt { get; set; }

    public Guid? ConfirmedBy { get; set; }

    public DateTimeOffset? RejectedAt { get; set; }

    public string? RejectReason { get; set; }

    public Guid InvoiceId { get; set; }

    public Invoice? Invoice { get; set; }
}
