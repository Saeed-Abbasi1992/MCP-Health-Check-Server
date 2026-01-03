namespace McpHealthServer.Models;

public class CheckStatusResponseBase
{
    public string Url { get; set; }
    public HealthStatusType Status { get; set; }
    public DateTime Checked_at { get; set; }
}