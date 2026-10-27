using System.Text.Json.Serialization;

namespace ContentManager.Api.Features.Publishing;

[JsonConverter(typeof(JsonStringEnumConverter<ProductState>))]
public enum ProductState { Draft, Published, Unpublished, Archived }

[JsonConverter(typeof(JsonStringEnumConverter<PricingMode>))]
public enum PricingMode { Free, Fixed, PWYW, Suggested, OptionalDonation, FixedPlusDonation }

/// <summary>Authoring price configuration in integer NPR paisa; the server owns its version.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PricingInput(PricingMode Mode, string Currency = "NPR", long? FixedPaisa = null,
    long? MinimumPaisa = null, long? SuggestedPaisa = null, bool AllowZero = true, bool ContributionEnabled = false);

/// <summary>Versioned owner-approved license text.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LicenseInput(int Version, string Text);

/// <summary>Plain-text search-engine metadata.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SeoInput(string? Title = null, string? Description = null);

/// <summary>Product fields. Create requires title and slug; PATCH changes only present fields.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ProductWriteRequest
{
    public string? Title { get; init; }
    public string? Slug { get; init; }
    public string? Summary { get; init; }
    public string? DescriptionHtml { get; init; }
    public Guid[]? CategoryIds { get; init; }
    public string[]? Tags { get; init; }
    public Guid? CoverAssetId { get; init; }
    public Guid[]? AssetIds { get; init; }
    public PricingInput? Pricing { get; init; }
    public LicenseInput? License { get; init; }
    public SeoInput? Seo { get; init; }
}

/// <summary>Category name and slug; PATCH may supply either field.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CategoryWriteRequest(string? Name, string? Slug);

/// <summary>Requested lifecycle transition and optional plain-text reason.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ProductTransitionRequest(ProductState To, string? Reason);

/// <summary>Server-owned version plus authoring price fields.</summary>
public sealed record PricingResponse(PricingMode Mode, string Currency, long? FixedPaisa,
    long? MinimumPaisa, long? SuggestedPaisa, bool AllowZero, bool ContributionEnabled, int Version);

/// <summary>Private administrator product preview; contains no storage keys or credentials.</summary>
public sealed record ProductResponse(Guid Id, string Title, string Slug, string Summary, string DescriptionHtml,
    ProductState State, Guid[] CategoryIds, string[] Tags, Guid? CoverAssetId, Guid[] AssetIds,
    PricingResponse Pricing, LicenseInput License, SeoInput Seo, string[] RedirectSlugs, long Revision,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? PublishedAt)
{
    public IReadOnlyList<AssetPreviewResponse> Attachments { get; init; } = [];
}

/// <summary>Private attachment metadata and validation status, never storage credentials or object keys.</summary>
public sealed record AssetPreviewResponse(Guid Id, string Kind, string Status, string Name, string MediaType, long SizeBytes);

/// <summary>Administrator category metadata and revision.</summary>
public sealed record CategoryResponse(Guid Id, string Name, string Slug, long Revision,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

/// <summary>Bounded page ordered by creation time and identifier.</summary>
public sealed record PublishingPage<T>(IReadOnlyList<T> Items, string? NextCursor);

/// <summary>Non-secret standalone support policy; not a payment-provider configuration.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record StandaloneDonationSetting
{
    public required bool Enabled { get; init; }
}

/// <summary>Full replacement of the allowlisted standalone-donation policy.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SettingWriteRequest
{
    public required StandaloneDonationSetting Value { get; init; }
}

/// <summary>Versioned non-secret policy; missing persisted configuration defaults to disabled.</summary>
public sealed record SettingResponse(string Key, StandaloneDonationSetting Value, long Revision, DateTimeOffset? UpdatedAt);

/// <summary>Allowlisted audit metadata, excluding credentials and arbitrary stored change payloads.</summary>
public sealed record AuditEventResponse(Guid Id, string Action, Guid ActorId, string EntityType,
    string EntityId, DateTimeOffset At, string? Reason, long? FromRevision, long? ToRevision);

public sealed class PublishingSettingDocument
{
    public string Id { get; set; } = "standaloneDonationsEnabled";
    public int SchemaVersion { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public StandaloneDonationSetting Value { get; set; } = new() { Enabled = false };
    public long Revision { get; set; } = 1;
    public DateTime? UpdatedAt { get; set; }
    public List<PublishingAuditIntent> AuditIntents { get; set; } = [];
}

public sealed class PublishingAuditIntent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActorId { get; set; }
    public string Action { get; set; } = "";
    public DateTime At { get; set; }
    public long FromRevision { get; set; }
    public long ToRevision { get; set; }
    public string? Reason { get; set; }
}

public sealed class ProductDocument
{
    public Guid Id { get; set; }
    public int SchemaVersion { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long Revision { get; set; } = 1;
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Summary { get; set; } = "";
    public string DescriptionHtml { get; set; } = "";
    public ProductState State { get; set; }
    public Guid[] CategoryIds { get; set; } = [];
    public string[] Tags { get; set; } = [];
    public Guid? CoverAssetId { get; set; }
    public Guid[] AssetIds { get; set; } = [];
    public PricingInput Pricing { get; set; } = new(PricingMode.Free);
    public int PricingVersion { get; set; } = 1;
    public LicenseInput License { get; set; } = new(1, "");
    public SeoInput Seo { get; set; } = new();
    public string[] RedirectSlugs { get; set; } = [];
    public DateTime? PublishedAt { get; set; }
    public bool Deleted { get; set; }
    public Guid CategoryBindingId { get; set; }
    public List<PublishingAuditIntent> AuditIntents { get; set; } = [];
    public string CreateHash { get; set; } = "";
}

public sealed class CategoryDocument
{
    public Guid Id { get; set; }
    public int SchemaVersion { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long Revision { get; set; } = 1;
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public bool Deleted { get; set; }
    public List<Guid> ProductBindings { get; set; } = [];
    public List<PublishingAuditIntent> AuditIntents { get; set; } = [];
    public string CreateHash { get; set; } = "";
}

public sealed class SlugReservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public Guid ProductId { get; set; }
    public string Kind { get; set; } = "Current";
}

public sealed class PublishingException(int status, string code, string message,
    Dictionary<string, string[]>? errors = null) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
    public Dictionary<string, string[]>? Errors { get; } = errors;
}
