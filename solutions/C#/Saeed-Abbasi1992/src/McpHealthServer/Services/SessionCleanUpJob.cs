using McpHealthServer.Options;
using Microsoft.Extensions.Options;

public class SessionCleanUpJob : BackgroundService
{
    private readonly SessionService _sessionService;
    private readonly SessionCleanupOptions _options;
    private readonly ILogger<SessionCleanUpJob> _logger;

    public SessionCleanUpJob(
        SessionService sessionService,
        IOptions<SessionCleanupOptions> options,
        ILogger<SessionCleanUpJob> logger)
    {
        _sessionService = sessionService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "SessionCleanupService started. Timeout={Timeout} minutes, Interval={Interval}s",
            _options.SessionTimeoutMinutes,
            _options.CleanupIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _sessionService.CleanupExpiredSessions(
                    TimeSpan.FromMinutes(_options.SessionTimeoutMinutes));

                _logger.LogDebug("Expired sessions cleaned up");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_options.CleanupIntervalSeconds),
                stoppingToken);
        }

        _logger.LogInformation("SessionCleanupService stopping");
    }
}
