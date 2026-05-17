using Bogus;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;

namespace NexInvoice.Infrastructure.Data.FakeData;

internal sealed class PaymentFakeDataGenerator
{
    public IReadOnlyCollection<Payment> Generate(int count, IReadOnlyList<Invoice> invoices)
    {
        var faker = new Faker("vi");
        var payableInvoices = invoices
            .Where(invoice => invoice.Status != InvoiceStatus.Cancelled)
            .ToArray();

        return Enumerable.Range(1, count)
            .Select(index =>
            {
                var invoice = faker.PickRandom(payableInvoices);
                var paymentScenario = index % 3;
                var amount = paymentScenario switch
                {
                    0 => invoice.TotalAmount,
                    1 => Math.Round(invoice.TotalAmount * faker.Random.Decimal(0.25m, 0.75m), 2),
                    _ => Math.Round(invoice.TotalAmount * faker.Random.Decimal(0.1m, 0.4m), 2)
                };
                var paymentDate = invoice.DueDate ?? invoice.IssueDate.AddDays(15);

                if (paymentScenario == 2)
                {
                    paymentDate = paymentDate.AddDays(faker.Random.Int(1, 60));
                }
                else
                {
                    paymentDate = paymentDate.AddDays(faker.Random.Int(-10, 10));
                }

                var status = faker.Random.WeightedRandom(
                    new[] { PaymentStatus.Pending, PaymentStatus.Confirmed, PaymentStatus.Rejected },
                    new[] { 0.18f, 0.75f, 0.07f });

                return new Payment
                {
                    InvoiceId = invoice.Id,
                    Amount = amount <= 0 ? 100_000m : amount,
                    Method = faker.PickRandom<PaymentMethod>(),
                    Status = status,
                    PaymentDate = paymentDate,
                    TransactionReference = $"TXN{faker.Random.AlphaNumeric(10).ToUpperInvariant()}",
                    ConfirmedAt = status == PaymentStatus.Confirmed ? DateTimeOffset.UtcNow.AddDays(-faker.Random.Int(1, 300)) : null,
                    RejectedAt = status == PaymentStatus.Rejected ? DateTimeOffset.UtcNow.AddDays(-faker.Random.Int(1, 300)) : null
                };
            })
            .ToArray();
    }
}
