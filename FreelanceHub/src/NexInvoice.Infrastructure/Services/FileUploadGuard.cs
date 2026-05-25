using NexInvoice.Application.Common.Exceptions;
using Microsoft.AspNetCore.Hosting;

namespace NexInvoice.Infrastructure.Services;

internal static class FileUploadGuard
{
    public static string ValidateAndCreateStoredFileName(
        string fileName,
        string? contentType,
        long sizeInBytes,
        IReadOnlySet<string> allowedExtensions,
        IReadOnlySet<string> allowedContentTypes,
        string requiredMessage,
        string sizeMessage,
        string typeMessage)
    {
        if (sizeInBytes <= 0)
        {
            throw new BadRequestException(requiredMessage);
        }

        if (sizeInBytes > 10 * 1024 * 1024)
        {
            throw new BadRequestException(sizeMessage);
        }

        var safeFileName = Path.GetFileName(fileName);
        var extension = Path.GetExtension(safeFileName).ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(safeFileName)
            || string.IsNullOrWhiteSpace(extension)
            || !allowedExtensions.Contains(extension)
            || (!string.IsNullOrWhiteSpace(contentType) && !allowedContentTypes.Contains(contentType)))
        {
            throw new BadRequestException(typeMessage);
        }

        return $"{Guid.NewGuid():N}{extension}";
    }

    public static string ResolveUploadRoot(
        IWebHostEnvironment webHostEnvironment,
        params string[] segments)
    {
        var webRootPath = webHostEnvironment.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(webHostEnvironment.ContentRootPath, "wwwroot");
        }

        var root = Path.GetFullPath(Path.Combine(new[] { webRootPath }.Concat(segments).ToArray()));
        var allowedRoot = Path.GetFullPath(Path.Combine(webRootPath, "uploads"));

        if (!root.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Đường dẫn lưu tệp không hợp lệ.");
        }

        return root;
    }
}
