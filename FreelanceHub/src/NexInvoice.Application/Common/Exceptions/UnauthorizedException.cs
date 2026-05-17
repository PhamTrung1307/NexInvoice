namespace NexInvoice.Application.Common.Exceptions;

public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Bạn cần đăng nhập để thực hiện hành động này.")
        : base(message)
    {
    }
}
