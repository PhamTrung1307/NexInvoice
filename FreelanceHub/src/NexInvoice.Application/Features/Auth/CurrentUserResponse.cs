namespace NexInvoice.Application.Features.Auth;

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
