namespace NexInvoice.Application.Features.Auth;

public sealed record LoginRequest(
    string Email,
    string Password);
