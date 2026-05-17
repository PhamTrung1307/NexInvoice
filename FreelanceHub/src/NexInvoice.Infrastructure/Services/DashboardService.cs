using System.Text.Json;
using NexInvoice.Application.Features.Dashboard;
using NexInvoice.Application.Interfaces;
using NexInvoice.Domain.Enums;
using NexInvoice.Infrastructure.Caching;
using NexInvoice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace NexInvoice.Infrastructure.Services;

internal sealed class DashboardService : IDashboardService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _dbContext;
    private readonly IDistributedCache _cache;

    public DashboardService(AppDbContext dbContext, IDistributedCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetStringAsync(DashboardCacheKeys.Summary, cancellationToken);

        if (!string.IsNullOrWhiteSpace(cached))
        {
            var cachedResponse = JsonSerializer.Deserialize<DashboardSummaryResponse>(
                cached,
                JsonSerializerOptions);

            if (cachedResponse is not null)
            {
                return cachedResponse;
            }
        }

        var response = await BuildSummaryAsync(cancellationToken);
        var serialized = JsonSerializer.Serialize(response, JsonSerializerOptions);

        await _cache.SetStringAsync(
            DashboardCacheKeys.Summary,
            serialized,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            },
            cancellationToken);

        return response;
    }

    private async Task<DashboardSummaryResponse> BuildSummaryAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-11);

        var totalClients = await _dbContext.Clients
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var totalProjects = await _dbContext.Projects
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var totalInvoices = await _dbContext.Invoices
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var overdueInvoices = await _dbContext.Invoices
            .AsNoTracking()
            .CountAsync(invoice => invoice.Status == InvoiceStatus.Overdue, cancellationToken);

        var pendingPayments = await _dbContext.Payments
            .AsNoTracking()
            .CountAsync(payment => payment.Status == PaymentStatus.Pending, cancellationToken);

        var totalRevenue = await _dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.Status == PaymentStatus.Confirmed)
            .SumAsync(payment => (decimal?)payment.Amount, cancellationToken);

        var confirmedPayments = await _dbContext.Payments
            .AsNoTracking()
            .Where(payment =>
                payment.Status == PaymentStatus.Confirmed
                && payment.PaymentDate >= startMonth)
            .Select(payment => new
            {
                payment.PaymentDate,
                payment.Amount
            })
            .ToArrayAsync(cancellationToken);

        var projectStatusStatistics = await _dbContext.Projects
            .AsNoTracking()
            .GroupBy(project => project.Status)
            .Select(group => new ProjectStatusStatisticResponse(group.Key, group.Count()))
            .ToArrayAsync(cancellationToken);

        var monthlyRevenue = Enumerable.Range(0, 12)
            .Select(offset => startMonth.AddMonths(offset))
            .Select(month =>
            {
                var revenue = confirmedPayments
                    .Where(payment =>
                        payment.PaymentDate.Year == month.Year
                        && payment.PaymentDate.Month == month.Month)
                    .Sum(payment => payment.Amount);

                return new MonthlyRevenueResponse(month.Year, month.Month, revenue);
            })
            .ToArray();

        return new DashboardSummaryResponse(
            totalClients,
            totalProjects,
            totalInvoices,
            totalRevenue ?? 0,
            overdueInvoices,
            pendingPayments,
            monthlyRevenue,
            projectStatusStatistics);
    }
}
