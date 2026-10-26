namespace ContentManager.Api.Features.Publishing;

public sealed class PublishingAuditWorker(IPublishingService publishing, ILogger<PublishingAuditWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        do
        {
            try { await publishing.ProjectAuditAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (MongoDB.Driver.MongoException exception)
            {
                logger.LogError("Publishing audit projection failed Type={ExceptionType}; durable intents retained for retry",
                    exception.GetType().Name);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
