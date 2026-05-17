namespace NexInvoice.Application.Features.Invoices.Pdfs;

public sealed record PdfFileResponse(
    byte[] Content,
    string FileName,
    string ContentType);
