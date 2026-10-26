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
}

public sealed class AdminSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TokenHash { get; set; } = "";
    public Guid AdminId { get; set; }
    public string Scope { get; set; } = "Admin";
    public DateTime ExpiresAt { get; set; }
    public DateTime LastActivityAt { get; set; }
}

public sealed record LoginRequest(string? Login, string? Password, string? Code);

public enum LoginOutcome { Success, Invalid, Throttled }

public sealed record LoginResult(
    LoginOutcome Outcome, string? Token = null, DateTime? ExpiresAt = null, TimeSpan? RetryAfter = null);

public sealed record AdminIdentity(Guid Id, string Login, DateTime ExpiresAt);

public sealed record OwnerSetup(
    string Login, string TotpSecret, string OtpAuthUri, IReadOnlyList<string> RecoveryCodes);
