namespace McpHealthServer.Options
{
    public class SessionCleanupOptions
    {
        public int CleanupIntervalSeconds { get; set; } = 30;
        public int SessionTimeoutMinutes { get; set; } = 5;
    }
}
