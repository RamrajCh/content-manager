using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using OtpNet;

namespace ContentManager.Api.Features.Admin;

public sealed class AdminAuthService(IMongoDatabase database, AdminCrypto crypto, TimeProvider clock)
{
    public const int MaxFailures = 5;
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan AbsoluteTimeout = TimeSpan.FromHours(8);
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const string RecoveryAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private static readonly PasswordHasher<AdminAccount> Hasher = new(
        Microsoft.Extensions.Options.Options.Create(new PasswordHasherOptions { IterationCount = 210_000 }));
    private static readonly AdminAccount Placeholder = new();
    private static readonly string PlaceholderHash = Hasher.HashPassword(Placeholder, Guid.NewGuid().ToString("N"));

    private IMongoCollection<AdminAccount> Admins => database.GetCollection<AdminAccount>("admins");
    private IMongoCollection<AdminSession> Sessions => database.GetCollection<AdminSession>("sessions");

    public static void RegisterConventions()
    {
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        ConventionRegistry.Register("ContentManagerAdmin",
            new ConventionPack { new CamelCaseElementNameConvention(), new IgnoreExtraElementsConvention(true) },
            type => type == typeof(AdminAccount) || type == typeof(AdminSession));
    }

    public async Task<OwnerSetup> ProvisionOwnerAsync(string login, string password)
    {
        login = NormalizeLogin(login);
        ValidateNewCredentials(login, password);
        if (await Admins.CountDocumentsAsync(FilterDefinition<AdminAccount>.Empty) > 0)
            throw new InvalidOperationException("An owner already exists. Use --recover-owner for offline recovery.");

        var admin = new AdminAccount { Login = login };
        var setup = ApplyCredentials(admin, password);
        await Admins.InsertOneAsync(admin);
        await AuditAsync(admin.Id, "admin.provisioned");
        return setup;
    }

    public async Task<OwnerSetup> RecoverOwnerAsync(string password)
    {
        var admin = await Admins.Find(FilterDefinition<AdminAccount>.Empty).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No owner exists. Use --provision-owner first.");
        ValidateNewCredentials(admin.Login, password);

        var setup = ApplyCredentials(admin, password);
        admin.Disabled = false;
        admin.FailedAttempts = 0;
        admin.LockedUntil = null;
        admin.LastTotpStep = 0;
        await Admins.ReplaceOneAsync(a => a.Id == admin.Id, admin);
        await Sessions.DeleteManyAsync(s => s.AdminId == admin.Id);
        await AuditAsync(admin.Id, "admin.recovered");
        return setup;
    }

    public async Task<LoginResult> LoginAsync(string login, string password, string code)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var admin = await Admins.Find(a => a.Login == NormalizeLogin(login)).FirstOrDefaultAsync();
        var passwordResult = Hasher.VerifyHashedPassword(admin ?? Placeholder, admin?.PasswordHash ?? PlaceholderHash, password);
        if (admin is null || admin.Disabled) return new(LoginOutcome.Invalid);
        if (admin.LockedUntil > now) return new(LoginOutcome.Throttled, RetryAfter: admin.LockedUntil - now);

        if (passwordResult == PasswordVerificationResult.Failed || !await VerifySecondFactorAsync(admin, code, now))
        {
            await RegisterFailureAsync(admin, now);
            return new(LoginOutcome.Invalid);
        }

        var update = Builders<AdminAccount>.Update
            .Set(a => a.LastLoginAt, now).Set(a => a.FailedAttempts, 0).Set(a => a.LockedUntil, null);
        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
            update = update.Set(a => a.PasswordHash, Hasher.HashPassword(admin, password));
        await Admins.UpdateOneAsync(a => a.Id == admin.Id, update);

