using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using NexInvoice.Application.Common.Authorization;
using NexInvoice.Domain.Entities;
using NexInvoice.Domain.Enums;
using NexInvoice.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace NexInvoice.UnitTests;

public sealed class ApiIntegrationTests : IClassFixture<NexInvoiceApiFactory>
{
    private readonly NexInvoiceApiFactory _factory;

    public ApiIntegrationTests(NexInvoiceApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Auth_Register_And_Login_ReturnsTokens()
    {
        using var client = _factory.CreateClient();
        var email = $"new-{Guid.NewGuid():N}@example.com";

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            fullName = "New Client",
            email,
            password = "Client@123"
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "Client@123"
        });

        login.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await login.Content.ReadAsStreamAsync());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("data").GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("data").GetProperty("refreshToken").GetString()));
    }

    [Fact]
    public async Task Clients_Crud_Works()
    {
        using var client = await _factory.CreateAdminClientAsync();

        var created = await PostDataAsync(client, "/api/v1/clients", new
        {
            fullName = "Client CRUD",
            email = $"client-{Guid.NewGuid():N}@example.com",
            phoneNumber = "0900000000",
            companyName = "Nex Test",
            address = "Ho Chi Minh City",
            status = 1
        });

        var id = created.GetProperty("id").GetGuid();
        var detail = await GetDataAsync(client, $"/api/v1/clients/{id}");
        Assert.Equal("Client CRUD", detail.GetProperty("fullName").GetString());

        var updated = await PutDataAsync(client, $"/api/v1/clients/{id}", new
        {
            fullName = "Client CRUD Updated",
            email = detail.GetProperty("email").GetString(),
            phoneNumber = "0911111111",
            companyName = "Nex Test",
            address = "Da Nang",
            status = 1
        });
        Assert.Equal("Client CRUD Updated", updated.GetProperty("fullName").GetString());

        var delete = await client.DeleteAsync($"/api/v1/clients/{id}");
        delete.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Projects_Crud_Works()
    {
        using var client = await _factory.CreateAdminClientAsync();
        var clientId = await CreateClientAsync(client);

        var created = await PostDataAsync(client, "/api/v1/projects", new
        {
            name = "Project CRUD",
            description = "Integration project",
            startDate = "2026-05-01",
            endDate = "2026-05-30",
            budget = 5000000,
            clientId,
            status = 2
        });

        var id = created.GetProperty("id").GetGuid();
        var updated = await PutDataAsync(client, $"/api/v1/projects/{id}", new
        {
            name = "Project CRUD Updated",
            description = "Updated",
            startDate = "2026-05-01",
            endDate = "2026-06-15",
            budget = 6000000,
            clientId,
            status = 2
        });

        Assert.Equal("Project CRUD Updated", updated.GetProperty("name").GetString());
        var delete = await client.DeleteAsync($"/api/v1/projects/{id}");
        delete.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Invoices_Create_MarkPaid_And_Cancel_Work()
    {
        using var client = await _factory.CreateAdminClientAsync();
        var projectId = await CreateClientProjectAsync(client);

        var paidInvoice = await CreateInvoiceAsync(client, projectId, "INV-PAID");
        var markPaid = await client.PatchAsync($"/api/v1/invoices/{paidInvoice}/mark-paid", null);
        markPaid.EnsureSuccessStatusCode();

        var cancelledInvoice = await CreateInvoiceAsync(client, projectId, "INV-CANCEL");
        var cancel = await client.PatchAsync($"/api/v1/invoices/{cancelledInvoice}/cancel", null);
        cancel.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Payments_Create_Confirm_And_Reject_Work()
    {
        using var client = await _factory.CreateAdminClientAsync();
        var projectId = await CreateClientProjectAsync(client);
        var invoiceId = await CreateInvoiceAsync(client, projectId, "INV-PAY");

        var payment = await PostDataAsync(client, "/api/v1/payments", new
        {
            invoiceId,
            amount = 500000,
            method = 1,
            paymentDate = "2026-05-20",
            transactionReference = "TX-001"
        });
        var confirm = await client.PatchAsync($"/api/v1/payments/{payment.GetProperty("id").GetGuid()}/confirm", null);
        confirm.EnsureSuccessStatusCode();

        var rejectedPayment = await PostDataAsync(client, "/api/v1/payments", new
        {
            invoiceId,
            amount = 100000,
            method = 1,
            paymentDate = "2026-05-21",
            transactionReference = "TX-002"
        });
        var reject = await client.PatchAsJsonAsync($"/api/v1/payments/{rejectedPayment.GetProperty("id").GetGuid()}/reject", new
        {
            reason = "Invalid proof"
        });
        reject.EnsureSuccessStatusCode();
    }

    private static async Task<Guid> CreateClientAsync(HttpClient client)
    {
        var created = await PostDataAsync(client, "/api/v1/clients", new
        {
            fullName = $"Client {Guid.NewGuid():N}",
            email = $"client-{Guid.NewGuid():N}@example.com",
            status = 1
        });

        return created.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateClientProjectAsync(HttpClient client)
    {
        var clientId = await CreateClientAsync(client);
        var project = await PostDataAsync(client, "/api/v1/projects", new
        {
            name = $"Project {Guid.NewGuid():N}",
            description = "Integration project",
            startDate = "2026-05-01",
            endDate = "2026-05-30",
            budget = 1000000,
            clientId,
            status = 2
        });

        return project.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateInvoiceAsync(HttpClient client, Guid projectId, string prefix)
    {
        var invoice = await PostDataAsync(client, "/api/v1/invoices", new
        {
            invoiceNumber = $"{prefix}-{Guid.NewGuid():N}",
            issueDate = "2026-05-01",
            dueDate = "2026-05-20",
            projectId,
            taxAmount = 0,
            discountAmount = 0,
            status = 1,
            items = new[]
            {
                new { description = "Development", quantity = 1, unitPrice = 1000000 }
            }
        });

        return invoice.GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> GetDataAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await ReadDataAsync(response);
    }

    private static async Task<JsonElement> PostDataAsync(HttpClient client, string url, object payload)
    {
        var response = await client.PostAsJsonAsync(url, payload);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }
        return await ReadDataAsync(response);
    }

    private static async Task<JsonElement> PutDataAsync(HttpClient client, string url, object payload)
    {
        var response = await client.PutAsJsonAsync(url, payload);
        response.EnsureSuccessStatusCode();
        return await ReadDataAsync(response);
    }

    private static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response)
    {
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.GetProperty("data").Clone();
    }
}

public sealed class NexInvoiceApiFactory : WebApplicationFactory<Program>
{
    public static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly InMemoryDatabaseRoot _databaseRoot = new();
    private readonly string _databaseName = $"NexInvoiceApiTests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "NexInvoice",
                ["JwtSettings:Audience"] = "NexInvoice.Api",
                ["JwtSettings:SecretKey"] = "NexInvoice_Tests_Jwt_Secret_Key_At_Least_32_Characters",
                ["JwtSettings:Secret"] = "NexInvoice_Tests_Jwt_Secret_Key_At_Least_32_Characters",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "60",
                ["JwtSettings:RefreshTokenExpirationDays"] = "7",
                ["Redis:Enabled"] = "false"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName, _databaseRoot));

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            SeedIdentity(dbContext);

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }

    public Task<HttpClient> CreateAdminClientAsync()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, "admin");
        return Task.FromResult(client);
    }

    private static void SeedIdentity(AppDbContext dbContext)
    {
        var adminRole = new Role { Name = AppRoles.Admin, Description = "Admin" };
        var clientRole = new Role { Name = AppRoles.Client, Description = "Client" };
        dbContext.Roles.AddRange(adminRole, clientRole);

        var permissions = AppPermissions.All
            .Select(permission => new Permission
            {
                Code = permission,
                Name = permission,
                Description = permission
            })
            .ToArray();
        dbContext.Permissions.AddRange(permissions);
        dbContext.SaveChanges();

        dbContext.RolePermissions.AddRange(permissions.Select(permission => new RolePermission
        {
            RoleId = adminRole.Id,
            PermissionId = permission.Id
        }));

        var admin = new AppUser
        {
            Id = AdminUserId,
            FullName = "Test Admin",
            Email = "admin@nexinvoice.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            IsActive = true
        };
        dbContext.AppUsers.Add(admin);
        dbContext.SaveChanges();

        dbContext.UserRoles.Add(new UserRole
        {
            UserId = admin.Id,
            RoleId = adminRole.Id
        });

        dbContext.SaveChanges();
    }
}

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string HeaderName = "X-Test-Auth";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var value) || value != "admin")
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, NexInvoiceApiFactory.AdminUserId.ToString()),
            new Claim("UserId", NexInvoiceApiFactory.AdminUserId.ToString()),
            new Claim(ClaimTypes.Name, "Test Admin"),
            new Claim(ClaimTypes.Role, AppRoles.Admin)
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
