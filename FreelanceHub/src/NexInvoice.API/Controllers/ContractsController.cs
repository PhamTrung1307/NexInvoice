using NexInvoice.API.Authorization;
using NexInvoice.Application.Common.Authorization;
using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Contracts;
using NexInvoice.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NexInvoice.API.Controllers;

[ApiController]
[Route("api/v1/contracts")]
[Authorize]
public sealed class ContractsController : ControllerBase
{
    private readonly IContractService _contractService;

    public ContractsController(IContractService contractService)
    {
        _contractService = contractService;
    }

    [HttpGet]
    [HasPermission(AppPermissions.ContractView)]
    public async Task<ActionResult<ApiResponse<PagedResult<ContractResponse>>>> GetContracts(
        [FromQuery] ContractQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _contractService.GetPagedAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<ContractResponse>>.Ok(result, "Lấy danh sách hợp đồng thành công."));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AppPermissions.ContractView)]
    public async Task<ActionResult<ApiResponse<ContractResponse>>> GetContract(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _contractService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ContractResponse>.Ok(result, "Lấy thông tin hợp đồng thành công."));
    }

    [HttpPost]
    [HasPermission(AppPermissions.ContractCreate)]
    public async Task<ActionResult<ApiResponse<ContractResponse>>> CreateContract(
        CreateContractRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _contractService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetContract),
            new { id = result.Id },
            ApiResponse<ContractResponse>.Ok(result, "Tạo hợp đồng thành công"));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(AppPermissions.ContractUpdate)]
    public async Task<ActionResult<ApiResponse<ContractResponse>>> UpdateContract(
        Guid id,
        UpdateContractRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _contractService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ContractResponse>.Ok(result, "Cập nhật hợp đồng thành công"));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(AppPermissions.ContractDelete)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteContract(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _contractService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Xóa hợp đồng thành công."));
    }

    [HttpPost("{id:guid}/upload")]
    [HasPermission(AppPermissions.ContractUpdate)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ContractResponse>>> UploadContract(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await _contractService.UploadAsync(
            id,
            new UploadContractFileRequest(stream, file.FileName, file.ContentType, file.Length),
            cancellationToken);

        return Ok(ApiResponse<ContractResponse>.Ok(result, "Tải hợp đồng lên thành công"));
    }

    [HttpGet("{id:guid}/download")]
    [HasPermission(AppPermissions.ContractView)]
    public async Task<IActionResult> DownloadContract(Guid id, CancellationToken cancellationToken)
    {
        var result = await _contractService.DownloadAsync(id, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpPatch("{id:guid}/approve")]
    [HasPermission(AppPermissions.ContractUpdate)]
    public async Task<ActionResult<ApiResponse<ContractResponse>>> ApproveContract(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _contractService.ApproveAsync(id, cancellationToken);
        return Ok(ApiResponse<ContractResponse>.Ok(result, "Phê duyệt hợp đồng thành công"));
    }

    [HttpPatch("{id:guid}/reject")]
    [HasPermission(AppPermissions.ContractUpdate)]
    public async Task<ActionResult<ApiResponse<ContractResponse>>> RejectContract(
        Guid id,
        RejectContractRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _contractService.RejectAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ContractResponse>.Ok(result, "Từ chối hợp đồng thành công"));
    }
}
