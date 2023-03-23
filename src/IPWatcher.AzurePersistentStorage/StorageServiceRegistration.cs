using IPStorage;
using IPWatcher.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IPWatcher.AzurePersistantStorage;

public static class StorageServiceRegistration
{
    public static IServiceCollection AddIPStorage(this IServiceCollection services)
    {
        services.TryAddScoped<IStorageRepository, AzureBlobContainerStorage>();
        return services;
    }
}