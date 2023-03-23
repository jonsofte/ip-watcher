using IPWatcher.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IPWatcher.SyncHandler;

public static class SyncHandlerServiceRegistration
{
    public static IServiceCollection AddSyncService(this IServiceCollection services)
    {
        services.TryAddScoped<IIpSyncService,IpSyncHandler>();
        return services;
    }
}