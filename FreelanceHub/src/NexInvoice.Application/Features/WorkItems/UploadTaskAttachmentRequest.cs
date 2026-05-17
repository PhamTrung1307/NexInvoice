namespace NexInvoice.Application.Features.WorkItems;

public sealed record UploadTaskAttachmentRequest(
    Stream Content,
    string FileName,
    string? ContentType,
    long SizeInBytes,
    Guid UploadedById);
