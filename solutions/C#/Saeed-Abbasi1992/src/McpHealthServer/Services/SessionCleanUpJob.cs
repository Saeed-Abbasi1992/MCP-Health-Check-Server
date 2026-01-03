using Microsoft.Extensions.Options;

public class SessionCleanupOptions
{
    public int CleanupIntervalSeconds { get; set; } = 30;
    public int SessionTimeoutMinutes { get; set; } = 5;
}

public class SessionCleanUpJob : BackgroundService
{
    private readonly SessionService _sessionService;
    private readonly SessionCleanupOptions _options;
    private readonly ILogger<SessionCleanUpJob> _logger;

    public SessionCleanUpJob(SessionService sessionService, ILogger<SessionCleanUpJob> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionCleanupService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _sessionService.CleanupExpiredSessions(TimeSpan.FromMinutes(_options.SessionTimeoutMinutes));
                _logger.LogDebug("Expired sessions cleaned up");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
            }

            await Task.Delay(_options.CleanupIntervalSeconds, stoppingToken);
        }

        _logger.LogInformation("SessionCleanupService stopping");
    }
}
