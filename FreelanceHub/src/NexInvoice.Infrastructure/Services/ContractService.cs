using NexInvoice.Application.Common.Exceptions;
using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Contracts;
using NexInvoice.Application.Interfaces;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;
using NexInvoice.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace NexInvoice.Infrastructure.Services;

internal sealed class ContractService : IContractService
{
    private const long MaxFileSizeInBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx" };
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ContractService(AppDbContext dbContext, IWebHostEnvironment webHostEnvironment)
    {
        _dbContext = dbContext;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<PagedResult<ContractResponse>> GetPagedAsync(
        ContractQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var contractsQuery = _dbContext.Contracts
            .AsNoTracking()
            .Include(contract => contract.Client)
            .Include(contract => contract.Project)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            contractsQuery = contractsQuery.Where(contract =>
                contract.ContractNumber.Contains(search)
                || contract.Title.Contains(search)
                || (contract.Client != null && contract.Client.Name.Contains(search))
                || (contract.Project != null && contract.Project.Name.Contains(search)));
        }

        if (query.Status.HasValue)
        {
            contractsQuery = contractsQuery.Where(contract => contract.Status == query.Status.Value);
        }

        var totalItems = await contractsQuery.CountAsync(cancellationToken);
        var items = await contractsQuery
            .OrderByDescending(contract => contract.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(contract => MapToResponse(contract))
            .ToArrayAsync(cancellationToken);

        return PagedResult<ContractResponse>.Create(items, page, pageSize, totalItems);
    }

    public async Task<ContractResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contract = await GetContractAsync(id, asNoTracking: true, cancellationToken);
        return MapToResponse(contract);
    }

    public async Task<ContractResponse> CreateAsync(CreateContractRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequired(request.ContractNumber, "Số hợp đồng là bắt buộc.");
        ValidateRequired(request.Title, "Tiêu đề hợp đồng là bắt buộc.");
        await ValidateRelationsAsync(request.ClientId, request.ProjectId, cancellationToken);

        var contract = new Contract
        {
            ContractNumber = request.ContractNumber.Trim(),
            Title = request.Title.Trim(),
            Description = NormalizeOptional(request.Description),
            Status = request.Status,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Amount = request.Amount,
            ClientId = request.ClientId,
            ProjectId = request.ProjectId
        };

        _dbContext.Contracts.Add(contract);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(contract.Id, cancellationToken);
    }

