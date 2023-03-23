using IPWatcher.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IPWatcher.IpifyClient;

public static class IpifyClientServiceRegistration
{
    private static readonly string _serviceURL = "https://api.ipify.org?format=json";
    public static IServiceCollection AddIpifyClient(this IServiceCollection services)
    {
        services.AddHttpClient("IpifyClient", HttpClient =>
        {
            HttpClient.BaseAddress = new Uri(_serviceURL);
        });
        services.TryAddScoped<ICurrentIPResolver, IpifyClient>();
        return services;
    }
}