using Bogus;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;

namespace NexInvoice.Infrastructure.Data.FakeData;

internal sealed class InvoiceFakeDataGenerator
{
    public IReadOnlyCollection<Invoice> Generate(int count, IReadOnlyList<Project> projects)
    {
        var faker = new Faker("vi");
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2));
        var to = DateOnly.FromDateTime(DateTime.UtcNow);

        return Enumerable.Range(1, count)
            .Select(index =>
            {
                var project = projects[faker.Random.Int(0, projects.Count - 1)];
                var issueDate = faker.Date.BetweenDateOnly(from, to);
                var dueDate = issueDate.AddDays(faker.Random.Int(7, 45));
                var items = GenerateItems(faker);
                var subtotal = items.Sum(item => item.Amount);
                var taxAmount = Math.Round(subtotal * faker.PickRandom(0m, 0.05m, 0.08m, 0.1m), 2);
                var discountAmount = faker.Random.Bool(0.35f)
                    ? Math.Round(subtotal * faker.Random.Decimal(0.02m, 0.12m), 2)
                    : 0m;

                return new Invoice
                {
                    InvoiceNumber = $"INV-{issueDate:yyyyMM}-{index:0000}",
                    IssueDate = issueDate,
                    DueDate = dueDate,
                    ClientId = project.ClientId,
                    ProjectId = project.Id,
                    Status = faker.Random.WeightedRandom(
                        new[] { InvoiceStatus.Draft, InvoiceStatus.Sent, InvoiceStatus.Overdue, InvoiceStatus.Cancelled },
                        new[] { 0.12f, 0.5f, 0.3f, 0.08f }),
                    Subtotal = subtotal,
                    TaxAmount = taxAmount,
                    DiscountAmount = discountAmount,
                    TotalAmount = subtotal + taxAmount - discountAmount,
                    Items = items
                };
            })
            .ToArray();
    }

    private static List<InvoiceItem> GenerateItems(Faker faker)
    {
        return Enumerable.Range(1, faker.Random.Int(1, 5))
            .Select(_ =>
            {
                var quantity = faker.Random.Decimal(1m, 12m);
                var unitPrice = faker.Random.Decimal(800_000m, 15_000_000m);

                return new InvoiceItem
                {
                    Description = faker.PickRandom(VietnameseFakeData.InvoiceItems),
                    Quantity = Math.Round(quantity, 2),
                    UnitPrice = Math.Round(unitPrice, 2),
                    Amount = Math.Round(quantity * unitPrice, 2)
                };
            })
            .ToList();
    }
}
