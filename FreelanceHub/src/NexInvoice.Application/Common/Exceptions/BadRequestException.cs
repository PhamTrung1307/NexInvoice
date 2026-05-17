namespace NexInvoice.Application.Common.Exceptions;

public sealed class BadRequestException : Exception
{
    public BadRequestException(string message = "Yêu cầu không hợp lệ.")
        : base(message)
    {
    }

    public BadRequestException(string message, IEnumerable<string> errors)
        : base(message)
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyCollection<string> Errors { get; } = Array.Empty<string>();
}
