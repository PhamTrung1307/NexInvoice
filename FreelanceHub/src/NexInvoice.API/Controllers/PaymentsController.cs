using NexInvoice.API.Authorization;
using NexInvoice.Application.Common.Authorization;
using NexInvoice.Application.Common.Exceptions;
using NexInvoice.Application.Common.Models;
using NexInvoice.Application.Features.Payments;
using NexInvoice.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace NexInvoice.API.Controllers;

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    [HasPermission(AppPermissions.PaymentCreate)]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> CreatePayment(
        CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.CreateAsync(request, cancellationToken);

        return Ok(ApiResponse<PaymentResponse>.Ok(result, "Tạo thanh toán thành công."));
    }

    [HttpGet("/api/v1/invoices/{invoiceId:guid}/payments")]
    [HasPermission(AppPermissions.PaymentView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PaymentResponse>>>> GetPaymentsByInvoice(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetByInvoiceAsync(invoiceId, cancellationToken);

        return Ok(ApiResponse<IReadOnlyCollection<PaymentResponse>>.Ok(
            result,
            "Lấy lịch sử thanh toán thành công."));
    }

    [HttpPost("{id:guid}/proof")]
    [HasPermission(AppPermissions.PaymentUpdate)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> UploadPaymentProof(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new BadRequestException("Tệp minh chứng thanh toán là bắt buộc.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _paymentService.UploadProofAsync(
            id,
            new UploadPaymentProofRequest(
                stream,
                file.FileName,
                file.ContentType,
                file.Length),
            cancellationToken);

        return Ok(ApiResponse<PaymentResponse>.Ok(result, "Tải minh chứng thanh toán thành công."));
    }

    [HttpPatch("{id:guid}/confirm")]
    [Authorize(Roles = AppRoles.Admin)]
    [HasPermission(AppPermissions.PaymentUpdate)]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> ConfirmPayment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.ConfirmAsync(id, GetCurrentUserId(), cancellationToken);

        return Ok(ApiResponse<PaymentResponse>.Ok(result, "Xác nhận thanh toán thành công."));
    }

    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = AppRoles.Admin)]
    [HasPermission(AppPermissions.PaymentUpdate)]
    public async Task<ActionResult<ApiResponse<PaymentResponse>>> RejectPayment(
        Guid id,
        RejectPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.RejectAsync(id, request, GetCurrentUserId(), cancellationToken);

        return Ok(ApiResponse<PaymentResponse>.Ok(result, "Từ chối thanh toán thành công."));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue("UserId")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            throw new UnauthorizedException("Token không hợp lệ.");
        }

        return parsedUserId;
    }
}
