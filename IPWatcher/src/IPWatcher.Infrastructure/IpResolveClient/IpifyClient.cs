using System.Text.Json;
using Microsoft.Extensions.Logging;
using CSharpFunctionalExtensions;

using IPWatcher.Core.Interfaces;
using IPWatcher.Core.Domain;

namespace IPWatcher.Infrastructure.IpifyClient;

public class IpifyClient(IHttpClientFactory httpClientFactory, ILogger<IpifyClient> logger) : ICurrentIPAddressResolver
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<IpifyClient> _logger = logger;

    public async Task<Result<IPAddress>> GetIP(CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("IpifyClient");
            _logger.LogDebug("Fetching ip from {url}", client.BaseAddress);
            var result = await client.GetAsync("", cancellationToken);

            if (result.IsSuccessStatusCode)
            {
                string response = await result.Content.ReadAsStringAsync(cancellationToken);
                var contentResponse = JsonSerializer.Deserialize<IpifyResponse>(response);
                if (contentResponse != null)
                {
                    _logger.LogDebug("IpIfy client returned IP address {ip}", contentResponse.ip);
                    var ip = new IPAddress() { Ip = contentResponse.ip };
                    return ip;
                }
            }
            _logger.LogError("Not able to get IP address: {errorcode}", result.StatusCode);
            return Result.Failure<IPAddress>($"Not able to get IP address: {result.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error: {error}", ex.Message);
            return Result.Failure<IPAddress>(ex.Message);
        }
    }
}