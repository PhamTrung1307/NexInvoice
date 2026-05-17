using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Contracts;

public sealed record UpdateContractRequest(
    string ContractNumber,
    string Title,
    string? Description,
    ContractStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    decimal Amount,
    Guid ClientId,
    Guid? ProjectId);
