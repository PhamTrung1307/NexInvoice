using DomainTaskStatus = NexInvoice.Domain.Enums.TaskStatus;
using NexInvoice.Application.Common.Exceptions;
using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Projects;
using NexInvoice.Application.Interfaces;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;
using NexInvoice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NexInvoice.Infrastructure.Services;

internal sealed class ProjectService : IProjectService
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext _dbContext;

    public ProjectService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ProjectListItemResponse>> GetPagedAsync(
        ProjectQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var page = queryParameters.Page < 1 ? 1 : queryParameters.Page;
        var pageSize = queryParameters.PageSize < 1 ? 10 : Math.Min(queryParameters.PageSize, MaxPageSize);

        var query = _dbContext.Projects
            .AsNoTracking()
            .Include(project => project.Client)
            .Include(project => project.Tasks)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParameters.Search))
        {
            var keyword = queryParameters.Search.Trim();

            query = query.Where(project =>
                project.Name.Contains(keyword)
                || (project.Client != null && project.Client.Name.Contains(keyword)));
        }

        if (queryParameters.Status.HasValue)
        {
            query = query.Where(project => project.Status == queryParameters.Status.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var projects = await query
            .OrderByDescending(project => project.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        var items = projects
            .Select(project => new ProjectListItemResponse(
                project.Id,
                project.Name,
                project.Status,
                project.StartDate,
                project.EndDate,
                project.Budget,
                project.ClientId,
                project.Client?.Name ?? string.Empty,
                GetTotalTasks(project),
                GetCompletedTasks(project),
                CalculateProgress(project),
                project.CreatedAt))
            .ToArray();

        return PagedResult<ProjectListItemResponse>.Create(items, page, pageSize, totalItems);
    }

    public async Task<ProjectResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await GetProjectWithDetailsAsync(id, asNoTracking: true, cancellationToken);

        if (project is null)
        {
            throw new NotFoundException("Không tìm thấy dự án.");
        }

        return MapToResponse(project);
    }

    public async Task<ProjectResponse> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateProjectRequest(
            request.Name,
            request.StartDate,
            request.EndDate,
            request.Budget,
            request.ClientId);

        var client = await _dbContext.Clients
            .FirstOrDefaultAsync(currentClient => currentClient.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            throw new NotFoundException("Không tìm thấy khách hàng.");
        }

        if (client.Status != ClientStatus.Active)
        {
            throw new BadRequestException("Không thể tạo dự án cho khách hàng không hoạt động.");
        }

        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Budget = request.Budget,
            ClientId = request.ClientId,
            Status = request.Status
        };

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync(cancellationToken);

        project.Client = client;
        return MapToResponse(project);
    }

    public async Task<ProjectResponse> UpdateAsync(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateProjectRequest(
            request.Name,
            request.StartDate,
            request.EndDate,
            request.Budget,
            request.ClientId);

        var project = await GetProjectWithDetailsAsync(id, asNoTracking: false, cancellationToken);

        if (project is null)
        {
            throw new NotFoundException("Không tìm thấy dự án.");
        }

        var client = await _dbContext.Clients
            .FirstOrDefaultAsync(currentClient => currentClient.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            throw new NotFoundException("Không tìm thấy khách hàng.");
        }

        if (client.Status != ClientStatus.Active)
        {
            throw new BadRequestException("Không thể cập nhật dự án cho khách hàng không hoạt động.");
        }

        EnsureCanSetStatus(project, request.Status);

        project.Name = request.Name.Trim();
        project.Description = NormalizeOptional(request.Description);
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.Budget = request.Budget;
        project.ClientId = request.ClientId;
        project.Client = client;
        project.Status = request.Status;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(project);
    }

    public async Task<ProjectResponse> UpdateStatusAsync(
        Guid id,
        UpdateProjectStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var project = await GetProjectWithDetailsAsync(id, asNoTracking: false, cancellationToken);

        if (project is null)
        {
            throw new NotFoundException("Không tìm thấy dự án.");
        }

        EnsureCanSetStatus(project, request.Status);

        project.Status = request.Status;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(project);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(currentProject => currentProject.Id == id, cancellationToken);

        if (project is null)
        {
            throw new NotFoundException("Không tìm thấy dự án.");
        }

        project.IsDeleted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Project?> GetProjectWithDetailsAsync(
        Guid id,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Projects
            .Include(project => project.Client)
            .Include(project => project.Tasks)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(project => project.Id == id, cancellationToken);
    }

    private static ProjectResponse MapToResponse(Project project)
    {
        return new ProjectResponse(
            project.Id,
            project.Name,
            project.Description,
            project.Status,
            project.StartDate,
            project.EndDate,
            project.Budget,
            project.ClientId,
            project.Client?.Name ?? string.Empty,
            GetTotalTasks(project),
            GetCompletedTasks(project),
            CalculateProgress(project),
            project.CreatedAt,
            project.UpdatedAt);
    }

    private static void ValidateProjectRequest(
        string name,
        DateOnly? startDate,
        DateOnly? endDate,
        decimal budget,
        Guid clientId)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Tên dự án là bắt buộc.");
        }

        if (clientId == Guid.Empty)
        {
            errors.Add("Khách hàng là bắt buộc.");
        }

        if (budget < 0)
        {
            errors.Add("Ngân sách dự án phải lớn hơn hoặc bằng 0.");
        }

        if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
        {
            errors.Add("Hạn hoàn thành phải lớn hơn hoặc bằng ngày bắt đầu.");
        }

        if (errors.Count > 0)
        {
            throw new BadRequestException("Dữ liệu dự án không hợp lệ.", errors);
        }
    }

    private static void EnsureCanSetStatus(Project project, ProjectStatus status)
    {
        if (status == ProjectStatus.Completed && HasUnfinishedTasks(project))
        {
            throw new BadRequestException("Không thể hoàn thành dự án khi vẫn còn công việc chưa hoàn tất.");
        }
    }

    private static bool HasUnfinishedTasks(Project project)
    {
        return project.Tasks.Any(task => task.Status != DomainTaskStatus.Done);
    }

    private static int GetTotalTasks(Project project)
    {
        return project.Tasks.Count;
    }

    private static int GetCompletedTasks(Project project)
    {
        return project.Tasks.Count(task => task.Status == DomainTaskStatus.Done);
    }

    private static decimal CalculateProgress(Project project)
    {
        var totalTasks = GetTotalTasks(project);

        if (totalTasks == 0)
        {
            return 0;
        }

        return Math.Round(GetCompletedTasks(project) * 100m / totalTasks, 2);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
