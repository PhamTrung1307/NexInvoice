using Bogus;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;
using DomainTaskStatus = NexInvoice.Domain.Enums.TaskStatus;

namespace NexInvoice.Infrastructure.Data.FakeData;

internal sealed class TaskFakeDataGenerator
{
    public IReadOnlyCollection<TaskItem> Generate(
        int count,
        IReadOnlyList<Project> projects,
        IReadOnlyList<Guid> userIds)
    {
        var faker = new Faker("vi");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return Enumerable.Range(1, count)
            .Select(_ =>
            {
                var project = projects[faker.Random.Int(0, projects.Count - 1)];

                return new TaskItem
                {
                    Title = faker.PickRandom(VietnameseFakeData.TaskTitles),
                    Description = faker.Lorem.Paragraph(),
                    Status = faker.Random.WeightedRandom(
                        new[] { DomainTaskStatus.Todo, DomainTaskStatus.InProgress, DomainTaskStatus.InReview, DomainTaskStatus.Done, DomainTaskStatus.Cancelled },
                        new[] { 0.25f, 0.3f, 0.15f, 0.25f, 0.05f }),
                    Priority = faker.Random.WeightedRandom(
                        new[] { TaskPriority.Low, TaskPriority.Medium, TaskPriority.High, TaskPriority.Urgent },
                        new[] { 0.2f, 0.5f, 0.25f, 0.05f }),
                    DueDate = today.AddDays(faker.Random.Int(-180, 120)),
                    ProjectId = project.Id,
                    AssignedToId = userIds[faker.Random.Int(0, userIds.Count - 1)]
                };
            })
            .ToArray();
    }
}
