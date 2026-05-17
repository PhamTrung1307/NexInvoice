namespace NexInvoice.Application.Common.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string message = "Không tìm thấy dữ liệu.")
        : base(message)
    {
    }
}
