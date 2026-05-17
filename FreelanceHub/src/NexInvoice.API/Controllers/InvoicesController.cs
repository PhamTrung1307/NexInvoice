using NexInvoice.API.Authorization;
using NexInvoice.Application.Common.Authorization;
using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Invoices;
using NexInvoice.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NexInvoice.API.Controllers;

[ApiController]
[Route("api/v1/invoices")]
[Authorize]
public sealed class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IPdfService _pdfService;

    public InvoicesController(
        IInvoiceService invoiceService,
        IPdfService pdfService)
    {
        _invoiceService = invoiceService;
        _pdfService = pdfService;
    }

    [HttpGet]
    [HasPermission(AppPermissions.InvoiceView)]
    public async Task<ActionResult<ApiResponse<PagedResult<InvoiceListItemResponse>>>> GetInvoices(
        [FromQuery] InvoiceQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.GetPagedAsync(queryParameters, cancellationToken);

        return Ok(ApiResponse<PagedResult<InvoiceListItemResponse>>.Ok(
            result,
            "Lấy danh sách hóa đơn thành công."));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AppPermissions.InvoiceView)]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> GetInvoiceById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.GetByIdAsync(id, cancellationToken);

        return Ok(ApiResponse<InvoiceResponse>.Ok(
            result,
            "Lấy thông tin hóa đơn thành công."));
    }

    [HttpGet("{id:guid}/pdf")]
    [HasPermission(AppPermissions.InvoiceView)]
    public async Task<IActionResult> DownloadInvoicePdf(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _pdfService.GenerateInvoicePdfAsync(id, cancellationToken);

        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpPost]
    [HasPermission(AppPermissions.InvoiceCreate)]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> CreateInvoice(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetInvoiceById),
            new { id = result.Id },
            ApiResponse<InvoiceResponse>.Ok(result, "Tạo hóa đơn thành công."));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(AppPermissions.InvoiceUpdate)]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> UpdateInvoice(
        Guid id,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.UpdateAsync(id, request, cancellationToken);

        return Ok(ApiResponse<InvoiceResponse>.Ok(
            result,
            "Cập nhật hóa đơn thành công."));
    }

    [HttpPatch("{id:guid}/send")]
    [HasPermission(AppPermissions.InvoiceUpdate)]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> SendInvoice(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.SendAsync(id, cancellationToken);

        return Ok(ApiResponse<InvoiceResponse>.Ok(
            result,
            "Gửi hóa đơn thành công."));
    }

    [HttpPatch("{id:guid}/cancel")]
    [HasPermission(AppPermissions.InvoiceUpdate)]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> CancelInvoice(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.CancelAsync(id, cancellationToken);

        return Ok(ApiResponse<InvoiceResponse>.Ok(
            result,
            "Hủy hóa đơn thành công."));
    }

    [HttpPatch("{id:guid}/mark-paid")]
    [HasPermission(AppPermissions.InvoiceUpdate)]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> MarkInvoicePaid(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.MarkPaidAsync(id, cancellationToken);

        return Ok(ApiResponse<InvoiceResponse>.Ok(
            result,
            "Đánh dấu hóa đơn đã thanh toán thành công."));
    }
}
