using McpHealthServer.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Security.AccessControl;

namespace McpHealthServer.Tools;

public class CheckApiStatusTool
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CheckApiStatusTool> _logger;

    public CheckApiStatusTool(HttpClient httpClient, ILogger<CheckApiStatusTool> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromMilliseconds(3000);
    }

    public async Task<CheckStatusResponseBase> ExecuteAsync(string url)
    {
        _logger.LogInformation("Checking API status: {Url}", url);

        var sw = Stopwatch.StartNew();

        try
        {
            url = NormalizeUrl(url);

            var response = await _httpClient.GetAsync(url);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("API UP: {Url} Status={StatusCode} Latency={Latency}ms", url,response.StatusCode,sw.ElapsedMilliseconds);

                return new UpStatusResponse
                {
                    Url = url,
                    Status = HealthStatusType.Up,
                    Checked_at = DateTime.UtcNow,
                    Http_Status = response.StatusCode,
                    Latency_ms = sw.ElapsedMilliseconds
                };
            }

            _logger.LogWarning("API DOWN (HTTP): {Url} Status={StatusCode}",url, response.StatusCode);

            return new DownStatusResponse
            {
                Url = url,
                Status = HealthStatusType.Down,
                Checked_at = DateTime.UtcNow,
                Error = $"HTTP {(int)response.StatusCode}"
            };
        }
        catch (TaskCanceledException ex)
        {
            sw.Stop();

            _logger.LogWarning(ex,"API TIMEOUT after {Timeout}ms: {Url}",_httpClient.Timeout.TotalMilliseconds,url);

            return new DownStatusResponse
            {
                Url = url,
                Status = HealthStatusType.Down,
                Checked_at = DateTime.UtcNow,
                Error = $"Timeout after {_httpClient.Timeout.TotalMilliseconds}ms"
            };
        }
        catch (Exception ex)
        {
            sw.Stop();

            _logger.LogError(ex,"Unexpected error while checking API: {Url}",url);

            return new DownStatusResponse
            {
                Url = url,
                Status = HealthStatusType.Down,
                Checked_at = DateTime.UtcNow,
                Error = "Unexpected error"
            };
        }
    }
    
    private static string NormalizeUrl(string url)
    {
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        return "https://" + url;
    }
}

