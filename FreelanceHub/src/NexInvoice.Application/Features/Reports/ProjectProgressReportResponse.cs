namespace NexInvoice.Application.Features.Reports;

public sealed record ProjectProgressReportResponse(
    IReadOnlyCollection<ProjectProgressItem> Projects,
    IReadOnlyCollection<ProjectStatusItem> StatusSummary);

public sealed record ProjectProgressItem(
    Guid ProjectId,
    string ProjectName,
    string Status,
    int ProgressPercentage,
    int TotalTasks,
    int CompletedTasks);

public sealed record ProjectStatusItem(string Status, int Count);
