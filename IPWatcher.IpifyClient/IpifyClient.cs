using System.Text.Json;
using Microsoft.Extensions.Logging;
using CSharpFunctionalExtensions;
using IPWatcher.Abstractions.Interfaces;
using IPWatcher.Abstractions;

namespace IpifyClient;
public class IpifyClient : ICurrentIPResolver
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<IpifyClient> _logger;

    public IpifyClient(IHttpClientFactory httpClientFactory, ILogger<IpifyClient> logger)
    {
        _httpClientFactory= httpClientFactory;
        _logger= logger;
    }

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
                var associationResponse = JsonSerializer.Deserialize<IpifyResponse>(response);
                if (associationResponse != null)
                {
                    _logger.LogDebug("IpIfy client returned IP address {ip}", associationResponse.ip);
                    var ip = new IPAddress() { Ip = associationResponse.ip };
                    return Result.Success(ip);
                }
            }
            _logger.LogError("Not able to get IP address");
            return Result.Failure<IPAddress>("Not able to get IP address");
        } catch (Exception ex)
        {
            _logger.LogError("Error: {error}", ex.Message);
            return Result.Failure<IPAddress>(ex.Message);
        }
    }
}