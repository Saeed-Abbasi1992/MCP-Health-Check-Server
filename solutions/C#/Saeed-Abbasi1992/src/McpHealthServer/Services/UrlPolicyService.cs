using System.Net;

namespace McpHealthServer.Services;

public class UrlPolicyService
{
    public bool IsAllowed(string url, out string reason)
    {
        reason = null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            reason = "Invalid URL";
            return false;
        }

        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            reason = "Only HTTP/HTTPS allowed";
            return false;
        }

        if (IsPrivateHost(uri.Host))
        {
            reason = "Private or loopback address blocked";
            return false;
        }

        return true;
    }

    private bool IsPrivateHost(string host)
    {
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!IPAddress.TryParse(host, out var ip))
            return false; // DNS name – allow (optional: resolve & check)

        if (IPAddress.IsLoopback(ip)) //(127.0.0.1 OR ::1)
            return true;

        var bytes = ip.GetAddressBytes();

        return bytes[0] switch
        {
            10 => true,//10.x.x.x Private network class A
            172 when bytes[1] >= 16 && bytes[1] <= 31 => true,//172.16.x.x – 172.31.x.x Private network class B
            192 when bytes[1] == 168 => true,//x.x.192.168 Private network class C
            _ => false
        };
    }
}
