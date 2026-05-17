namespace NexInvoice.Application.Features.Contracts;

public sealed record ContractFileResult(
    Stream Content,
    string FileName,
    string ContentType);
