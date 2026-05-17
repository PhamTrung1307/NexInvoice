using System.Net;
using NexInvoice.Application.Common.Exceptions;
using NexInvoice.Application.Common.Models;

namespace NexInvoice.API.Middlewares;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, exception.Message, Array.Empty<string>()),
            BadRequestException badRequest => (
                HttpStatusCode.BadRequest,
                exception.Message,
                badRequest.Errors.Count > 0 ? badRequest.Errors : new[] { exception.Message }),
            UnauthorizedException => (HttpStatusCode.Unauthorized, exception.Message, Array.Empty<string>()),
            ForbiddenException => (HttpStatusCode.Forbidden, exception.Message, Array.Empty<string>()),
            _ => (HttpStatusCode.InternalServerError, "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.", Array.Empty<string>())
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred.");
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception occurred.");
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = ApiResponse<object>.Fail(message, errors);
        await context.Response.WriteAsJsonAsync(response);
    }
}
