using Bogus;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;

namespace NexInvoice.Infrastructure.Data.FakeData;

internal sealed class ClientFakeDataGenerator
{
    public IReadOnlyCollection<Client> Generate(int count, IReadOnlyList<Guid> ownerIds)
    {
        var faker = new Faker("vi");

        return Enumerable.Range(1, count)
            .Select(index =>
            {
                var name = VietnameseFakeData.FullName(faker);

                return new Client
                {
                    Name = name,
                    CompanyName = faker.PickRandom(VietnameseFakeData.Companies),
                    Email = $"client{index:000}@nexinvoice.test",
                    PhoneNumber = VietnameseFakeData.PhoneNumber(faker),
                    Address = VietnameseFakeData.Address(faker),
                    Status = faker.Random.WeightedRandom(
                        new[] { ClientStatus.Active, ClientStatus.Inactive, ClientStatus.Archived },
                        new[] { 0.82f, 0.14f, 0.04f }),
                    OwnerId = ownerIds[faker.Random.Int(0, ownerIds.Count - 1)]
                };
            })
            .ToArray();
    }
}
