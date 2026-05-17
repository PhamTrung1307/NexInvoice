using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Customers;

public sealed class ClientQueryParameters
{
    public string? Search { get; set; }

    public ClientStatus? Status { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;
}