    public async Task<ContractResponse> UpdateAsync(Guid id, UpdateContractRequest request, CancellationToken cancellationToken = default)
    {
        var contract = await GetContractAsync(id, asNoTracking: false, cancellationToken);

        if (contract.Status == ContractStatus.Approved)
        {
            throw new BadRequestException("Hợp đồng đã được phê duyệt, không thể chỉnh sửa");
        }

        ValidateRequired(request.ContractNumber, "Số hợp đồng là bắt buộc.");
        ValidateRequired(request.Title, "Tiêu đề hợp đồng là bắt buộc.");
        await ValidateRelationsAsync(request.ClientId, request.ProjectId, cancellationToken);

        contract.ContractNumber = request.ContractNumber.Trim();
        contract.Title = request.Title.Trim();
        contract.Description = NormalizeOptional(request.Description);
        contract.Status = request.Status;
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.Amount = request.Amount;
        contract.ClientId = request.ClientId;
        contract.ProjectId = request.ProjectId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contract = await GetContractAsync(id, asNoTracking: false, cancellationToken);
        contract.IsDeleted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ContractResponse> UploadAsync(Guid id, UploadContractFileRequest request, CancellationToken cancellationToken = default)
    {
        ValidateFile(request);
        var contract = await GetContractAsync(id, asNoTracking: false, cancellationToken);

        var uploadsRoot = GetUploadsRoot();
        Directory.CreateDirectory(uploadsRoot);

        var originalFileName = Path.GetFileName(request.FileName);
        var storedFileName = CreateStoredContractFileName(request);
        var storedFilePath = Path.Combine(uploadsRoot, storedFileName);

        await using (var fileStream = File.Create(storedFilePath))
        {
            await request.Content.CopyToAsync(fileStream, cancellationToken);
        }

        contract.FileName = originalFileName;
        contract.FileUrl = $"/uploads/contracts/{storedFileName}";
        contract.FileContentType = request.ContentType;
        contract.FileSizeInBytes = request.SizeInBytes;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ContractFileResult> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contract = await GetContractAsync(id, asNoTracking: true, cancellationToken);
        if (string.IsNullOrWhiteSpace(contract.FileUrl) || string.IsNullOrWhiteSpace(contract.FileName))
        {
            throw new NotFoundException("Không tìm thấy tệp hợp đồng.");
        }

        var filePath = Path.Combine(GetUploadsRoot(), Path.GetFileName(contract.FileUrl));
        if (!File.Exists(filePath))
        {
            throw new NotFoundException("Không tìm thấy tệp hợp đồng.");
        }

        return new ContractFileResult(
            File.OpenRead(filePath),
            contract.FileName,
            contract.FileContentType ?? "application/octet-stream");
    }

    public async Task<ContractResponse> ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contract = await GetContractAsync(id, asNoTracking: false, cancellationToken);
        EnsureCanReview(contract);

        contract.Status = ContractStatus.Approved;
        contract.ApprovedAt = DateTimeOffset.UtcNow;
        contract.RejectedAt = null;
        contract.RejectReason = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ContractResponse> RejectAsync(Guid id, RejectContractRequest request, CancellationToken cancellationToken = default)
    {
        var reason = NormalizeOptional(request.Reason);
        if (reason is null)
        {
            throw new BadRequestException("Lý do từ chối hợp đồng là bắt buộc.");
        }

        var contract = await GetContractAsync(id, asNoTracking: false, cancellationToken);
        EnsureCanReview(contract);

        contract.Status = ContractStatus.Rejected;
        contract.RejectedAt = DateTimeOffset.UtcNow;
        contract.ApprovedAt = null;
        contract.RejectReason = reason;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task<Contract> GetContractAsync(Guid id, bool asNoTracking, CancellationToken cancellationToken)
    {
        var query = _dbContext.Contracts
            .Include(contract => contract.Client)
            .Include(contract => contract.Project)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(contract => contract.Id == id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy hợp đồng");
    }

    private async Task ValidateRelationsAsync(Guid clientId, Guid? projectId, CancellationToken cancellationToken)
    {
        var clientExists = await _dbContext.Clients.AnyAsync(client => client.Id == clientId, cancellationToken);
        if (!clientExists)
        {
            throw new BadRequestException("Khách hàng không tồn tại.");
        }

        if (projectId.HasValue)
        {
            var projectBelongsToClient = await _dbContext.Projects.AnyAsync(
                project => project.Id == projectId.Value && project.ClientId == clientId,
                cancellationToken);
            if (!projectBelongsToClient)
            {
                throw new BadRequestException("Dự án không tồn tại hoặc không thuộc khách hàng đã chọn.");
            }
        }
    }

    private static void EnsureCanReview(Contract contract)
    {
        if (contract.Status is not (ContractStatus.Draft or ContractStatus.Sent))
        {
            throw new BadRequestException("Chỉ có thể phê duyệt hoặc từ chối hợp đồng bản nháp hoặc đã gửi.");
        }
    }

    private static ContractResponse MapToResponse(Contract contract)
    {
        return new ContractResponse(
            contract.Id,
            contract.ContractNumber,
            contract.Title,
            contract.Description,
            contract.Status,
            contract.StartDate,
            contract.EndDate,
            contract.Amount,
            contract.ClientId,
            contract.Client?.Name ?? string.Empty,
            contract.ProjectId,
            contract.Project?.Name,
            contract.FileName,
            contract.FileUrl,
            contract.ApprovedAt,
            contract.RejectedAt,
            contract.RejectReason,
            contract.CreatedAt,
            contract.UpdatedAt);
    }

    private static void ValidateRequired(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BadRequestException(message);
        }
    }

    private static void ValidateFile(UploadContractFileRequest request)
    {
        if (request.Content.Length == 0 || request.SizeInBytes <= 0)
        {
            throw new BadRequestException("Tệp hợp đồng là bắt buộc.");
        }

        _ = CreateStoredContractFileName(request);
    }

    private static string CreateStoredContractFileName(UploadContractFileRequest request)
    {
        return FileUploadGuard.ValidateAndCreateStoredFileName(
            request.FileName,
            request.ContentType,
            request.SizeInBytes,
            AllowedExtensions,
            AllowedContentTypes,
            "Tệp hợp đồng là bắt buộc.",
            "Kích thước tệp không được vượt quá 10MB.",
            "Chỉ hỗ trợ tệp pdf hoặc docx.");
    }

    private string GetUploadsRoot()
    {
        return FileUploadGuard.ResolveUploadRoot(_webHostEnvironment, "uploads", "contracts");
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
