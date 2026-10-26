using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Ganss.Xss;

namespace ContentManager.Api.Features.Publishing;

public static partial class PublishingValidation
{
    public static ProductDocument Apply(ProductDocument product, ProductWriteRequest request, bool create)
    {
        if (create && (request.Title is null || request.Slug is null))
            throw Invalid("title", "Title and slug are required.");
        if (request.Title is not null) product.Title = Text(request.Title, "title", 160, true);
        if (request.Slug is not null) product.Slug = Slug(request.Slug, 120);
        if (request.Summary is not null) product.Summary = Text(request.Summary, "summary", 500);
        if (request.DescriptionHtml is not null)
        {
            if (Encoding.UTF8.GetByteCount(request.DescriptionHtml) > 50_000)
                throw Invalid("descriptionHtml", "Description must not exceed 50,000 UTF-8 bytes.");
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedTags.UnionWith(["p", "br", "strong", "em", "ul", "ol", "li", "blockquote", "h2", "h3", "a", "code", "pre"]);
            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedAttributes.UnionWith(["href", "title"]);
            sanitizer.AllowedSchemes.Clear();
            sanitizer.AllowedSchemes.UnionWith(["https", "http"]);
            sanitizer.AllowedCssProperties.Clear();
            product.DescriptionHtml = sanitizer.Sanitize(request.DescriptionHtml);
            if (Encoding.UTF8.GetByteCount(product.DescriptionHtml) > 50_000)
                throw Invalid("descriptionHtml", "Sanitized description must not exceed 50,000 UTF-8 bytes.");
        }
        if (request.CategoryIds is not null)
        {
            if (request.CategoryIds.Length > 3 || request.CategoryIds.Contains(Guid.Empty))
                throw Invalid("categoryIds", "Select at most three nonempty category IDs.");
            product.CategoryIds = request.CategoryIds.Distinct().ToArray();
        }
        if (request.Tags is not null)
        {
            if (request.Tags.Length > 10 || request.Tags.Any(tag => tag is null))
                throw Invalid("tags", "Supply at most ten text tags.");
            product.Tags = request.Tags.Select(tag => Text(tag, "tags", 40, true).ToLowerInvariant())
                .Distinct(StringComparer.Ordinal).ToArray();
        }
        if (request.CoverAssetId.HasValue)
            product.CoverAssetId = request.CoverAssetId == Guid.Empty ? null : request.CoverAssetId;
        if (request.AssetIds is not null)
        {
            if (request.AssetIds.Length > 5 || request.AssetIds.Contains(Guid.Empty))
                throw Invalid("assetIds", "Select at most five nonempty asset IDs.");
            product.AssetIds = request.AssetIds.Distinct().ToArray();
        }
        if (product.CoverAssetId.HasValue && product.AssetIds.Contains(product.CoverAssetId.Value))
            throw Invalid("assetIds", "A cover cannot also be a download attachment.");
        if (request.Pricing is not null)
        {
            ValidatePricing(request.Pricing);
            if (product.Pricing != request.Pricing)
            {
                product.Pricing = request.Pricing;
                if (!create) product.PricingVersion = checked(product.PricingVersion + 1);
            }
        }
        if (request.License is not null)
        {
            var license = request.License with { Text = Text(request.License.Text, "license.text", 10_000) };
            if (license.Version < 1) throw Invalid("license.version", "License version must be positive.");
            if (!create && (license.Version < product.License.Version
                || (license.Text != product.License.Text && license.Version <= product.License.Version)))
                throw Invalid("license.version", "Changed license text requires a higher version.");
            product.License = license;
        }
        if (request.Seo is not null)
            product.Seo = new(Text(request.Seo.Title ?? "", "seo.title", 160),
                Text(request.Seo.Description ?? "", "seo.description", 500));
        return product;
    }

    public static void ValidatePricing(PricingInput price)
    {
        const long maximum = 10_000_000;
        var valid = Enum.IsDefined(price.Mode) && price.Currency == "NPR"
            && new[] { price.FixedPaisa, price.MinimumPaisa, price.SuggestedPaisa }
                .All(value => !value.HasValue || value is >= 100 and <= maximum);
        valid &= price.Mode switch
        {
            PricingMode.Free => price.AllowZero && price.FixedPaisa is null && price.MinimumPaisa is null && price.SuggestedPaisa is null,
            PricingMode.OptionalDonation => price.AllowZero && price.ContributionEnabled && price.FixedPaisa is null
                && price.MinimumPaisa is null && price.SuggestedPaisa is null,
            PricingMode.Fixed or PricingMode.FixedPlusDonation => !price.AllowZero && !price.ContributionEnabled
                && price.FixedPaisa.HasValue && price.MinimumPaisa is null && price.SuggestedPaisa is null,
            PricingMode.PWYW => !price.ContributionEnabled && price.FixedPaisa is null && price.MinimumPaisa.HasValue
                && (!price.SuggestedPaisa.HasValue || price.SuggestedPaisa >= price.MinimumPaisa),
            PricingMode.Suggested => !price.ContributionEnabled && price.FixedPaisa is null && price.MinimumPaisa.HasValue
                && price.SuggestedPaisa.HasValue && price.SuggestedPaisa >= price.MinimumPaisa,
            _ => false
        };
        if (!valid) throw new PublishingException(422, "INVALID_PRICING", "Invalid NPR pricing configuration.",
            new() { ["pricing"] = ["Use mode-appropriate integer paisa between 100 and 10,000,000; zero access uses allowZero."] });
    }

    public static string Slug(string value, int maximum)
    {
        var normalized = Text(value, "slug", maximum, true).ToLowerInvariant();
        if (!SlugPattern().IsMatch(normalized)) throw Invalid("slug", "Use lowercase ASCII letters, digits and single hyphens.");
        return normalized;
    }

    public static string Text(string? value, string field, int maximum, bool required = false)
    {
        if (value is null) throw Invalid(field, "Text cannot be null.");
        var text = value.Trim();
        if (text.Length > maximum || (required && text.Length == 0) || text.Any(c => char.IsControl(c) && c is not '\n' and not '\r' and not '\t'))
            throw Invalid(field, $"Text must be {(required ? "1" : "0")} to {maximum} characters with no unsupported control characters.");
        return text;
    }

    public static bool HasDescription(string html) =>
        !string.IsNullOrWhiteSpace(new HtmlParser().ParseDocument(html).Body?.TextContent);

    public static PublishingException Invalid(string field, string message) =>
        new(400, "INVALID_FIELD", "One or more authoring fields are invalid.", new() { [field] = [message] });

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
