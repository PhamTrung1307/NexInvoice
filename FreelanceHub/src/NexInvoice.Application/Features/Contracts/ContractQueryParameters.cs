using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Contracts;

public sealed class ContractQueryParameters
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? Search { get; init; }

    public ContractStatus? Status { get; init; }
}
