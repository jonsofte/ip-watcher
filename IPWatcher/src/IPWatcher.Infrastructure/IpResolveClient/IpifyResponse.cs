#pragma warning disable IDE1006 // Naming Styles

namespace IPWatcher.Infrastructure.IpifyClient;

public record IpifyResponse
{
    public string ip { get; set; } = "";
}

#pragma warning restore IDE1006 // Naming Styles