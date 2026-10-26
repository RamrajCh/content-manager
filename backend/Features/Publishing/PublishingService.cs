using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContentManager.Api.Infrastructure;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace ContentManager.Api.Features.Publishing;

public interface IPublishingService
{
    Task<ProductResponse> GetProductAsync(Guid id, CancellationToken ct);
    Task<PublishingPage<ProductResponse>> ListProductsAsync(string? state, string? cursor, int limit, CancellationToken ct);
    Task<ProductResponse> CreateProductAsync(Guid id, ProductWriteRequest request, Guid actor, CancellationToken ct);
    Task<ProductResponse> UpdateProductAsync(Guid id, long revision, ProductWriteRequest request, Guid actor, CancellationToken ct);
    Task<ProductResponse> TransitionAsync(Guid id, long revision, ProductTransitionRequest request, Guid actor, CancellationToken ct);
    Task DeleteProductAsync(Guid id, long revision, Guid actor, CancellationToken ct);
    Task<CategoryResponse> GetCategoryAsync(Guid id, CancellationToken ct);
    Task<PublishingPage<CategoryResponse>> ListCategoriesAsync(string? cursor, int limit, CancellationToken ct);
    Task<CategoryResponse> CreateCategoryAsync(Guid id, CategoryWriteRequest request, Guid actor, CancellationToken ct);
    Task<CategoryResponse> UpdateCategoryAsync(Guid id, long revision, CategoryWriteRequest request, Guid actor, CancellationToken ct);
    Task DeleteCategoryAsync(Guid id, long revision, Guid actor, CancellationToken ct);
    Task ProjectAuditAsync(CancellationToken ct);
    Task ReconcileOfflineAsync(CancellationToken ct);
}

public sealed class PublishingService(IMongoDatabase database, TimeProvider clock) : IPublishingService
{
    private IMongoCollection<ProductDocument> Products => database.GetCollection<ProductDocument>("products");
    private IMongoCollection<CategoryDocument> Categories => database.GetCollection<CategoryDocument>("categories");
    private IMongoCollection<SlugReservation> Slugs => database.GetCollection<SlugReservation>("slugReservations");
    private const int AuditLimit = 100;

    public static void RegisterConventions() => ConventionRegistry.Register("Publishing",
        new ConventionPack { new CamelCaseElementNameConvention(), new IgnoreExtraElementsConvention(true),
            new EnumRepresentationConvention(BsonType.String) },
        type => type.Namespace == typeof(PublishingService).Namespace);

    public async Task<ProductResponse> GetProductAsync(Guid id, CancellationToken ct)
    {
        var product = await ProductAsync(id, ct);
        var attachments = new List<AssetPreviewResponse>();
        foreach (var assetId in product.AssetIds.Concat(product.CoverAssetId.HasValue ? [product.CoverAssetId.Value] : []).Distinct())
        {
            var asset = await database.GetCollection<BsonDocument>("assets")
                .Find(new BsonDocument("_id", new BsonBinaryData(assetId, GuidRepresentation.Standard))).FirstOrDefaultAsync(ct);
            if (asset is null)
                throw new PublishingException(409, "ASSET_METADATA_MISSING", "Referenced asset metadata is missing; repair it before editing.");
            attachments.Add(new(assetId, asset["kind"].AsString, asset["status"].AsString,
                asset["originalName"].AsString, asset["mediaType"].AsString, asset["sizeBytes"].ToInt64()));
        }
        return ToResponse(product) with { Attachments = attachments };
    }
    public async Task<CategoryResponse> GetCategoryAsync(Guid id, CancellationToken ct) => ToResponse(await CategoryAsync(id, ct));

