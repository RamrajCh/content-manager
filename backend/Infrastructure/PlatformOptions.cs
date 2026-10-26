using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ContentManager.Api.Infrastructure;

public sealed class PlatformOptions
{
    public bool LocalServices { get; init; }
    public MongoOptions Mongo { get; init; } = new();
    public StorageOptions Storage { get; init; } = new();
    public MailOptions Mail { get; init; } = new();
    public SecurityOptions Security { get; init; } = new();
}

public sealed class MongoOptions
{
    public string ConnectionString { get; init; } = "";
    public string Database { get; init; } = "";
}

public sealed class StorageOptions
{
    public string Endpoint { get; init; } = "";
    public string Bucket { get; init; } = "";
    public string AccessKey { get; init; } = "";
    public string SecretKey { get; init; } = "";
}

public sealed class MailOptions
{
    public string Host { get; init; } = "";
    public int Port { get; init; }
}

public sealed class SecurityOptions
{
    public string KeyVersion { get; init; } = "";
    public string EncryptionKey { get; init; } = "";
    public string LookupKey { get; init; } = "";
    public string PublicOrigin { get; init; } = "";
}

public sealed class PlatformOptionsValidator(IHostEnvironment environment) : IValidateOptions<PlatformOptions>
{
    public ValidateOptionsResult Validate(string? name, PlatformOptions options)
    {
        var errors = new List<string>();
        MongoUrl? mongoUrl = null;
        try
        {
            mongoUrl = new MongoUrl(options.Mongo.ConnectionString);
        }
        catch (Exception exception) when (exception is MongoConfigurationException or FormatException or ArgumentException)
        {
            errors.Add("Platform:Mongo:ConnectionString must be a valid MongoDB URI.");
        }

        if (string.IsNullOrWhiteSpace(options.Mongo.Database)
            || options.Mongo.Database.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '_' and not '-'))
            errors.Add("Platform:Mongo:Database must contain only ASCII letters, numbers, underscores or hyphens.");

        var validEndpoint = Uri.TryCreate(options.Storage.Endpoint, UriKind.Absolute, out var endpoint)
            && endpoint.Scheme is "http" or "https" && string.IsNullOrEmpty(endpoint.UserInfo);
        if (!validEndpoint)
            errors.Add("Platform:Storage:Endpoint must be an HTTP(S) endpoint without credentials.");
        if (string.IsNullOrWhiteSpace(options.Storage.Bucket)
            || string.IsNullOrWhiteSpace(options.Storage.AccessKey)
            || string.IsNullOrWhiteSpace(options.Storage.SecretKey))
            errors.Add("Platform:Storage bucket and credentials are required.");
        if (string.IsNullOrWhiteSpace(options.Mail.Host) || options.Mail.Port is < 1 or > 65535)
            errors.Add("Platform:Mail host and valid port are required.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(options.Security.KeyVersion, "^v[1-9][0-9]*$"))
            errors.Add("Platform:Security:KeyVersion must be v followed by a positive integer.");
        if (!IsKey(options.Security.EncryptionKey) || !IsKey(options.Security.LookupKey)
            || options.Security.EncryptionKey == options.Security.LookupKey)
            errors.Add("Platform:Security requires two distinct base64-encoded 32-byte keys.");

        var validOrigin = Uri.TryCreate(options.Security.PublicOrigin, UriKind.Absolute, out var origin)
            && origin.Scheme is "http" or "https" && string.IsNullOrEmpty(origin.UserInfo)
            && origin.AbsolutePath == "/" && string.IsNullOrEmpty(origin.Query) && string.IsNullOrEmpty(origin.Fragment);
        if (!validOrigin)
            errors.Add("Platform:Security:PublicOrigin must be an origin such as https://example.com without a path.");
        else if (!environment.IsDevelopment() && origin!.Scheme != "https")
            errors.Add("Non-development PublicOrigin requires HTTPS.");

        if (options.LocalServices && !environment.IsDevelopment())
            errors.Add("Local services are permitted only in Development.");
        if (options.LocalServices
            && (mongoUrl is null || mongoUrl.Servers.Any(server => !IsLoopback(server.Host))
                || endpoint is null || !endpoint.IsLoopback || !IsLoopback(options.Mail.Host)))
            errors.Add("Local services must use loopback endpoints.");
        if (!environment.IsDevelopment() && (endpoint is null || endpoint.Scheme != "https"))
            errors.Add("Non-development storage requires HTTPS.");
        if (!environment.IsDevelopment()
            && ((mongoUrl?.Servers.Any(server => IsLoopback(server.Host)) ?? false)
                || endpoint?.IsLoopback == true || IsLoopback(options.Mail.Host)))
            errors.Add("Non-development configuration cannot use loopback development services.");

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }

    private static bool IsLoopback(string host) => host is "localhost" or "127.0.0.1" or "::1";

    private static bool IsKey(string value)
    {
        Span<byte> bytes = stackalloc byte[32];
        return Convert.TryFromBase64String(value, bytes, out var written) && written == 32;
    }
}
