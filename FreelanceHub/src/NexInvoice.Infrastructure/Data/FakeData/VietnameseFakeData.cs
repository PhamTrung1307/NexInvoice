namespace NexInvoice.Infrastructure.Data.FakeData;

internal static class VietnameseFakeData
{
    public static readonly string[] FirstNames =
    {
        "An", "Binh", "Chau", "Dung", "Giang", "Ha", "Hanh", "Hoa", "Khanh", "Lan",
        "Linh", "Long", "Minh", "Nam", "Ngoc", "Phuc", "Quan", "Quynh", "Son", "Thao",
        "Trang", "Trung", "Tuan", "Vy", "Yen"
    };

    public static readonly string[] LastNames =
    {
        "Nguyen", "Tran", "Le", "Pham", "Hoang", "Huynh", "Phan", "Vu", "Vo", "Dang",
        "Bui", "Do", "Ho", "Ngo", "Duong"
    };

    public static readonly string[] MiddleNames =
    {
        "Van", "Thi", "Minh", "Thanh", "Quoc", "Duc", "Anh", "Gia", "Bao", "Ngoc"
    };

    public static readonly string[] Companies =
    {
        "Cong ty TNHH Minh Long", "Cong ty Co phan Sao Viet", "Cong ty TNHH An Phat",
        "Cong ty Co phan Hoa Binh Digital", "Cong ty TNHH Nam Phuong", "Cong ty TNHH VietLink",
        "Cong ty Co phan Mekong Tech", "Cong ty TNHH Blue Ocean", "Cong ty TNHH Lotus Media",
        "Cong ty Co phan GreenHub"
    };

    public static readonly string[] Streets =
    {
        "Nguyen Hue", "Le Loi", "Tran Hung Dao", "Dien Bien Phu", "Nguyen Trai",
        "Pham Van Dong", "Vo Van Kiet", "Hai Ba Trung", "Ly Thuong Kiet", "Cach Mang Thang Tam"
    };

    public static readonly string[] Cities =
    {
        "Ho Chi Minh", "Ha Noi", "Da Nang", "Can Tho", "Hai Phong", "Nha Trang", "Hue", "Binh Duong"
    };

    public static readonly string[] ProjectNames =
    {
        "Website redesign", "Mobile app development", "ERP system", "CRM dashboard", "E-commerce website",
        "Booking platform", "Landing page optimization", "Inventory management system",
        "Customer portal", "Payment integration"
    };

    public static readonly string[] TaskTitles =
    {
        "Design login page", "Implement JWT authentication", "Optimize SQL query", "Deploy Docker container",
        "Build dashboard widgets", "Create invoice PDF template", "Configure Redis caching",
        "Implement payment confirmation", "Write API documentation", "Fix responsive layout"
    };

    public static readonly string[] InvoiceItems =
    {
        "UI/UX design", "Backend API development", "Frontend implementation", "Database design",
        "System integration", "Performance optimization", "Deployment support", "Testing and bug fixing"
    };

    public static string FullName(Bogus.Faker faker)
    {
        return $"{faker.PickRandom(LastNames)} {faker.PickRandom(MiddleNames)} {faker.PickRandom(FirstNames)}";
    }

    public static string PhoneNumber(Bogus.Faker faker)
    {
        return $"0{faker.PickRandom(new[] { "90", "91", "93", "94", "96", "97", "98" })}{faker.Random.Number(1000000, 9999999)}";
    }

    public static string Address(Bogus.Faker faker)
    {
        return $"{faker.Random.Number(1, 250)} {faker.PickRandom(Streets)}, {faker.PickRandom(Cities)}, Vietnam";
    }
}
