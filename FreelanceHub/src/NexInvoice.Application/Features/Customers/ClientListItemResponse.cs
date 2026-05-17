using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Customers;

public sealed record ClientListItemResponse(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? CompanyName,
    ClientStatus Status,
    DateTimeOffset CreatedAt);
