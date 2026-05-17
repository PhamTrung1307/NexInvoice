using NexInvoice.Application.Features.Settings;

namespace NexInvoice.Application.Interfaces;

public interface ISettingsService
{
    Task<CompanyProfileResponse> GetCompanyProfileAsync(CancellationToken cancellationToken = default);

    Task<CompanyProfileResponse> UpdateCompanyProfileAsync(UpdateCompanyProfileRequest request, CancellationToken cancellationToken = default);

    Task<SystemPreferenceResponse> GetSystemPreferencesAsync(CancellationToken cancellationToken = default);

    Task<SystemPreferenceResponse> UpdateSystemPreferencesAsync(UpdateSystemPreferenceRequest request, CancellationToken cancellationToken = default);
}
