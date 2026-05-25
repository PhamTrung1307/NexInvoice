using DomainTaskStatus = NexInvoice.Domain.Enums.TaskStatus;
using NexInvoice.Application.Common.Exceptions;
using NexInvoice.Application.Features.WorkItems;
using NexInvoice.Application.Interfaces;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;
using NexInvoice.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace NexInvoice.Infrastructure.Services;

internal sealed class TaskService : ITaskService
{
    private const long MaxFileSizeInBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".pdf",
        ".docx",
        ".xlsx"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };

    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IRealtimeNotificationService _realtimeNotificationService;

    public TaskService(
        AppDbContext dbContext,
        IWebHostEnvironment webHostEnvironment,
        IRealtimeNotificationService realtimeNotificationService)
    {
        _dbContext = dbContext;
        _webHostEnvironment = webHostEnvironment;
        _realtimeNotificationService = realtimeNotificationService;
    }

    public async Task<IReadOnlyCollection<TaskListItemResponse>> GetByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var projectExists = await _dbContext.Projects
            .AnyAsync(project => project.Id == projectId, cancellationToken);

        if (!projectExists)
        {
            throw new NotFoundException("Không tìm thấy dự án.");
        }

        return await _dbContext.TaskItems
            .AsNoTracking()
            .Include(task => task.AssignedTo)
            .Where(task => task.ProjectId == projectId)
            .OrderByDescending(task => task.CreatedAt)
            .Select(task => new TaskListItemResponse(
                task.Id,
                task.Title,
                task.Status,
                task.Priority,
                task.DueDate,
                task.AssignedToId,
                task.AssignedTo != null ? task.AssignedTo.FullName : null,
                task.CreatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskWithDetailsAsync(id, asNoTracking: true, cancellationToken);

        if (task is null)
        {
            throw new NotFoundException("Không tìm thấy công việc.");
        }

        return MapToResponse(task);
    }

    public async Task<TaskResponse> CreateAsync(
        Guid projectId,
        CreateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTaskRequest(request.Title);

        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(currentProject => currentProject.Id == projectId, cancellationToken);

        if (project is null)
        {
            throw new NotFoundException("Không tìm thấy dự án.");
        }

        await EnsureUserExistsAsync(request.AssignedToId, cancellationToken);

        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = NormalizeOptional(request.Description),
            Status = request.Status,
            Priority = request.Priority,
            DueDate = request.DueDate,
            ProjectId = projectId,
            AssignedToId = request.AssignedToId
        };

        _dbContext.TaskItems.Add(task);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(task.Id, cancellationToken);
    }

    public async Task<TaskResponse> UpdateAsync(
        Guid id,
        UpdateTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateTaskRequest(request.Title);

        var task = await GetTaskWithDetailsAsync(id, asNoTracking: false, cancellationToken);

        if (task is null)
        {
            throw new NotFoundException("Không tìm thấy công việc.");
        }

        await EnsureUserExistsAsync(request.AssignedToId, cancellationToken);

        var oldStatus = task.Status;
        task.Title = request.Title.Trim();
        task.Description = NormalizeOptional(request.Description);
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.AssignedToId = request.AssignedToId;

        var notification = CreateStatusChangedNotificationIfNeeded(task, oldStatus, request.Status);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SendNotificationIfNeededAsync(notification, cancellationToken);

        return await GetByIdAsync(task.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.TaskItems
            .FirstOrDefaultAsync(currentTask => currentTask.Id == id, cancellationToken);

        if (task is null)
        {
            throw new NotFoundException("Không tìm thấy công việc.");
        }

        task.IsDeleted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TaskResponse> UpdateStatusAsync(
        Guid id,
        UpdateTaskStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await GetTaskWithDetailsAsync(id, asNoTracking: false, cancellationToken);

        if (task is null)
        {
            throw new NotFoundException("Không tìm thấy công việc.");
        }

        var oldStatus = task.Status;
        task.Status = request.Status;

        var notification = CreateStatusChangedNotificationIfNeeded(task, oldStatus, request.Status);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SendNotificationIfNeededAsync(notification, cancellationToken);

        return await GetByIdAsync(task.Id, cancellationToken);
    }

    public async Task<TaskResponse> UpdatePriorityAsync(
        Guid id,
        UpdateTaskPriorityRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.TaskItems
            .FirstOrDefaultAsync(currentTask => currentTask.Id == id, cancellationToken);

        if (task is null)
        {
            throw new NotFoundException("Không tìm thấy công việc.");
        }

        task.Priority = request.Priority;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(task.Id, cancellationToken);
    }

    public async Task<TaskResponse> AssignAsync(
        Guid id,
        AssignTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.TaskItems
            .FirstOrDefaultAsync(currentTask => currentTask.Id == id, cancellationToken);

        if (task is null)
        {
            throw new NotFoundException("Không tìm thấy công việc.");
        }

        await EnsureUserExistsAsync(request.AssignedToId, cancellationToken);

        task.AssignedToId = request.AssignedToId;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(task.Id, cancellationToken);
    }

    public async Task<TaskCommentResponse> AddCommentAsync(
        Guid id,
        AddTaskCommentRequest request,
        Guid authorId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new BadRequestException("Nội dung bình luận là bắt buộc.");
        }

        var taskExists = await _dbContext.TaskItems
            .AnyAsync(task => task.Id == id, cancellationToken);

        if (!taskExists)
        {
            throw new NotFoundException("Không tìm thấy công việc.");
        }

        await EnsureRequiredUserExistsAsync(authorId, "Không tìm thấy người bình luận.", cancellationToken);

        var comment = new TaskComment
        {
            TaskItemId = id,
            AuthorId = authorId,
            Content = request.Content.Trim()
        };

        _dbContext.TaskComments.Add(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var savedComment = await _dbContext.TaskComments
            .AsNoTracking()
            .Include(currentComment => currentComment.Author)
            .FirstAsync(currentComment => currentComment.Id == comment.Id, cancellationToken);

        return MapComment(savedComment);
    }

    public async Task<TaskAttachmentResponse> UploadAttachmentAsync(
        Guid id,
        UploadTaskAttachmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateAttachment(request);

        var taskExists = await _dbContext.TaskItems
            .AnyAsync(task => task.Id == id, cancellationToken);

        if (!taskExists)
        {
            throw new NotFoundException("Không tìm thấy công việc.");
        }

        await EnsureRequiredUserExistsAsync(request.UploadedById, "Không tìm thấy người tải tệp.", cancellationToken);

        var uploadsRoot = GetTaskUploadsRoot();
        Directory.CreateDirectory(uploadsRoot);

        var originalFileName = Path.GetFileName(request.FileName);
        var storedFileName = CreateStoredAttachmentFileName(request);
        var storedFilePath = Path.Combine(uploadsRoot, storedFileName);

        await using (var fileStream = File.Create(storedFilePath))
        {
            await request.Content.CopyToAsync(fileStream, cancellationToken);
        }

        var attachment = new TaskAttachment
        {
            TaskItemId = id,
            UploadedById = request.UploadedById,
            FileName = originalFileName,
            FileUrl = $"/uploads/tasks/{storedFileName}",
            ContentType = request.ContentType,
            SizeInBytes = request.SizeInBytes
        };

        _dbContext.TaskAttachments.Add(attachment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var savedAttachment = await _dbContext.TaskAttachments
            .AsNoTracking()
            .Include(currentAttachment => currentAttachment.UploadedBy)
            .FirstAsync(currentAttachment => currentAttachment.Id == attachment.Id, cancellationToken);

        return MapAttachment(savedAttachment);
    }

    private async Task<TaskItem?> GetTaskWithDetailsAsync(
        Guid id,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.TaskItems
            .Include(task => task.Project)
            .Include(task => task.AssignedTo)
            .Include(task => task.Comments)
            .ThenInclude(comment => comment.Author)
            .Include(task => task.Attachments)
            .ThenInclude(attachment => attachment.UploadedBy)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
    }

    private static TaskResponse MapToResponse(TaskItem task)
    {
        return new TaskResponse(
            task.Id,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.DueDate,
            task.ProjectId,
            task.Project?.Name ?? string.Empty,
            task.AssignedToId,
            task.AssignedTo?.FullName,
            task.Comments
                .OrderBy(comment => comment.CreatedAt)
                .Select(MapComment)
                .ToArray(),
            task.Attachments
                .OrderBy(attachment => attachment.CreatedAt)
                .Select(MapAttachment)
                .ToArray(),
            task.CreatedAt,
            task.UpdatedAt);
    }

    private static TaskCommentResponse MapComment(TaskComment comment)
    {
        return new TaskCommentResponse(
            comment.Id,
            comment.Content,
            comment.AuthorId,
            comment.Author?.FullName,
            comment.CreatedAt);
    }

    private static TaskAttachmentResponse MapAttachment(TaskAttachment attachment)
    {
        return new TaskAttachmentResponse(
            attachment.Id,
            attachment.FileName,
            attachment.FileUrl,
            attachment.ContentType,
            attachment.SizeInBytes,
            attachment.UploadedById,
            attachment.UploadedBy?.FullName,
            attachment.CreatedAt);
    }

    private Notification? CreateStatusChangedNotificationIfNeeded(
        TaskItem task,
        DomainTaskStatus oldStatus,
        DomainTaskStatus newStatus)
    {
        if (oldStatus == newStatus || task.AssignedToId is null)
        {
            return null;
        }

        var notification = new Notification
        {
            UserId = task.AssignedToId.Value,
            Title = "Trạng thái công việc đã thay đổi",
            Message = $"Công việc '{task.Title}' đã được cập nhật trạng thái thành {newStatus}.",
            Type = NotificationType.Task
        };

        _dbContext.Notifications.Add(notification);

        return notification;
    }

    private async Task SendNotificationIfNeededAsync(
        Notification? notification,
        CancellationToken cancellationToken)
    {
        if (notification is null)
        {
            return;
        }

        await _realtimeNotificationService.SendToUserAsync(
            notification.UserId,
            new Application.Features.Notifications.NotificationResponse(
                notification.Id,
                notification.Title,
                notification.Message,
                notification.Type,
                notification.IsRead,
                notification.ReadAt,
                notification.CreatedAt),
            cancellationToken);
    }

    private async Task EnsureUserExistsAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is null)
        {
            return;
        }

        await EnsureRequiredUserExistsAsync(userId.Value, "Không tìm thấy người được giao.", cancellationToken);
    }

    private async Task EnsureRequiredUserExistsAsync(
        Guid userId,
        string message,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new BadRequestException(message);
        }

        var exists = await _dbContext.AppUsers
            .AnyAsync(user => user.Id == userId && user.IsActive, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException(message);
        }
    }

    private static void ValidateTaskRequest(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new BadRequestException("Tiêu đề công việc là bắt buộc.");
        }
    }

    private static void ValidateAttachment(UploadTaskAttachmentRequest request)
    {
        if (request.Content.Length == 0 || request.SizeInBytes <= 0)
        {
            throw new BadRequestException("Tệp tải lên là bắt buộc.");
        }

        _ = CreateStoredAttachmentFileName(request);
    }

    private static string CreateStoredAttachmentFileName(UploadTaskAttachmentRequest request)
    {
        return FileUploadGuard.ValidateAndCreateStoredFileName(
            request.FileName,
            request.ContentType,
            request.SizeInBytes,
            AllowedExtensions,
            AllowedContentTypes,
            "Tệp tải lên là bắt buộc.",
            "Kích thước tệp không được vượt quá 10MB.",
            "Định dạng tệp không được hỗ trợ.");
    }

    private string GetTaskUploadsRoot()
    {
        return FileUploadGuard.ResolveUploadRoot(_webHostEnvironment, "uploads", "tasks");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
