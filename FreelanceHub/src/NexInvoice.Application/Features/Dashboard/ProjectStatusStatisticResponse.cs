using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Dashboard;

public sealed record ProjectStatusStatisticResponse(
    ProjectStatus Status,
    int Count);
