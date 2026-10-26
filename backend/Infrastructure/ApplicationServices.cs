using System.Diagnostics;
using ContentManager.Api.Features.Admin;
using ContentManager.Api.Features.Publishing;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ContentManager.Api.Infrastructure;

public static class ApplicationServices
{
    public static void AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options => options.IncludeScopes = false);
        builder.Services.AddOptions<PlatformOptions>()
            .BindConfiguration("Platform")
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<PlatformOptions>, PlatformOptionsValidator>();
        builder.Services.AddSingleton<IMongoClient>(services =>
        {
            var options = services.GetRequiredService<IOptions<PlatformOptions>>().Value;
            var settings = MongoClientSettings.FromConnectionString(options.Mongo.ConnectionString);
            settings.MaxConnectionPoolSize = 20;
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
            settings.ConnectTimeout = TimeSpan.FromSeconds(3);
            settings.ApplicationName = "ContentManager.Api";
            return new MongoClient(settings);
        });
        builder.Services.AddSingleton(services =>
        {
            var options = services.GetRequiredService<IOptions<PlatformOptions>>().Value;
            return services.GetRequiredService<IMongoClient>().GetDatabase(options.Mongo.Database);
        });
        builder.Services.AddHealthChecks().AddCheck<MongoReadinessCheck>("database");
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Detail = null;
                context.ProblemDetails.Extensions["traceId"] =
                    Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
                context.ProblemDetails.Extensions.TryAdd("code", $"HTTP_{context.ProblemDetails.Status}");
            };
        });
        builder.Services.AddExceptionHandler<SafeExceptionHandler>();
        builder.Services.AddAdminAuthentication();
        PublishingService.RegisterConventions();
        builder.Services.AddSingleton<IPublishingService, PublishingService>();
        builder.Services.AddHostedService<PublishingAuditWorker>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
            options.SwaggerDoc("v1", new() { Title = "ContentManager API", Version = "v1" }));
    }
}
