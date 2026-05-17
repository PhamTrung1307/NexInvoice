namespace NexInvoice.Application.Features.WorkItems;

public sealed record TaskAttachmentResponse(
    Guid Id,
    string FileName,
    string FileUrl,
    string? ContentType,
    long SizeInBytes,
    Guid UploadedById,
    string? UploadedByName,
    DateTimeOffset CreatedAt);