        var token = AdminCrypto.NewToken();
        var expiresAt = now + AbsoluteTimeout;
        await Sessions.InsertOneAsync(new AdminSession
        {
            TokenHash = crypto.Hash("session", token),
            AdminId = admin.Id,
            ExpiresAt = expiresAt,
            LastActivityAt = now
        });
        return new(LoginOutcome.Success, token, expiresAt);
    }

    public async Task<AdminIdentity?> AuthenticateAsync(string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 128) return null;
        var now = clock.GetUtcNow().UtcDateTime;
        var hash = crypto.Hash("session", token);
        var session = await Sessions.Find(s => s.TokenHash == hash && s.Scope == "Admin").FirstOrDefaultAsync();
        if (session is null) return null;
        if (session.ExpiresAt <= now || session.LastActivityAt + IdleTimeout <= now)
        {
            await Sessions.DeleteOneAsync(s => s.Id == session.Id);
            return null;
        }

        var admin = await Admins.Find(a => a.Id == session.AdminId && !a.Disabled).FirstOrDefaultAsync();
        if (admin is null) return null;
        await Sessions.UpdateOneAsync(s => s.Id == session.Id, Builders<AdminSession>.Update.Set(s => s.LastActivityAt, now));
        return new(admin.Id, admin.Login, session.ExpiresAt);
    }

    public async Task RevokeAsync(string token)
    {
        var hash = crypto.Hash("session", token);
        await Sessions.DeleteOneAsync(s => s.TokenHash == hash);
    }

    private async Task<bool> VerifySecondFactorAsync(AdminAccount admin, string code, DateTime now)
    {
        var normalized = code.Replace("-", "").Replace(" ", "").Trim();
        if (normalized.Length == 6 && normalized.All(char.IsAsciiDigit))
        {
            var totp = new Totp(crypto.Unprotect(admin.TotpSecretCiphertext));
            if (!totp.VerifyTotp(now, normalized, out var step, new VerificationWindow(1, 1))) return false;
            var result = await Admins.UpdateOneAsync(a => a.Id == admin.Id && a.LastTotpStep < step,
                Builders<AdminAccount>.Update.Set(a => a.LastTotpStep, step));
            return result.ModifiedCount == 1;
        }

        if (normalized.Length is < 10 or > 20) return false;
        var hash = crypto.Hash("recovery", normalized.ToUpperInvariant());
        var consumed = await Admins.UpdateOneAsync(a => a.Id == admin.Id && a.RecoveryCodeHashes.Contains(hash),
            Builders<AdminAccount>.Update.Pull(a => a.RecoveryCodeHashes, hash));
        return consumed.ModifiedCount == 1;
    }

    private async Task RegisterFailureAsync(AdminAccount admin, DateTime now)
    {
        var updated = await Admins.FindOneAndUpdateAsync(a => a.Id == admin.Id,
            Builders<AdminAccount>.Update.Inc(a => a.FailedAttempts, 1),
            new FindOneAndUpdateOptions<AdminAccount> { ReturnDocument = ReturnDocument.After });
        if (updated?.FailedAttempts >= MaxFailures)
            await Admins.UpdateOneAsync(a => a.Id == admin.Id,
                Builders<AdminAccount>.Update.Set(a => a.LockedUntil, now + LockoutDuration).Set(a => a.FailedAttempts, 0));
    }

    private OwnerSetup ApplyCredentials(AdminAccount admin, string password)
    {
        var secret = KeyGeneration.GenerateRandomKey(20);
        var encodedSecret = Base32Encoding.ToString(secret);
        var codes = Enumerable.Range(0, 8)
            .Select(_ => RandomNumberGenerator.GetString(RecoveryAlphabet, 10)).ToList();
        admin.PasswordHash = Hasher.HashPassword(admin, password);
        admin.TotpSecretCiphertext = crypto.Protect(secret);
        admin.RecoveryCodeHashes = codes.Select(code => crypto.Hash("recovery", code)).ToList();
        var uri = $"otpauth://totp/ContentManager:{Uri.EscapeDataString(admin.Login)}"
            + $"?secret={encodedSecret}&issuer=ContentManager&digits=6&period=30";
        return new(admin.Login, encodedSecret, uri, codes.Select(code => $"{code[..5]}-{code[5..]}").ToList());
    }

    private async Task AuditAsync(Guid adminId, string action) =>
        await database.GetCollection<BsonDocument>("auditEvents").InsertOneAsync(new BsonDocument
        {
            ["_id"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
            ["actorId"] = new BsonBinaryData(adminId, GuidRepresentation.Standard),
            ["action"] = action,
            ["entityType"] = "admin",
            ["entityId"] = new BsonBinaryData(adminId, GuidRepresentation.Standard),
            ["at"] = clock.GetUtcNow().UtcDateTime,
            ["changeSummary"] = new BsonDocument()
        });

    private static string NormalizeLogin(string login) => login.Trim().ToLowerInvariant();

    private static void ValidateNewCredentials(string login, string password)
    {
        if (login.Length is < 3 or > 64) throw new InvalidOperationException("Login must be 3 to 64 characters.");
        if (password.Length is < 14 or > 256)
            throw new InvalidOperationException("Password must be 14 to 256 characters.");
    }
}
