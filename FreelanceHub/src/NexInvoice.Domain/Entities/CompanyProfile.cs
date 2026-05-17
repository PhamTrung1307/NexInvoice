using NexInvoice.Domain.Common;

namespace NexInvoice.Domain.Entities;

public class CompanyProfile : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;

    public string? TaxCode { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? Website { get; set; }

    public string? LogoUrl { get; set; }
}
