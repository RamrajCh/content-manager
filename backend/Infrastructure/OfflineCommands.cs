using ContentManager.Api.Features.Admin;
using ContentManager.Api.Features.Publishing;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ContentManager.Api.Infrastructure;

public static class OfflineCommands
{
    public static async Task<bool> TryRunOfflineCommandAsync(this WebApplication app, string[] args)
    {
        var offlineModes = new[] { "--initialize-database", "--provision-owner", "--recover-owner", "--project-admin-audit", "--reconcile-publishing" };
        if (args.Count(argument => offlineModes.Contains(argument, StringComparer.Ordinal)) > 1)
        {
            Console.Error.WriteLine("Choose exactly one offline command.");
            Environment.ExitCode = 1;
            return true;
        }

        if (args.Contains("--project-admin-audit", StringComparer.Ordinal))
        {
            _ = app.Services.GetRequiredService<IOptions<PlatformOptions>>().Value;
            await app.Services.GetRequiredService<AdminAuthService>().ProjectAuditAsync();
            app.Logger.LogInformation("Administrator audit projection completed");
            return true;
        }

        if (args.Contains("--reconcile-publishing", StringComparer.Ordinal))
        {
            if (!args.Contains("--confirm-offline", StringComparer.Ordinal))
            {
                Console.Error.WriteLine("Stop all API writers, then add --confirm-offline to reconcile publishing references.");
                Environment.ExitCode = 1;
                return true;
            }
            _ = app.Services.GetRequiredService<IOptions<PlatformOptions>>().Value;
            await app.Services.GetRequiredService<IPublishingService>().ReconcileOfflineAsync(CancellationToken.None);
            app.Logger.LogInformation("Offline publishing reconciliation completed");
            return true;
        }

        if (args.Contains("--initialize-database", StringComparer.Ordinal))
        {
            _ = app.Services.GetRequiredService<IOptions<PlatformOptions>>().Value;
            await DatabaseSetup.InitializeAsync(app.Services.GetRequiredService<IMongoDatabase>());
            app.Logger.LogInformation("Database index initialization completed");
            return true;
        }

        if (args.Contains("--provision-owner", StringComparer.Ordinal) || args.Contains("--recover-owner", StringComparer.Ordinal))
        {
            _ = app.Services.GetRequiredService<IOptions<PlatformOptions>>().Value;
            await OwnerCommands.RunAsync(app.Services.GetRequiredService<AdminAuthService>(), args);
            return true;
        }

        return false;
    }
}
