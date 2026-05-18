using NexInvoice.Application.Interfaces;
using NexInvoice.Application.Common.Settings;
using NexInvoice.Infrastructure.Data;
using NexInvoice.Infrastructure.BackgroundJobs;
using NexInvoice.Infrastructure.Identity;
using NexInvoice.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NexInvoice.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        var redisEnabled = configuration.GetValue<bool>("Redis:Enabled");
        var redisConnectionString = configuration.GetConnectionString("Redis");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        if (redisEnabled && !string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = configuration.GetValue<string>("Redis:InstanceName") ?? "NexInvoice:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<FreelancerSettings>(configuration.GetSection("Freelancer"));
        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IContractService, ContractService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IPdfService, PdfService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<InvoiceReminderJob>();
        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<AppDbContextSeeder>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }
}
