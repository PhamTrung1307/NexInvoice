namespace NexInvoice.Application.Features.WorkItems;

public sealed record TaskCommentResponse(
    Guid Id,
    string Content,
    Guid AuthorId,
    string? AuthorName,
    DateTimeOffset CreatedAt);
