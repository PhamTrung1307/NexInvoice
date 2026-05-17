namespace NexInvoice.Application.Features.Reports;

public sealed class ReportQueryParameters
{
    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }

    public Guid? CustomerId { get; init; }

    public Guid? ProjectId { get; init; }
}
