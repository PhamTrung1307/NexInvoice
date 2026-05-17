using NexInvoice.Application.Common.Exceptions;
using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Customers;
using NexInvoice.Application.Interfaces;
using NexInvoice.Domain.Entities;
using NexInvoice.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace NexInvoice.Infrastructure.Services;

internal sealed class ClientService : IClientService
{
    private const int MaxPageSize = 100;

    private readonly AppDbContext _dbContext;

    public ClientService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<ClientListItemResponse>> GetPagedAsync(
        ClientQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var page = queryParameters.Page < 1 ? 1 : queryParameters.Page;
        var pageSize = queryParameters.PageSize < 1 ? 10 : Math.Min(queryParameters.PageSize, MaxPageSize);

        var query = _dbContext.Clients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(queryParameters.Search))
        {
            var keyword = queryParameters.Search.Trim();

            query = query.Where(client =>
                client.Name.Contains(keyword)
                || client.Email.Contains(keyword)
                || (client.PhoneNumber != null && client.PhoneNumber.Contains(keyword))
                || (client.CompanyName != null && client.CompanyName.Contains(keyword)));
        }

        if (queryParameters.Status.HasValue)
        {
            query = query.Where(client => client.Status == queryParameters.Status.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(client => client.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(client => new ClientListItemResponse(
                client.Id,
                client.Name,
                client.Email,
                client.PhoneNumber,
                client.CompanyName,
                client.Status,
                client.CreatedAt))
            .ToArrayAsync(cancellationToken);

        return PagedResult<ClientListItemResponse>.Create(items, page, pageSize, totalItems);
    }

    public async Task<ClientResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var client = await _dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(currentClient => currentClient.Id == id, cancellationToken);

        if (client is null)
        {
            throw new NotFoundException("Không tìm thấy khách hàng.");
        }

        return MapToResponse(client);
    }

    public async Task<ClientResponse> CreateAsync(
        CreateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);

        var email = NormalizeEmail(request.Email);
        var emailExists = await _dbContext.Clients
            .AnyAsync(client => client.Email == email, cancellationToken);

        if (emailExists)
        {
            throw new BadRequestException("Email khách hàng đã được sử dụng.");
        }

        var client = new Client
        {
            Name = request.FullName.Trim(),
            Email = email,
            PhoneNumber = NormalizeOptional(request.PhoneNumber),
            CompanyName = NormalizeOptional(request.CompanyName),
            Address = NormalizeOptional(request.Address),
            Status = request.Status
        };

        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(client);
    }

    public async Task<ClientResponse> UpdateAsync(
        Guid id,
        UpdateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateUpdateRequest(request);

        var client = await _dbContext.Clients
            .FirstOrDefaultAsync(currentClient => currentClient.Id == id, cancellationToken);

        if (client is null)
        {
            throw new NotFoundException("Không tìm thấy khách hàng.");
        }

        var email = NormalizeEmail(request.Email);
        var emailExists = await _dbContext.Clients
            .AnyAsync(currentClient => currentClient.Id != id && currentClient.Email == email, cancellationToken);

        if (emailExists)
        {
            throw new BadRequestException("Email khách hàng đã được sử dụng.");
        }

        client.Name = request.FullName.Trim();
        client.Email = email;
        client.PhoneNumber = NormalizeOptional(request.PhoneNumber);
        client.CompanyName = NormalizeOptional(request.CompanyName);
        client.Address = NormalizeOptional(request.Address);
        client.Status = request.Status;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(client);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var client = await _dbContext.Clients
            .FirstOrDefaultAsync(currentClient => currentClient.Id == id, cancellationToken);

        if (client is null)
        {
            throw new NotFoundException("Không tìm thấy khách hàng.");
        }

        client.IsDeleted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ClientResponse MapToResponse(Client client)
    {
        return new ClientResponse(
            client.Id,
            client.Name,
            client.Email,
            client.PhoneNumber,
            client.CompanyName,
            client.Address,
            client.Status,
            client.CreatedAt,
            client.UpdatedAt);
    }

    private static void ValidateCreateRequest(CreateClientRequest request)
    {
        ValidateRequiredFields(request.FullName, request.Email);
    }

    private static void ValidateUpdateRequest(UpdateClientRequest request)
    {
        ValidateRequiredFields(request.FullName, request.Email);
    }

    private static void ValidateRequiredFields(string fullName, string email)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            errors.Add("Họ tên khách hàng là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add("Email khách hàng là bắt buộc.");
        }

        if (errors.Count > 0)
        {
            throw new BadRequestException("Dữ liệu khách hàng không hợp lệ.", errors);
        }
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
