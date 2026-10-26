using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ContentManager.Api.Infrastructure;

public sealed class MongoReadinessCheck(IMongoDatabase database, ILogger<MongoReadinessCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(4));
        try
        {
            await database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1), cancellationToken: timeout.Token);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is MongoException or TimeoutException or OperationCanceledException)
        {
            logger.LogWarning("Database readiness failed Type={ExceptionType}", exception.GetType().Name);
            return HealthCheckResult.Unhealthy("Database unavailable.");
        }
    }
}
