namespace NexInvoice.Application.Features.Settings;

public sealed record UpdateCompanyProfileRequest(
    string CompanyName,
    string? TaxCode,
    string? Email,
    string? Phone,
    string? Address,
    string? Website,
    string? LogoUrl);
