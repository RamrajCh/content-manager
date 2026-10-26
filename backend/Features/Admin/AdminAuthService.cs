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
            type => type == typeof(AdminAccount) || type == typeof(AdminSession) || type == typeof(AdminAuditIntent));
    }

    public async Task<OwnerSetup> ProvisionOwnerAsync(string login, string password)
    {
        login = NormalizeLogin(login);
        ValidateNewCredentials(login, password);
        await Infrastructure.DatabaseSetup.InitializeAdminAsync(database);
        if (await Admins.CountDocumentsAsync(FilterDefinition<AdminAccount>.Empty) > 0)
            throw new InvalidOperationException("An owner already exists. Use --recover-owner for offline recovery.");

        var admin = new AdminAccount { Login = login };
        var setup = ApplyCredentials(admin, password);
        admin.AuditIntents.Add(NewAudit("admin.provisioned"));
        try
        {
            await Admins.InsertOneAsync(admin);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new InvalidOperationException("An owner already exists. Use --recover-owner for offline recovery.");
        }
        await ProjectAuditAsync();
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
        var revision = admin.Revision;
        admin.Revision++;
        admin.AuditIntents.Add(NewAudit("admin.recovered"));
        var replaced = await Admins.ReplaceOneAsync(AtRevision(admin.Id, revision), admin);
        if (replaced.ModifiedCount != 1)
            throw new InvalidOperationException("Owner changed during recovery. Retry the recovery command.");
        await Sessions.DeleteManyAsync(s => s.AdminId == admin.Id && s.CredentialVersion != admin.CredentialVersion);
        await ProjectAuditAsync();
        return setup;
    }

    public async Task<LoginResult> LoginAsync(string login, string password, string code, CancellationToken ct = default)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var now = clock.GetUtcNow().UtcDateTime;
            var admin = await Admins.Find(a => a.Login == NormalizeLogin(login)).FirstOrDefaultAsync(ct);
            var passwordResult = Hasher.VerifyHashedPassword(admin ?? Placeholder, admin?.PasswordHash ?? PlaceholderHash, password);
            if (admin is null || admin.Disabled) return new(LoginOutcome.Invalid);
            if (admin.LockedUntil > now) return new(LoginOutcome.Throttled, RetryAfter: admin.LockedUntil - now);

            var factor = passwordResult == PasswordVerificationResult.Failed ? null : VerifySecondFactor(admin, code, now);
            var update = Builders<AdminAccount>.Update.Inc(a => a.Revision, 1);
            var version = admin.CredentialVersion;
            if (factor is null)
            {
                var failures = admin.FailedAttempts + 1;
                update = update.Set(a => a.FailedAttempts, failures >= MaxFailures ? 0 : failures)
                    .Set(a => a.LockedUntil, failures >= MaxFailures ? now + LockoutDuration : null);
            }
            else
            {
                update = Builders<AdminAccount>.Update.Combine(update, factor).Set(a => a.LastLoginAt, now)
                    .Set(a => a.FailedAttempts, 0).Set(a => a.LockedUntil, null);
                if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
                    update = update.Set(a => a.PasswordHash, Hasher.HashPassword(admin, password));
                if (!IsTotp(code))
                {
                    version = AdminCrypto.NewToken();
                    update = update.Set(a => a.CredentialVersion, version)
                        .Push(a => a.AuditIntents, NewAudit("admin.recovery-code-used"));
                }
            }

            var changed = await Admins.UpdateOneAsync(
                AtRevision(admin.Id, admin.Revision) & Builders<AdminAccount>.Filter.Eq(a => a.Disabled, false),
                update, cancellationToken: ct);
            if (changed.ModifiedCount != 1) continue;
            if (factor is null) return new(LoginOutcome.Invalid);

            var token = AdminCrypto.NewToken();
            var expiresAt = now + AbsoluteTimeout;
            await Sessions.InsertOneAsync(new AdminSession
            {
                TokenHash = crypto.Hash("session", token),
                AdminId = admin.Id,
                CredentialVersion = version,
                ExpiresAt = expiresAt,
                LastActivityAt = now
            }, cancellationToken: ct);
            return new(LoginOutcome.Success, token, expiresAt);
        }
        return new(LoginOutcome.Throttled, RetryAfter: TimeSpan.FromSeconds(1));
    }

    public async Task<AdminIdentity?> AuthenticateAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 128) return null;
        var now = clock.GetUtcNow().UtcDateTime;
        var hash = crypto.Hash("session", token);
        var session = await Sessions.FindOneAndUpdateAsync(
            s => s.TokenHash == hash && s.Scope == "Admin" && s.ExpiresAt > now && s.LastActivityAt > now - IdleTimeout,
            Builders<AdminSession>.Update.Max(s => s.LastActivityAt, now),
            new FindOneAndUpdateOptions<AdminSession> { ReturnDocument = ReturnDocument.After }, ct);
        if (session is null) return null;

        var admin = await Admins.Find(a => a.Id == session.AdminId && !a.Disabled).FirstOrDefaultAsync(ct);
        if (admin is null || admin.CredentialVersion != session.CredentialVersion) return null;
        return new(admin.Id, admin.Login, session.ExpiresAt);
    }

    public async Task RevokeAsync(string token, CancellationToken ct = default)
    {
        var hash = crypto.Hash("session", token);
        await Sessions.DeleteOneAsync(s => s.TokenHash == hash, ct);
    }

    private static bool IsTotp(string code)
    {
        var normalized = NormalizeCode(code);
        return normalized.Length == 6 && normalized.All(char.IsAsciiDigit);
    }

    private static string NormalizeCode(string code) => code.Replace("-", "").Replace(" ", "").Trim();

    private UpdateDefinition<AdminAccount>? VerifySecondFactor(AdminAccount admin, string code, DateTime now)
    {
        var normalized = NormalizeCode(code);
        if (IsTotp(code))
        {
            var totp = new Totp(crypto.Unprotect(admin.TotpSecretCiphertext));
            if (!totp.VerifyTotp(now, normalized, out var step, new VerificationWindow(1, 1)) || step <= admin.LastTotpStep)
                return null;
            return Builders<AdminAccount>.Update.Set(a => a.LastTotpStep, step);
        }

        if (normalized.Length is < 10 or > 20) return null;
        var hash = crypto.Hash("recovery", normalized.ToUpperInvariant());
        return admin.RecoveryCodeHashes.Contains(hash) ? Builders<AdminAccount>.Update.Pull(a => a.RecoveryCodeHashes, hash) : null;
    }

    private static FilterDefinition<AdminAccount> AtRevision(Guid id, long revision) =>
        Builders<AdminAccount>.Filter.Eq(a => a.Id, id)
        & (revision == 0
            ? Builders<AdminAccount>.Filter.Or(Builders<AdminAccount>.Filter.Eq(a => a.Revision, 0),
                Builders<AdminAccount>.Filter.Exists(a => a.Revision, false))
            : Builders<AdminAccount>.Filter.Eq(a => a.Revision, revision));

    private OwnerSetup ApplyCredentials(AdminAccount admin, string password)
    {
        var secret = KeyGeneration.GenerateRandomKey(20);
        var encodedSecret = Base32Encoding.ToString(secret);
        var codes = Enumerable.Range(0, 8)
            .Select(_ => RandomNumberGenerator.GetString(RecoveryAlphabet, 10)).ToList();
        admin.PasswordHash = Hasher.HashPassword(admin, password);
        admin.CredentialVersion = AdminCrypto.NewToken();
        admin.TotpSecretCiphertext = crypto.Protect(secret);
        admin.RecoveryCodeHashes = codes.Select(code => crypto.Hash("recovery", code)).ToList();
        var uri = $"otpauth://totp/ContentManager:{Uri.EscapeDataString(admin.Login)}"
            + $"?secret={encodedSecret}&issuer=ContentManager&digits=6&period=30";
        return new(admin.Login, encodedSecret, uri, codes.Select(code => $"{code[..5]}-{code[5..]}").ToList());
    }

    private AdminAuditIntent NewAudit(string action) => new() { Action = action, At = clock.GetUtcNow().UtcDateTime };

    public async Task ProjectAuditAsync()
    {
        var admins = await Admins.Find(FilterDefinition<AdminAccount>.Empty).ToListAsync();
        foreach (var admin in admins)
        foreach (var intent in admin.AuditIntents)
        {
            var id = new BsonBinaryData(intent.Id, GuidRepresentation.Standard);
            var document = new BsonDocument
            {
                ["_id"] = id,
                ["actorId"] = new BsonBinaryData(admin.Id, GuidRepresentation.Standard),
                ["action"] = intent.Action,
                ["entityType"] = "admin",
                ["entityId"] = new BsonBinaryData(admin.Id, GuidRepresentation.Standard),
                ["at"] = intent.At,
                ["changeSummary"] = new BsonDocument()
            };
            await database.GetCollection<BsonDocument>("auditEvents").UpdateOneAsync(
                new BsonDocument("_id", id), new BsonDocument("$setOnInsert", document), new UpdateOptions { IsUpsert = true });
        }
    }

    private static string NormalizeLogin(string login) => login.Trim().ToLowerInvariant();

    private static void ValidateNewCredentials(string login, string password)
    {
        if (login.Length is < 3 or > 64) throw new InvalidOperationException("Login must be 3 to 64 characters.");
        if (password.Length is < 14 or > 256)
            throw new InvalidOperationException("Password must be 14 to 256 characters.");
    }
}
