using System.Globalization;
using System.Text.Json;
using ContentManager.Api.Features.Admin;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ContentManager.Api.Features.Publishing;

public static class PublishingEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void MapPublishingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/admin").RequireAdministrator().AddEndpointFilter(HandleErrors);
        var products = group.MapGroup("/products").WithTags("Administrator products");
        products.MapGet("", async (string? state, string? cursor, int? limit, IPublishingService service, CancellationToken ct) =>
            TypedResults.Ok(await service.ListProductsAsync(state, cursor, limit ?? 20, ct)))
            .WithSummary("List private products with bounded pagination");
        products.MapGet("/{id:guid}", async (Guid id, HttpContext context, IPublishingService service, CancellationToken ct) =>
        {
            var response = await service.GetProductAsync(id, ct);
            ETag(context, response.Revision);
            return TypedResults.Ok(response);
        }).WithSummary("Get a private product authoring preview");
        products.MapPost("", async (JsonElement body, HttpContext context, IPublishingService service, CancellationToken ct) =>
        {
            var response = await service.CreateProductAsync(CreateId(context), ProductInput(body), Actor(context), ct);
            ETag(context, response.Revision);
            return TypedResults.Created($"/api/v1/admin/products/{response.Id}", response);
        }).Accepts<ProductWriteRequest>("application/json")
            .WithSummary("Create an idempotent draft").ProducesValidationProblem().ProducesProblem(409).ProducesProblem(422);
        products.MapPatch("/{id:guid}", async (Guid id, JsonElement body, HttpContext context, IPublishingService service, CancellationToken ct) =>
        {
            var response = await service.UpdateProductAsync(id, Revision(context), ProductInput(body), Actor(context), ct);
            ETag(context, response.Revision);
            return TypedResults.Ok(response);
        }).Accepts<ProductWriteRequest>("application/json")
            .WithSummary("Edit supplied product fields with If-Match").ProducesProblem(412).ProducesProblem(428);
        products.MapPost("/{id:guid}/transitions", async (Guid id, ProductTransitionRequest request, HttpContext context,
            IPublishingService service, CancellationToken ct) =>
        {
            var response = await service.TransitionAsync(id, Revision(context), request, Actor(context), ct);
            ETag(context, response.Revision);
            return TypedResults.Ok(response);
        }).WithSummary("Change product lifecycle; publication remains blocked until EX-09").ProducesProblem(409).ProducesProblem(412).ProducesProblem(422);
        products.MapDelete("/{id:guid}", async (Guid id, HttpContext context, IPublishingService service, CancellationToken ct) =>
        {
            await service.DeleteProductAsync(id, Revision(context), Actor(context), ct);
            return TypedResults.NoContent();
        }).WithSummary("Delete only an unreferenced draft, retaining slug and audit tombstone").ProducesProblem(409).ProducesProblem(412);

        var categories = group.MapGroup("/categories").WithTags("Administrator categories");
        categories.MapGet("", async (string? cursor, int? limit, IPublishingService service, CancellationToken ct) =>
            TypedResults.Ok(await service.ListCategoriesAsync(cursor, limit ?? 20, ct)))
            .WithSummary("List administrator categories");
        categories.MapGet("/{id:guid}", async (Guid id, HttpContext context, IPublishingService service, CancellationToken ct) =>
        {
            var response = await service.GetCategoryAsync(id, ct);
            ETag(context, response.Revision);
            return TypedResults.Ok(response);
        }).WithSummary("Get category metadata and revision");
        categories.MapPost("", async (JsonElement body, HttpContext context, IPublishingService service, CancellationToken ct) =>
        {
            var response = await service.CreateCategoryAsync(CreateId(context), CategoryInput(body), Actor(context), ct);
            ETag(context, response.Revision);
            return TypedResults.Created($"/api/v1/admin/categories/{response.Id}", response);
        }).Accepts<CategoryWriteRequest>("application/json").WithSummary("Create an idempotent category").ProducesProblem(409);
        categories.MapPatch("/{id:guid}", async (Guid id, JsonElement body, HttpContext context, IPublishingService service, CancellationToken ct) =>
        {
            var response = await service.UpdateCategoryAsync(id, Revision(context), CategoryInput(body), Actor(context), ct);
            ETag(context, response.Revision);
            return TypedResults.Ok(response);
        }).Accepts<CategoryWriteRequest>("application/json")
            .WithSummary("Edit category fields with If-Match").ProducesProblem(409).ProducesProblem(412);
        categories.MapDelete("/{id:guid}", async (Guid id, HttpContext context, IPublishingService service, CancellationToken ct) =>
        {
            await service.DeleteCategoryAsync(id, Revision(context), Actor(context), ct);
            return TypedResults.NoContent();
        }).WithSummary("Delete only an unreferenced category").ProducesProblem(409).ProducesProblem(412);
    }

    private static async ValueTask<object?> HandleErrors(EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
    {
        try { return await next(invocation); }
        catch (PublishingException exception)
        {
            var context = invocation.HttpContext;
            return TypedResults.Problem(new ProblemDetails
            {
                Status = exception.Status, Title = exception.Message,
                Extensions =
                {
                    ["code"] = exception.Code, ["errors"] = exception.Errors,
                    ["traceId"] = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier
                }
            });
        }
    }

    private static ProductWriteRequest ProductInput(JsonElement body)
    {
        ValidateBody(body);
        ProductWriteRequest request;
        try { request = body.Deserialize<ProductWriteRequest>(JsonOptions)!; }
        catch (JsonException) { throw PublishingValidation.Invalid("body", "Invalid product field or value."); }
        foreach (var field in body.EnumerateObject())
            if (field.Value.ValueKind == JsonValueKind.Null && !field.Name.Equals("coverAssetId", StringComparison.OrdinalIgnoreCase))
                throw PublishingValidation.Invalid(field.Name, "Use an empty value to clear this field, not null.");
        if (body.EnumerateObject().Any(p => p.Name.Equals("coverAssetId", StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.Null))
            request = request with { CoverAssetId = Guid.Empty };
        return request;
    }

    private static CategoryWriteRequest CategoryInput(JsonElement body)
    {
        ValidateBody(body);
        if (body.EnumerateObject().Any(p => p.Value.ValueKind == JsonValueKind.Null))
            throw PublishingValidation.Invalid("body", "Category fields cannot be null.");
        try { return body.Deserialize<CategoryWriteRequest>(JsonOptions)!; }
        catch (JsonException) { throw PublishingValidation.Invalid("body", "Invalid category field or value."); }
    }

    private static void ValidateBody(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object || !body.EnumerateObject().Any())
            throw PublishingValidation.Invalid("body", "Supply a nonempty JSON object.");
        ValidateProperties(body);
    }

    private static void ValidateProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw PublishingValidation.Invalid("body", "Duplicate JSON fields are not allowed.");
                ValidateProperties(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) ValidateProperties(item);
    }

    private static Guid Actor(HttpContext context) => AdminEndpoints.CurrentAdministrator(context).Id;
    private static Guid CreateId(HttpContext context)
    {
        if (!Guid.TryParseExact(context.Request.Headers["Idempotency-Key"], "D", out var id) || id == Guid.Empty)
            throw PublishingValidation.Invalid("Idempotency-Key", "Supply a nonempty UUID request key for creates.");
        return id;
    }
    private static long Revision(HttpContext context)
    {
        var header = context.Request.Headers.IfMatch.ToString();
        if (header.Length == 0) throw new PublishingException(428, "REVISION_REQUIRED", "Supply the current quoted revision in If-Match.");
        if (header.Length < 3 || header[0] != '"' || header[^1] != '"'
            || !long.TryParse(header.AsSpan(1, header.Length - 2), NumberStyles.None, CultureInfo.InvariantCulture, out var revision) || revision < 1)
            throw PublishingValidation.Invalid("If-Match", "Use a single strong quoted revision, such as \"1\".");
        return revision;
    }
    private static void ETag(HttpContext context, long revision) =>
        context.Response.Headers.ETag = $"\"{revision.ToString(CultureInfo.InvariantCulture)}\"";
}
