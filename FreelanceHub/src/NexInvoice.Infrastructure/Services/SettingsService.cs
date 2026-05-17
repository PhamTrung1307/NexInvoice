using NexInvoice.Application.Common.Exceptions;
using NexInvoice.Application.Features.Settings;
using NexInvoice.Application.Interfaces;
using NexInvoice.Domain.Entities;
using NexInvoice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NexInvoice.Infrastructure.Services;

internal sealed class SettingsService : ISettingsService
{
    private readonly AppDbContext _dbContext;

    public SettingsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CompanyProfileResponse> GetCompanyProfileAsync(CancellationToken cancellationToken = default)
    {
        var profile = await GetOrCreateCompanyProfileAsync(cancellationToken);
        return MapCompany(profile);
    }

    public async Task<CompanyProfileResponse> UpdateCompanyProfileAsync(
        UpdateCompanyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName))
        {
            throw new BadRequestException("Tên công ty là bắt buộc.");
        }

        var profile = await GetOrCreateCompanyProfileAsync(cancellationToken);
        profile.CompanyName = request.CompanyName.Trim();
        profile.TaxCode = NormalizeOptional(request.TaxCode);
        profile.Email = NormalizeOptional(request.Email);
        profile.Phone = NormalizeOptional(request.Phone);
        profile.Address = NormalizeOptional(request.Address);
        profile.Website = NormalizeOptional(request.Website);
        profile.LogoUrl = NormalizeOptional(request.LogoUrl);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapCompany(profile);
    }

    public async Task<SystemPreferenceResponse> GetSystemPreferencesAsync(CancellationToken cancellationToken = default)
    {
        var preferences = await GetOrCreatePreferencesAsync(cancellationToken);
        return MapPreferences(preferences);
    }

    public async Task<SystemPreferenceResponse> UpdateSystemPreferencesAsync(
        UpdateSystemPreferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Currency)
            || string.IsNullOrWhiteSpace(request.DateFormat)
            || string.IsNullOrWhiteSpace(request.TimeZone)
            || string.IsNullOrWhiteSpace(request.InvoicePrefix))
        {
            throw new BadRequestException("Thông tin cấu hình hệ thống chưa hợp lệ.");
        }

        if (request.DefaultTaxRate < 0 || request.PaymentReminderDays < 0)
        {
            throw new BadRequestException("Thuế mặc định và số ngày nhắc thanh toán phải lớn hơn hoặc bằng 0.");
        }

        var preferences = await GetOrCreatePreferencesAsync(cancellationToken);
        preferences.Currency = request.Currency.Trim();
        preferences.DateFormat = request.DateFormat.Trim();
        preferences.TimeZone = request.TimeZone.Trim();
        preferences.InvoicePrefix = request.InvoicePrefix.Trim();
        preferences.DefaultTaxRate = request.DefaultTaxRate;
        preferences.PaymentReminderDays = request.PaymentReminderDays;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapPreferences(preferences);
    }

    private async Task<CompanyProfile> GetOrCreateCompanyProfileAsync(CancellationToken cancellationToken)
    {
        var profile = await _dbContext.CompanyProfiles.FirstOrDefaultAsync(cancellationToken);
        if (profile is not null)
        {
            return profile;
        }

        profile = new CompanyProfile { CompanyName = "NexInvoice" };
        _dbContext.CompanyProfiles.Add(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    private async Task<SystemPreference> GetOrCreatePreferencesAsync(CancellationToken cancellationToken)
    {
        var preferences = await _dbContext.SystemPreferences.FirstOrDefaultAsync(cancellationToken);
        if (preferences is not null)
        {
            return preferences;
        }

        preferences = new SystemPreference();
        _dbContext.SystemPreferences.Add(preferences);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return preferences;
    }

    private static CompanyProfileResponse MapCompany(CompanyProfile profile)
    {
        return new CompanyProfileResponse(
            profile.Id,
            profile.CompanyName,
            profile.TaxCode,
            profile.Email,
            profile.Phone,
            profile.Address,
            profile.Website,
            profile.LogoUrl);
    }

    private static SystemPreferenceResponse MapPreferences(SystemPreference preferences)
    {
        return new SystemPreferenceResponse(
            preferences.Id,
            preferences.Currency,
            preferences.DateFormat,
            preferences.TimeZone,
            preferences.InvoicePrefix,
            preferences.DefaultTaxRate,
            preferences.PaymentReminderDays);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
