using McpHealthServer.Options;
using Microsoft.Extensions.Options;
using System.Net;

public class UrlPolicyService
{
    private readonly HashSet<string> _allowedDomains;

    public UrlPolicyService(IOptions<UrlPolicyOptions> options)
    {
        _allowedDomains = options.Value.AllowedDomains
            .Select(NormalizeHost)
            .ToHashSet();
    }

    public bool IsAllowed(string rawUrl, out string reason)
    {
        reason = null;

        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            reason = "URL is empty";
            return false;
        }

        //Add default scheme if missing
        if (!rawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !rawUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            rawUrl = "https://" + rawUrl;
        }

        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
        {
            reason = "Invalid URL format";
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            reason = "Only HTTP/HTTPS allowed";
            return false;
        }

        var host = NormalizeHost(uri.Host);

        //Allowlist domain check
        if (!_allowedDomains.Any(d =>
            host == d || host.EndsWith("." + d)))
        {
            reason = "Domain not in allowlist";
            return false;
        }

        //Block private / loopback IPs
        if (IsPrivateOrLoopback(uri.Host))
        {
            reason = "Private or loopback address blocked";
            return false;
        }

        return true;
    }

    private static string NormalizeHost(string host)
    {
        host = host.ToLowerInvariant();
        return host.StartsWith("www.") ? host.Substring(4) : host;
    }

    private static bool IsPrivateOrLoopback(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!IPAddress.TryParse(host, out var ip))
            return false; // DNS name allowed

        if (IPAddress.IsLoopback(ip))
            return true;

        var bytes = ip.GetAddressBytes();
        return bytes[0] switch
        {
            10 => true,
            172 when bytes[1] >= 16 && bytes[1] <= 31 => true,
            192 when bytes[1] == 168 => true,
            _ => false
        };
    }
}
