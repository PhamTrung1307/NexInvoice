namespace NexInvoice.Application.Common.Settings;

public sealed class FreelancerSettings
{
    public string Name { get; init; } = "NexInvoice";

    public string Email { get; init; } = "contact@nexinvoice.com";

    public string? PhoneNumber { get; init; }

    public string? Address { get; init; }

    public string PaymentNote { get; init; } = "Vui lòng thanh toán theo thông tin đã thỏa thuận.";
}
