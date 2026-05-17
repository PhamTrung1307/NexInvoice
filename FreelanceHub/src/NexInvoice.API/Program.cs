using Hangfire;
using NexInvoice.API.Extensions;
using NexInvoice.API.Hubs;
using NexInvoice.Application;
using NexInvoice.Infrastructure;
using NexInvoice.Infrastructure.Data;
using NexInvoice.Infrastructure.BackgroundJobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiServices()
    .AddReactCors(builder.Configuration)
    .AddHangfireServices(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseApiMiddleware();
app.UseHttpsRedirection();
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
