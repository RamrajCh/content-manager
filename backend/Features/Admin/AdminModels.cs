namespace ContentManager.Api.Features.Admin;

public sealed class AdminAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Login { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string TotpSecretCiphertext { get; set; } = "";
    public List<string> RecoveryCodeHashes { get; set; } = [];
    public bool Disabled { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public long LastTotpStep { get; set; }
    public string OwnerSlot { get; set; } = "owner";
    public long Revision { get; set; }
    public string CredentialVersion { get; set; } = "";
    public List<AdminAuditIntent> AuditIntents { get; set; } = [];
}

public sealed class AdminAuditIntent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Action { get; set; } = "";
    public DateTime At { get; set; }
}

public sealed class AdminSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TokenHash { get; set; } = "";
    public Guid AdminId { get; set; }
    public string Scope { get; set; } = "Admin";
    public DateTime ExpiresAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public string CredentialVersion { get; set; } = "";
}

/// <summary>Administrator credentials; the second factor is mandatory.</summary>
public sealed record LoginRequest(string? Login, string? Password, string? TotpOrRecoveryCode);

/// <summary>Absolute expiry of a newly established administrator session.</summary>
public sealed record LoginResponse(DateTimeOffset ExpiresAt);

/// <summary>Non-secret identity and expiry of the current administrator session.</summary>
public sealed record AdminSessionResponse(string Login, DateTimeOffset ExpiresAt);

/// <summary>Cookie-bound token required on unsafe administrator requests.</summary>
public sealed record CsrfResponse(string CsrfToken);

public enum LoginOutcome { Success, Invalid, Throttled }

public sealed record LoginResult(
    LoginOutcome Outcome, string? Token = null, DateTime? ExpiresAt = null, TimeSpan? RetryAfter = null);

public sealed record AdminIdentity(Guid Id, string Login, DateTime ExpiresAt);

public sealed record OwnerSetup(
    string Login, string TotpSecret, string OtpAuthUri, IReadOnlyList<string> RecoveryCodes);
