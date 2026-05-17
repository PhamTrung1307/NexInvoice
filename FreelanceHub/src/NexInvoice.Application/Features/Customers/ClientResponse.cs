using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Customers;

public sealed record ClientResponse(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? CompanyName,
    string? Address,
    ClientStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
