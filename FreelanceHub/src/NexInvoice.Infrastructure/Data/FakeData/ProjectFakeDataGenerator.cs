using Bogus;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;

namespace NexInvoice.Infrastructure.Data.FakeData;

internal sealed class ProjectFakeDataGenerator
{
    public IReadOnlyCollection<Project> Generate(
        int count,
        IReadOnlyList<Client> clients,
        IReadOnlyList<Guid> ownerIds)
    {
        var faker = new Faker("vi");
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-2));
        var to = DateOnly.FromDateTime(DateTime.UtcNow);

        return Enumerable.Range(1, count)
            .Select(index =>
            {
                var client = clients[faker.Random.Int(0, clients.Count - 1)];
                var startDate = faker.Date.BetweenDateOnly(from, to.AddMonths(-1));
                DateOnly? endDate = faker.Random.Bool(0.85f)
                    ? startDate.AddDays(faker.Random.Int(14, 180))
                    : null;

                return new Project
                {
                    Name = $"{faker.PickRandom(VietnameseFakeData.ProjectNames)} - {client.CompanyName}",
                    Description = faker.Lorem.Sentence(10),
                    Status = faker.Random.WeightedRandom(
                        new[] { ProjectStatus.Draft, ProjectStatus.Active, ProjectStatus.OnHold, ProjectStatus.Completed, ProjectStatus.Cancelled },
                        new[] { 0.08f, 0.55f, 0.12f, 0.2f, 0.05f }),
                    StartDate = startDate,
                    EndDate = endDate,
                    Budget = faker.Random.Decimal(5_000_000m, 250_000_000m),
                    ClientId = client.Id,
                    OwnerId = ownerIds[faker.Random.Int(0, ownerIds.Count - 1)]
                };
            })
            .ToArray();
    }
}