    public async Task<PublishingPage<ProductResponse>> ListProductsAsync(string? state, string? cursor, int limit, CancellationToken ct)
    {
        ValidateLimit(limit);
        var filter = Builders<ProductDocument>.Filter.Eq(p => p.Deleted, false);
        if (state is not null)
        {
            if (!Enum.TryParse<ProductState>(state, false, out var parsed) || !Enum.IsDefined(parsed))
                throw PublishingValidation.Invalid("state", "Unknown product state.");
            filter &= Builders<ProductDocument>.Filter.Eq(p => p.State, parsed);
        }
        if (cursor is not null)
        {
            var (at, id) = DecodeCursor(cursor);
            filter &= Builders<ProductDocument>.Filter.Gt(p => p.CreatedAt, at)
                | (Builders<ProductDocument>.Filter.Eq(p => p.CreatedAt, at) & Builders<ProductDocument>.Filter.Gt(p => p.Id, id));
        }
        var items = await Products.Find(filter).SortBy(p => p.CreatedAt).ThenBy(p => p.Id).Limit(limit + 1).ToListAsync(ct);
        var hasMore = items.Count > limit;
        var page = items.Take(limit).ToArray();
        return new(page.Select(ToResponse).ToArray(), hasMore ? EncodeCursor(page[^1].CreatedAt, page[^1].Id) : null);
    }

    public async Task<PublishingPage<CategoryResponse>> ListCategoriesAsync(string? cursor, int limit, CancellationToken ct)
    {
        ValidateLimit(limit);
        var filter = Builders<CategoryDocument>.Filter.Eq(c => c.Deleted, false);
        if (cursor is not null)
        {
            var (at, id) = DecodeCursor(cursor);
            filter &= Builders<CategoryDocument>.Filter.Gt(c => c.CreatedAt, at)
                | (Builders<CategoryDocument>.Filter.Eq(c => c.CreatedAt, at) & Builders<CategoryDocument>.Filter.Gt(c => c.Id, id));
        }
        var items = await Categories.Find(filter).SortBy(c => c.CreatedAt).ThenBy(c => c.Id).Limit(limit + 1).ToListAsync(ct);
        var page = items.Take(limit).ToArray();
        return new(page.Select(ToResponse).ToArray(), items.Count > limit ? EncodeCursor(page[^1].CreatedAt, page[^1].Id) : null);
    }

