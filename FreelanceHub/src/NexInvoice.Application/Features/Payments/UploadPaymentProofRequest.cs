namespace NexInvoice.Application.Features.Payments;

public sealed record UploadPaymentProofRequest(
    Stream Content,
    string FileName,
    string? ContentType,
    long SizeInBytes);
