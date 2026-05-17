namespace NexInvoice.Application.Features.Auth;

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string FullName,
    IReadOnlyCollection<string> Roles,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
