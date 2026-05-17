using NexInvoice.Application.Features.Reports;
using NexInvoice.Application.Interfaces;
using NexInvoice.Domain.Enums;
using NexInvoice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NexInvoice.Infrastructure.Services;

internal sealed class ReportService : IReportService
{
    private readonly AppDbContext _dbContext;

    public ReportService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RevenueReportResponse> GetRevenueAsync(
        ReportQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var invoicesQuery = ApplyInvoiceFilters(_dbContext.Invoices.AsNoTracking(), query);
        var invoices = await invoicesQuery.ToArrayAsync(cancellationToken);

        var paidRevenue = invoices
            .Where(invoice => invoice.Status == InvoiceStatus.Paid)
            .Sum(invoice => invoice.TotalAmount);
        var pendingRevenue = invoices
            .Where(invoice => invoice.Status is InvoiceStatus.Sent or InvoiceStatus.PartiallyPaid)
            .Sum(invoice => invoice.TotalAmount);

        var monthlyRevenue = invoices
            .Where(invoice => invoice.Status == InvoiceStatus.Paid)
            .GroupBy(invoice => new { invoice.IssueDate.Year, invoice.IssueDate.Month })
            .OrderBy(group => group.Key.Year)
            .ThenBy(group => group.Key.Month)
            .Select(group => new MonthlyRevenueItem(
                $"{group.Key.Month:00}/{group.Key.Year}",
                group.Sum(invoice => invoice.TotalAmount)))
            .ToArray();

        return new RevenueReportResponse(
            invoices.Sum(invoice => invoice.TotalAmount),
            paidRevenue,
            pendingRevenue,
            invoices.Count(invoice => invoice.Status == InvoiceStatus.Overdue),
            monthlyRevenue);
    }

    public async Task<InvoiceStatusReportResponse> GetInvoiceStatusAsync(
        ReportQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var items = await ApplyInvoiceFilters(_dbContext.Invoices.AsNoTracking(), query)
            .GroupBy(invoice => invoice.Status)
            .Select(group => new StatusCountItem(
                group.Key.ToString(),
                group.Count(),
                group.Sum(invoice => invoice.TotalAmount)))
            .ToArrayAsync(cancellationToken);

        return new InvoiceStatusReportResponse(items);
    }

    public async Task<ProjectProgressReportResponse> GetProjectProgressAsync(
        ReportQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var projectsQuery = _dbContext.Projects.AsNoTracking().Include(project => project.Tasks).AsQueryable();
        if (query.CustomerId.HasValue)
        {
            projectsQuery = projectsQuery.Where(project => project.ClientId == query.CustomerId.Value);
        }

        if (query.ProjectId.HasValue)
        {
            projectsQuery = projectsQuery.Where(project => project.Id == query.ProjectId.Value);
        }

        var projects = await projectsQuery
            .OrderByDescending(project => project.CreatedAt)
            .Take(50)
            .ToArrayAsync(cancellationToken);

        var projectItems = projects.Select(project =>
        {
            var totalTasks = project.Tasks.Count;
            var completedTasks = project.Tasks.Count(task => task.Status == Domain.Enums.TaskStatus.Done);
            var progress = totalTasks == 0 ? 0 : (int)Math.Round(completedTasks * 100m / totalTasks);

            return new ProjectProgressItem(
                project.Id,
                project.Name,
                project.Status.ToString(),
                progress,
                totalTasks,
                completedTasks);
        }).ToArray();

        var statusSummary = projects
            .GroupBy(project => project.Status)
            .Select(group => new ProjectStatusItem(group.Key.ToString(), group.Count()))
            .ToArray();

        return new ProjectProgressReportResponse(projectItems, statusSummary);
    }

    public async Task<CustomerRevenueReportResponse> GetCustomerRevenueAsync(
        ReportQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var invoicesQuery = ApplyInvoiceFilters(
            _dbContext.Invoices.AsNoTracking(),
            query);

        var customerRows = await invoicesQuery
            .Where(invoice => invoice.Status == InvoiceStatus.Paid)
            .GroupBy(invoice => new
            {
                invoice.ClientId,
                ClientName = invoice.Client != null ? invoice.Client.Name : string.Empty
            })
            .Select(group => new
            {
                group.Key.ClientId,
                group.Key.ClientName,
                Revenue = group.Sum(invoice => invoice.TotalAmount),
                InvoiceCount = group.Count()
            })
            .OrderByDescending(item => item.Revenue)
            .Take(10)
            .ToListAsync(cancellationToken);

        var customers = customerRows
            .Select(item => new CustomerRevenueItem(
                item.ClientId,
                item.ClientName,
                item.Revenue,
                item.InvoiceCount))
            .ToArray();

        return new CustomerRevenueReportResponse(customers);
    }

    private static IQueryable<Domain.Entities.Invoice> ApplyInvoiceFilters(
        IQueryable<Domain.Entities.Invoice> query,
        ReportQueryParameters parameters)
    {
        if (parameters.FromDate.HasValue)
        {
            query = query.Where(invoice => invoice.IssueDate >= parameters.FromDate.Value);
        }

        if (parameters.ToDate.HasValue)
        {
            query = query.Where(invoice => invoice.IssueDate <= parameters.ToDate.Value);
        }

        if (parameters.CustomerId.HasValue)
        {
            query = query.Where(invoice => invoice.ClientId == parameters.CustomerId.Value);
        }

        if (parameters.ProjectId.HasValue)
        {
            query = query.Where(invoice => invoice.ProjectId == parameters.ProjectId.Value);
        }

        return query;
    }
}
