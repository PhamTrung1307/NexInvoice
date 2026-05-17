namespace NexInvoice.Application.Common.Settings;

public sealed class JwtSettings
{
    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string Secret { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; init; } = 60;

    public int RefreshTokenExpirationDays { get; init; } = 7;
}
