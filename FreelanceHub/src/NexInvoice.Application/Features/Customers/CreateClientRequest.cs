using NexInvoice.Domain.Enums;

namespace NexInvoice.Application.Features.Customers;

public sealed record CreateClientRequest(
    string FullName,
    string Email,
    string? PhoneNumber,
    string? CompanyName,
    string? Address,
    ClientStatus Status = ClientStatus.Active);
