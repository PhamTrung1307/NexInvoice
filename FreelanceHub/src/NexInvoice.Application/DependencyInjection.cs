using Microsoft.Extensions.DependencyInjection;

namespace NexInvoice.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IApplicationAssemblyMarker, ApplicationAssemblyMarker>();

        return services;
    }
}
