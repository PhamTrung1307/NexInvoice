using Hangfire;
using NexInvoice.API.Extensions;
using NexInvoice.API.Hubs;
using NexInvoice.Application;
using NexInvoice.Infrastructure;
using NexInvoice.Infrastructure.Data;
using NexInvoice.Infrastructure.BackgroundJobs;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(context.Configuration));

builder.Services
    .AddApiServices()
    .AddReactCors(builder.Configuration)
    .AddHangfireServices(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

var swaggerEnabled = app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("Swagger:Enabled");

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UseApiMiddleware();
app.UseSerilogRequestLogging();

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseCors("ReactFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

RecurringJob.AddOrUpdate<InvoiceReminderJob>(
    "invoice-reminder-job",
    job => job.RunAsync(),
    Cron.Daily);

app.Run();
