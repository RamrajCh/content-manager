using System.Diagnostics;

namespace ContentManager.Api.Infrastructure;

public static class ApplicationPipeline
{
    public static void UseApplicationPipeline(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api/v1/admin/products")
                || context.Request.Path.StartsWithSegments("/api/v1/admin/categories")
                || context.Request.Path.StartsWithSegments("/api/v1/admin/settings"))
            {
                var bodyLimit = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
                if (bodyLimit is { IsReadOnly: false }) bodyLimit.MaxRequestBodySize = 100_000;
            }
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
    }
}
