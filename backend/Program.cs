using System.Diagnostics;
using ContentManager.Api.Features.Admin;
using ContentManager.Api.Infrastructure;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);
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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new() { Title = "ContentManager API", Version = "v1" }));

var app = builder.Build();

if (args.Contains("--initialize-database", StringComparer.Ordinal))
{
    _ = app.Services.GetRequiredService<IOptions<PlatformOptions>>().Value;
    await DatabaseSetup.InitializeAsync(app.Services.GetRequiredService<IMongoDatabase>());
    app.Logger.LogInformation("Database index initialization completed");
    return;
}

if (args.Contains("--provision-owner", StringComparer.Ordinal) || args.Contains("--recover-owner", StringComparer.Ordinal))
{
    _ = app.Services.GetRequiredService<IOptions<PlatformOptions>>().Value;
    await OwnerCommands.RunAsync(app.Services.GetRequiredService<AdminAuthService>(), args);
    return;
}

app.Use(async (context, next) =>
{
    var started = Stopwatch.GetTimestamp();
    var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    context.Response.Headers["X-Trace-Id"] = traceId;
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    try
    {
        await next(context);
    }
    finally
    {
        app.Logger.LogInformation(
            "HTTP {Method} completed {StatusCode} in {ElapsedMs}ms TraceId={TraceId}",
            context.Request.Method, context.Response.StatusCode,
            Stopwatch.GetElapsedTime(started).TotalMilliseconds, traceId);
    }
});
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    SuppressDiagnosticsCallback = _ => true
});
app.UseStatusCodePages();
app.UseRateLimiter();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "ContentManager API v1"));
}
app.MapAdminEndpoints();
app.MapGet("/api/v1/health/live", () => Results.Ok(new { status = "live" }));
app.MapHealthChecks("/api/v1/health/ready", new()
{
    ResponseWriter = async (context, report) =>
    {
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
                ? "ready" : "unavailable"
        });
    }
});
app.Run();
