using NexInvoice.API.Authorization;
using NexInvoice.Application.Common.Authorization;
using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Customers;
using NexInvoice.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NexInvoice.API.Controllers;

[ApiController]
[Route("api/v1/clients")]
[Route("api/v1/customers")]
[Authorize]
public sealed class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    [HasPermission(AppPermissions.ClientView)]
    public async Task<ActionResult<ApiResponse<PagedResult<ClientListItemResponse>>>> GetClients(
        [FromQuery] ClientQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var result = await _clientService.GetPagedAsync(queryParameters, cancellationToken);

        return Ok(ApiResponse<PagedResult<ClientListItemResponse>>.Ok(
            result,
            "Lấy danh sách khách hàng thành công."));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AppPermissions.ClientView)]
    public async Task<ActionResult<ApiResponse<ClientResponse>>> GetClientById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _clientService.GetByIdAsync(id, cancellationToken);

        return Ok(ApiResponse<ClientResponse>.Ok(
            result,
            "Lấy thông tin khách hàng thành công."));
    }

    [HttpPost]
    [HasPermission(AppPermissions.ClientCreate)]
    public async Task<ActionResult<ApiResponse<ClientResponse>>> CreateClient(
        CreateClientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _clientService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetClientById),
            new { id = result.Id },
            ApiResponse<ClientResponse>.Ok(result, "Tạo khách hàng thành công."));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(AppPermissions.ClientUpdate)]
    public async Task<ActionResult<ApiResponse<ClientResponse>>> UpdateClient(
        Guid id,
        UpdateClientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _clientService.UpdateAsync(id, request, cancellationToken);

        return Ok(ApiResponse<ClientResponse>.Ok(
            result,
            "Cập nhật khách hàng thành công."));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(AppPermissions.ClientDelete)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteClient(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _clientService.DeleteAsync(id, cancellationToken);

        return Ok(ApiResponse<object>.Ok(null, "Xóa khách hàng thành công."));
    }
}
