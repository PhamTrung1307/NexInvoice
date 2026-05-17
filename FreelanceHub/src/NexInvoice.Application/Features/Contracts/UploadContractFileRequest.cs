namespace NexInvoice.Application.Features.Contracts;

public sealed record UploadContractFileRequest(
    Stream Content,
    string FileName,
    string ContentType,
    long SizeInBytes);
