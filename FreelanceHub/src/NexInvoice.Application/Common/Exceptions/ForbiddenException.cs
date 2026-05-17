namespace NexInvoice.Application.Common.Exceptions;

public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Bạn không có quyền thực hiện hành động này.")
        : base(message)
    {
    }
}
