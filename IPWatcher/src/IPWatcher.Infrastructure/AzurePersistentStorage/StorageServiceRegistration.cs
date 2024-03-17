using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using IPWatcher.Core.Interfaces;

namespace IPWatcher.Infrastructure.AzurePersistentStorage;

public static class StorageServiceRegistration
{
    public static IServiceCollection AddIPStorage(this IServiceCollection services)
    {
        services.AddOptions<AzureStorageConfiguration>()
            .BindConfiguration(nameof(AzureStorageConfiguration))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddScoped<IStorageRepository, AzureBlobContainerStorage>();
        return services;
    }
}