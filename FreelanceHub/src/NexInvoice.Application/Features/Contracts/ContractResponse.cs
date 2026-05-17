using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Contracts;

public sealed record ContractResponse(
    Guid Id,
    string ContractNumber,
    string Title,
    string? Description,
    ContractStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    decimal Amount,
    Guid ClientId,
    string ClientName,
    Guid? ProjectId,
    string? ProjectName,
    string? FileName,
    string? FileUrl,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? RejectedAt,
    string? RejectReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
