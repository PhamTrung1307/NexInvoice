using Bogus;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;

namespace NexInvoice.Infrastructure.Data.FakeData;

internal sealed class NotificationFakeDataGenerator
{
    public IReadOnlyCollection<Notification> Generate(int count, IReadOnlyList<AppUser> users)
    {
        var faker = new Faker("vi");

        return Enumerable.Range(1, count)
            .Select(_ =>
            {
                var type = faker.PickRandom<NotificationType>();
                var isRead = faker.Random.Bool(0.55f);

                return new Notification
                {
                    UserId = users[faker.Random.Int(0, users.Count - 1)].Id,
                    Type = type,
                    Title = GetTitle(type),
                    Message = GetMessage(type, faker),
                    IsRead = isRead,
                    ReadAt = isRead ? DateTimeOffset.UtcNow.AddDays(-faker.Random.Int(1, 120)) : null
                };
            })
            .ToArray();
    }

    private static string GetTitle(NotificationType type)
    {
        return type switch
        {
            NotificationType.Project => "Cap nhat du an",
            NotificationType.Task => "Cap nhat cong viec",
            NotificationType.Invoice => "Cap nhat hoa don",
            NotificationType.Payment => "Cap nhat thanh toan",
            NotificationType.Contract => "Cap nhat hop dong",
            _ => "Thong bao he thong"
        };
    }

    private static string GetMessage(NotificationType type, Faker faker)
    {
        return type switch
        {
            NotificationType.Project => $"Du an {faker.PickRandom(VietnameseFakeData.ProjectNames)} vua duoc cap nhat.",
            NotificationType.Task => $"Cong viec {faker.PickRandom(VietnameseFakeData.TaskTitles)} co thay doi moi.",
            NotificationType.Invoice => "Hoa don cua ban da duoc cap nhat trang thai.",
            NotificationType.Payment => "Thanh toan vua duoc xu ly tren he thong.",
            NotificationType.Contract => "Hop dong moi dang cho xem xet.",
            _ => "Ban co mot thong bao moi tu NexInvoice."
        };
    }
}