    public async Task<ProductResponse> CreateProductAsync(Guid id, ProductWriteRequest request, Guid actor, CancellationToken ct)
    {
        var now = Now();
        var product = PublishingValidation.Apply(new ProductDocument { Id = id, CreatedAt = now, UpdatedAt = now }, request, true);
        product.CreateHash = Hash(request, actor);
        var existing = await Products.Find(p => p.Id == id).FirstOrDefaultAsync(ct);
        if (existing is not null) return ReplayProduct(existing, product.CreateHash);
        await ValidateAssetsAsync(product, ct);
        await ReserveSlugAsync(product.Slug, id, ct);
        product.CategoryBindingId = Guid.NewGuid();
        await BindCategoriesAsync(product.CategoryIds, product.CategoryBindingId, ct);
        product.AuditIntents.Add(Intent(actor, "product.created", 0, 1));
        try { await Products.InsertOneAsync(product, cancellationToken: ct); }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            await ReleaseCategoriesAsync(product.CategoryIds, product.CategoryBindingId, ct);
            existing = await Products.Find(p => p.Id == id).FirstOrDefaultAsync(ct);
            if (existing is not null) return ReplayProduct(existing, product.CreateHash);
            throw Conflict("SLUG_CONFLICT", "Product slug is already reserved.");
        }
        return ToResponse(product);
    }

    public async Task<ProductResponse> UpdateProductAsync(Guid id, long revision, ProductWriteRequest request, Guid actor, CancellationToken ct)
    {
        var product = await ProductAsync(id, ct);
        CheckRevision(product.Revision, revision);
        if (product.State == ProductState.Published)
            throw Conflict("UNPUBLISH_BEFORE_EDIT", "Unpublish before editing until validated publication is available in EX-09.");
        CheckAuditCapacity(product.AuditIntents);
        var oldSlug = product.Slug;
        var oldCategories = product.CategoryIds;
        var oldBinding = product.CategoryBindingId;
        var oldLicense = product.License;
        PublishingValidation.Apply(product, request, false);
        if (product.License != oldLicense && product.License.Version == oldLicense.Version)
            throw PublishingValidation.Invalid("license.version", "Changed license requires a new version.");
        await ValidateAssetsAsync(product, ct);
        if (oldSlug != product.Slug)
            product.RedirectSlugs = product.RedirectSlugs.Append(oldSlug).Where(s => s != product.Slug).Distinct().ToArray();
        if (product.RedirectSlugs.Length > 100)
            throw Conflict("SLUG_HISTORY_LIMIT", "Slug history is full; keep the current slug.");
        await ReserveSlugAsync(product.Slug, id, ct);
        // Bind by operation, not product ID: a losing edit cannot release a later edit's references.
        product.CategoryBindingId = Guid.NewGuid();
        await BindCategoriesAsync(product.CategoryIds, product.CategoryBindingId, ct);
        AddAudit(product.AuditIntents, Intent(actor, "product.updated", revision, revision + 1));
        product.Revision++;
        product.UpdatedAt = Now();
        var result = await Products.ReplaceOneAsync(p => p.Id == id && p.Revision == revision && !p.Deleted, product, cancellationToken: ct);
        if (result.ModifiedCount != 1)
        {
            await ReleaseCategoriesAsync(product.CategoryIds, product.CategoryBindingId, ct);
            throw Stale();
        }
        await ReleaseCategoriesAsync(oldCategories, oldBinding, ct);
        await RefreshSlugKindsAsync(product, ct);
        return ToResponse(product);
    }

    public async Task<ProductResponse> TransitionAsync(Guid id, long revision, ProductTransitionRequest request, Guid actor, CancellationToken ct)
    {
        var product = await ProductAsync(id, ct);
        CheckRevision(product.Revision, revision);
        var reason = request.Reason is null ? null : PublishingValidation.Text(request.Reason, "reason", 500);
        if (request.To == ProductState.Published)
        {
            if (product.State is not (ProductState.Draft or ProductState.Unpublished))
                throw Conflict("INVALID_TRANSITION", "Only draft or unpublished products can be published.");
            if (product.Title.Length == 0 || product.Summary.Length == 0
                || !PublishingValidation.HasDescription(product.DescriptionHtml) || product.License.Text.Length == 0)
                throw new PublishingException(422, "PUBLICATION_INCOMPLETE", "Title, summary, description and approved license are required.");
            PublishingValidation.ValidatePricing(product.Pricing);
            // EX-09 must supply validated attachment and storage/backup admission before this gate is removed.
            throw new PublishingException(422, "ASSET_PUBLICATION_UNAVAILABLE",
                "Publication is blocked until validated asset publication is implemented in EX-09.");
        }
        var allowed = request.To switch
        {
            ProductState.Unpublished => product.State == ProductState.Published,
            ProductState.Archived => product.State is ProductState.Draft or ProductState.Published or ProductState.Unpublished,
            _ => false
        };
        if (!allowed) throw Conflict("INVALID_TRANSITION", "The requested lifecycle transition is not allowed.");
        product.State = request.To;
        product.UpdatedAt = Now();
        product.Revision++;
        AddAudit(product.AuditIntents, Intent(actor, "product." + request.To.ToString().ToLowerInvariant(), revision, revision + 1, reason));
        var changed = await Products.ReplaceOneAsync(p => p.Id == id && p.Revision == revision && !p.Deleted, product, cancellationToken: ct);
        if (changed.ModifiedCount != 1) throw Stale();
        return ToResponse(product);
    }

    public async Task DeleteProductAsync(Guid id, long revision, Guid actor, CancellationToken ct)
    {
        var product = await ProductAsync(id, ct);
        CheckRevision(product.Revision, revision);
        var orderFilter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Eq("productSnapshot.id", new BsonBinaryData(id, GuidRepresentation.Standard)),
            Builders<BsonDocument>.Filter.Eq("relatedProductId", new BsonBinaryData(id, GuidRepresentation.Standard)));
        if (product.State != ProductState.Draft || product.AssetIds.Length > 0 || product.CoverAssetId.HasValue
            || await database.GetCollection<BsonDocument>("orders").Find(orderFilter).AnyAsync(ct)
            || await database.GetCollection<BsonDocument>("assets").Find(new BsonDocument("productId", new BsonBinaryData(id, GuidRepresentation.Standard))).AnyAsync(ct))
            throw Conflict("PRODUCT_REFERENCED", "Only an unreferenced draft may be deleted. Archive this product instead.");
        product.Deleted = true;
        var categories = product.CategoryIds;
        var binding = product.CategoryBindingId;
        product.Title = "";
        product.Summary = "";
        product.DescriptionHtml = "";
        product.CategoryIds = [];
        product.CategoryBindingId = Guid.Empty;
        product.Tags = [];
        product.AssetIds = [];
        product.CoverAssetId = null;
        product.License = new(1, "");
        product.Seo = new();
        product.Pricing = new(PricingMode.Free);
        product.Revision++;
        product.UpdatedAt = Now();
        AddAudit(product.AuditIntents, Intent(actor, "product.deleted", revision, revision + 1));
        var result = await Products.ReplaceOneAsync(p => p.Id == id && p.Revision == revision && !p.Deleted, product, cancellationToken: ct);
        if (result.ModifiedCount != 1) throw Stale();
        await ReleaseCategoriesAsync(categories, binding, ct);
    }

    public async Task<CategoryResponse> CreateCategoryAsync(Guid id, CategoryWriteRequest request, Guid actor, CancellationToken ct)
    {
        var category = new CategoryDocument
        {
            Id = id, Name = PublishingValidation.Text(request.Name, "name", 80, true),
            Slug = PublishingValidation.Slug(request.Slug ?? "", 100), CreatedAt = Now(), UpdatedAt = Now(),
            CreateHash = Hash(request, actor)
        };
        var existing = await Categories.Find(c => c.Id == id).FirstOrDefaultAsync(ct);
        if (existing is not null) return ReplayCategory(existing, category.CreateHash);
        category.AuditIntents.Add(Intent(actor, "category.created", 0, 1));
        try { await Categories.InsertOneAsync(category, cancellationToken: ct); }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            existing = await Categories.Find(c => c.Id == id).FirstOrDefaultAsync(ct);
            if (existing is not null) return ReplayCategory(existing, category.CreateHash);
            throw Conflict("SLUG_CONFLICT", "Category slug is already in use.");
        }
        return ToResponse(category);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(Guid id, long revision, CategoryWriteRequest request, Guid actor, CancellationToken ct)
    {
        var category = await CategoryAsync(id, ct);
        CheckRevision(category.Revision, revision);
        var name = request.Name is null ? category.Name : PublishingValidation.Text(request.Name, "name", 80, true);
        var slug = request.Slug is null ? category.Slug : PublishingValidation.Slug(request.Slug, 100);
        CheckAuditCapacity(category.AuditIntents);
        var update = Builders<CategoryDocument>.Update.Set(c => c.Name, name).Set(c => c.Slug, slug)
            .Inc(c => c.Revision, 1).Set(c => c.UpdatedAt, Now())
            .Push(c => c.AuditIntents, Intent(actor, "category.updated", revision, revision + 1));
        try
        {
            var updated = await Categories.FindOneAndUpdateAsync(c => c.Id == id && c.Revision == revision && !c.Deleted,
                update, new FindOneAndUpdateOptions<CategoryDocument> { ReturnDocument = ReturnDocument.After }, ct);
            return updated is null ? throw Stale() : ToResponse(updated);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        { throw Conflict("SLUG_CONFLICT", "Category slug is already in use."); }
    }

    public async Task ReconcileOfflineAsync(CancellationToken ct)
    {
        // Requires all API writers stopped. Never reclaim permanent slug reservations.
        using var categories = await Categories.FindAsync(FilterDefinition<CategoryDocument>.Empty, cancellationToken: ct);
        while (await categories.MoveNextAsync(ct))
            foreach (var category in categories.Current)
                foreach (var binding in category.ProductBindings)
                {
                    var referenced = await Products.Find(p => !p.Deleted && p.CategoryBindingId == binding
                        && p.CategoryIds.Contains(category.Id)).AnyAsync(ct);
                    if (!referenced)
                        await ReleaseCategoriesAsync([category.Id], binding, ct);
                }
        using var products = await Products.FindAsync(FilterDefinition<ProductDocument>.Empty, cancellationToken: ct);
        while (await products.MoveNextAsync(ct))
            foreach (var product in products.Current)
            {
                await ReserveSlugAsync(product.Slug, product.Id, ct);
                foreach (var slug in product.RedirectSlugs) await ReserveSlugAsync(slug, product.Id, ct);
                await RefreshSlugKindsAsync(product, ct);
            }
        await ProjectAuditAsync(ct);
    }

    public async Task DeleteCategoryAsync(Guid id, long revision, Guid actor, CancellationToken ct)
    {
        var category = await CategoryAsync(id, ct);
        CheckRevision(category.Revision, revision);
        if (category.ProductBindings.Count > 0 || await Products.Find(p => !p.Deleted && p.CategoryIds.Contains(id)).AnyAsync(ct))
            throw Conflict("CATEGORY_REFERENCED", "Reassign products before deleting this category.");
        CheckAuditCapacity(category.AuditIntents);
        var result = await Categories.UpdateOneAsync(c => c.Id == id && c.Revision == revision && !c.Deleted && c.ProductBindings.Count == 0,
            Builders<CategoryDocument>.Update.Set(c => c.Deleted, true).Inc(c => c.Revision, 1)
                .Set(c => c.UpdatedAt, Now()).Push(c => c.AuditIntents, Intent(actor, "category.deleted", revision, revision + 1)),
            cancellationToken: ct);
        if (result.ModifiedCount != 1) throw Conflict("CATEGORY_CHANGED", "Category changed or became referenced. Reload before deleting.");
    }

    private async Task BindCategoriesAsync(Guid[] ids, Guid binding, CancellationToken ct)
    {
        foreach (var id in ids)
        {
            var updated = await Categories.UpdateOneAsync(c => c.Id == id && !c.Deleted && c.ProductBindings.Count < 10_000,
                Builders<CategoryDocument>.Update.AddToSet(c => c.ProductBindings, binding), cancellationToken: ct);
            if (updated.MatchedCount == 0)
            {
                await ReleaseCategoriesAsync(ids, binding, ct);
                throw Conflict("CATEGORY_UNAVAILABLE", "A category is missing, deleted or at its reference limit.");
            }
        }
    }

    private async Task ReleaseCategoriesAsync(Guid[] ids, Guid binding, CancellationToken ct)
    {
        if (ids.Length == 0 || binding == Guid.Empty) return;
        await Categories.UpdateManyAsync(Builders<CategoryDocument>.Filter.In(c => c.Id, ids),
            Builders<CategoryDocument>.Update.Pull(c => c.ProductBindings, binding), cancellationToken: ct);
    }

    private async Task ReserveSlugAsync(string slug, Guid productId, CancellationToken ct)
    {
        var reservation = await Slugs.Find(s => s.Slug == slug).FirstOrDefaultAsync(ct);
        if (reservation is not null)
        {
            if (reservation.ProductId != productId) throw Conflict("SLUG_CONFLICT", "Product slug is permanently reserved.");
            return;
        }
        try { await Slugs.InsertOneAsync(new() { Slug = slug, ProductId = productId }, cancellationToken: ct); }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            reservation = await Slugs.Find(s => s.Slug == slug).FirstOrDefaultAsync(ct);
            if (reservation?.ProductId != productId) throw Conflict("SLUG_CONFLICT", "Product slug is permanently reserved.");
        }
    }

    private async Task RefreshSlugKindsAsync(ProductDocument product, CancellationToken ct)
    {
        // Registry kind is a projection. Future public lookup must resolve by productId and current product.slug.
        await Slugs.UpdateManyAsync(s => s.ProductId == product.Id && s.Slug != product.Slug,
            Builders<SlugReservation>.Update.Set(s => s.Kind, "Redirect"), cancellationToken: ct);
        await Slugs.UpdateOneAsync(s => s.ProductId == product.Id && s.Slug == product.Slug,
            Builders<SlugReservation>.Update.Set(s => s.Kind, "Current"), cancellationToken: ct);
    }

    private async Task ValidateAssetsAsync(ProductDocument product, CancellationToken ct)
    {
        var ids = product.AssetIds.Concat(product.CoverAssetId.HasValue ? [product.CoverAssetId.Value] : []).Distinct();
        foreach (var id in ids)
        {
            var asset = await database.GetCollection<BsonDocument>("assets")
                .Find(new BsonDocument("_id", new BsonBinaryData(id, GuidRepresentation.Standard))).FirstOrDefaultAsync(ct);
            var expectedKind = product.CoverAssetId == id ? "Cover" : "Download";
            if (asset is null || !asset.TryGetValue("kind", out var kind) || kind != expectedKind
                || (asset.TryGetValue("productId", out var owner) && !owner.IsBsonNull && owner != new BsonBinaryData(product.Id, GuidRepresentation.Standard)))
                throw PublishingValidation.Invalid("assetIds", "Asset is missing, has an incompatible kind or belongs to another product.");
        }
    }

    public async Task ProjectAuditAsync(CancellationToken ct)
    {
        using var products = await Products.FindAsync(p => p.AuditIntents.Count > 0, cancellationToken: ct);
        while (await products.MoveNextAsync(ct))
            foreach (var product in products.Current)
            {
                await ProjectAsync(product.Id, "product", product.AuditIntents, ct);
                var ids = product.AuditIntents.Select(i => i.Id).ToArray();
                await Products.UpdateOneAsync(p => p.Id == product.Id,
                    Builders<ProductDocument>.Update.PullFilter(p => p.AuditIntents,
                        Builders<PublishingAuditIntent>.Filter.In(i => i.Id, ids)), cancellationToken: ct);
            }
        using var categories = await Categories.FindAsync(c => c.AuditIntents.Count > 0, cancellationToken: ct);
        while (await categories.MoveNextAsync(ct))
            foreach (var category in categories.Current)
            {
                await ProjectAsync(category.Id, "category", category.AuditIntents, ct);
                var ids = category.AuditIntents.Select(i => i.Id).ToArray();
                await Categories.UpdateOneAsync(c => c.Id == category.Id,
                    Builders<CategoryDocument>.Update.PullFilter(c => c.AuditIntents,
                        Builders<PublishingAuditIntent>.Filter.In(i => i.Id, ids)), cancellationToken: ct);
            }
    }

    private async Task ProjectAsync(Guid entityId, string entityType, IEnumerable<PublishingAuditIntent> intents, CancellationToken ct)
    {
        foreach (var intent in intents)
        {
            var document = new BsonDocument
            {
                ["_id"] = new BsonBinaryData(intent.Id, GuidRepresentation.Standard),
                ["actorId"] = new BsonBinaryData(intent.ActorId, GuidRepresentation.Standard),
                ["entityId"] = new BsonBinaryData(entityId, GuidRepresentation.Standard),
                ["entityType"] = entityType, ["action"] = intent.Action, ["at"] = intent.At,
                ["changeSummary"] = new BsonDocument { ["fromRevision"] = intent.FromRevision, ["toRevision"] = intent.ToRevision }
            };
            if (intent.Reason is not null) document["reason"] = intent.Reason;
            await database.GetCollection<BsonDocument>("auditEvents").UpdateOneAsync(
                new BsonDocument("_id", document["_id"]), new BsonDocument("$setOnInsert", document),
                new UpdateOptions { IsUpsert = true }, ct);
        }
    }

    private async Task<ProductDocument> ProductAsync(Guid id, CancellationToken ct) =>
        await Products.Find(p => p.Id == id && !p.Deleted).FirstOrDefaultAsync(ct)
            ?? throw new PublishingException(404, "NOT_FOUND", "Product not found.");
    private async Task<CategoryDocument> CategoryAsync(Guid id, CancellationToken ct) =>
        await Categories.Find(c => c.Id == id && !c.Deleted).FirstOrDefaultAsync(ct)
            ?? throw new PublishingException(404, "NOT_FOUND", "Category not found.");
    private static ProductResponse ReplayProduct(ProductDocument product, string hash) =>
        product.CreateHash == hash && !product.Deleted ? ToResponse(product) : throw Conflict("IDEMPOTENCY_CONFLICT", "Request key was already used.");
    private static CategoryResponse ReplayCategory(CategoryDocument category, string hash) =>
        category.CreateHash == hash && !category.Deleted ? ToResponse(category) : throw Conflict("IDEMPOTENCY_CONFLICT", "Request key was already used.");
    private static string Hash<T>(T request, Guid actor) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(actor + ":" + JsonSerializer.Serialize(request))));
    private DateTime Now() => DateTimeOffset.FromUnixTimeMilliseconds(clock.GetUtcNow().ToUnixTimeMilliseconds()).UtcDateTime;
    private PublishingAuditIntent Intent(Guid actor, string action, long from, long to, string? reason = null) =>
        new() { ActorId = actor, Action = action, At = Now(), FromRevision = from, ToRevision = to, Reason = reason };
    private static void CheckRevision(long actual, long expected) { if (actual != expected) throw Stale(); }
    private static PublishingException Stale() => new(412, "STALE_REVISION", "The resource changed. Reload before saving.");
    private static PublishingException Conflict(string code, string message) => new(409, code, message);
    private static void CheckAuditCapacity(List<PublishingAuditIntent> intents)
    {
        if (intents.Count >= AuditLimit) throw Conflict("AUDIT_PROJECTION_REQUIRED", "Project pending publishing audit events before making further changes.");
    }
    private static void AddAudit(List<PublishingAuditIntent> intents, PublishingAuditIntent intent)
    { CheckAuditCapacity(intents); intents.Add(intent); }
    private static void ValidateLimit(int limit)
    { if (limit is < 1 or > 50) throw PublishingValidation.Invalid("limit", "Limit must be 1 to 50."); }
    private static string EncodeCursor(DateTime at, Guid id) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{new DateTimeOffset(at).ToUnixTimeMilliseconds()}:{id:D}"));
    private static (DateTime At, Guid Id) DecodeCursor(string cursor)
    {
        if (cursor.Length > 160) throw PublishingValidation.Invalid("cursor", "Invalid cursor.");
        try
        {
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(cursor)).Split(':');
            if (parts.Length == 2 && long.TryParse(parts[0], out var milliseconds) && Guid.TryParse(parts[1], out var id))
                return (DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime, id);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentOutOfRangeException) { }
        throw PublishingValidation.Invalid("cursor", "Invalid cursor.");
    }
    private static ProductResponse ToResponse(ProductDocument p) => new(p.Id, p.Title, p.Slug, p.Summary, p.DescriptionHtml,
        p.State, p.CategoryIds, p.Tags, p.CoverAssetId, p.AssetIds,
        new(p.Pricing.Mode, p.Pricing.Currency, p.Pricing.FixedPaisa, p.Pricing.MinimumPaisa, p.Pricing.SuggestedPaisa,
            p.Pricing.AllowZero, p.Pricing.ContributionEnabled, p.PricingVersion),
        p.License, p.Seo, p.RedirectSlugs, p.Revision, new(p.CreatedAt), new(p.UpdatedAt),
        p.PublishedAt.HasValue ? new DateTimeOffset(p.PublishedAt.Value) : null);
    private static CategoryResponse ToResponse(CategoryDocument c) => new(c.Id, c.Name, c.Slug, c.Revision, new(c.CreatedAt), new(c.UpdatedAt));
}
