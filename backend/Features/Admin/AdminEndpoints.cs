using System.Threading.RateLimiting;
using ContentManager.Api.Infrastructure;
using Microsoft.Extensions.Options;

namespace ContentManager.Api.Features.Admin;

public static class AdminEndpoints
{
    public const string AdminCookie = "__Host-admin";
    private const string CsrfCookie = "__Host-csrf";
    private const string CsrfHeader = "X-CSRF-Token";
    private const string IdentityKey = "admin.identity";
    private const int MaxFieldLength = 256;

    public static IServiceCollection AddAdminAuthentication(this IServiceCollection services)
    {
        AdminAuthService.RegisterConventions();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<AdminCrypto>();
        services.AddSingleton<AdminAuthService>();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
        });
        return services;
    }

    public static void MapAdminEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/auth/csrf", (HttpContext context, AdminCrypto crypto) =>
        {
            var binding = Binding(context);
            if (binding is null)
            {
                binding = AdminCrypto.NewToken();
                context.Response.Cookies.Append(CsrfCookie, binding, CookieSettings(TimeSpan.FromMinutes(30)));
            }
            return Results.Ok(new { csrfToken = crypto.Hash("csrf", binding) });
        }).RequireRateLimiting("auth");

        var admin = app.MapGroup("/api/v1/admin");
        admin.MapPost("/sessions", async (
            LoginRequest request, HttpContext context, AdminAuthService auth) =>
        {
            if (!HasText(request.Login) || !HasText(request.Password) || !HasText(request.Code))
                return Results.StatusCode(StatusCodes.Status401Unauthorized);

            var result = await auth.LoginAsync(request.Login!, request.Password!, request.Code!);
            if (result.Outcome == LoginOutcome.Throttled)
            {
                context.Response.Headers.RetryAfter = Math.Ceiling(result.RetryAfter!.Value.TotalSeconds).ToString("0");
                return Results.StatusCode(StatusCodes.Status429TooManyRequests);
            }
            if (result.Outcome != LoginOutcome.Success)
                return Results.StatusCode(StatusCodes.Status401Unauthorized);

            context.Response.Cookies.Delete(CsrfCookie, CookieSettings(null));
            context.Response.Cookies.Append(AdminCookie, result.Token!, CookieSettings(null));
            return Results.Ok(new { expiresAt = result.ExpiresAt });
        }).AddEndpointFilter(RequireOriginAndCsrf).RequireRateLimiting("auth");

        var protectedAdmin = admin.MapGroup("").AddEndpointFilter(RequireAdmin);
        protectedAdmin.MapGet("/sessions/current", (HttpContext context) =>
        {
            var identity = (AdminIdentity)context.Items[IdentityKey]!;
            return Results.Ok(new { login = identity.Login, expiresAt = identity.ExpiresAt });
        });
        protectedAdmin.MapDelete("/sessions/current", async (HttpContext context, AdminAuthService auth) =>
        {
            await auth.RevokeAsync(context.Request.Cookies[AdminCookie]!);
            context.Response.Cookies.Delete(AdminCookie, CookieSettings(null));
            return Results.NoContent();
        });
    }

    private static async ValueTask<object?> RequireAdmin(EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
    {
        var context = invocation.HttpContext;
        var identity = await context.RequestServices.GetRequiredService<AdminAuthService>()
            .AuthenticateAsync(context.Request.Cookies[AdminCookie]);
        if (identity is null)
        {
            context.Response.Cookies.Delete(AdminCookie, CookieSettings(null));
            return Results.StatusCode(StatusCodes.Status401Unauthorized);
        }

        context.Items[IdentityKey] = identity;
        return await RequireOriginAndCsrf(invocation, next);
    }

    private static async ValueTask<object?> RequireOriginAndCsrf(EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
    {
        var context = invocation.HttpContext;
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)
            || HttpMethods.IsOptions(context.Request.Method))
            return await next(invocation);

        var publicOrigin = context.RequestServices.GetRequiredService<IOptions<PlatformOptions>>().Value.Security.PublicOrigin;
        var binding = Binding(context);
        var token = context.Request.Headers[CsrfHeader].ToString();
        var expected = binding is null ? null : context.RequestServices.GetRequiredService<AdminCrypto>().Hash("csrf", binding);
        var originValid = string.Equals(context.Request.Headers.Origin.ToString(),
            new Uri(publicOrigin).GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase);
        if (!originValid || expected is null || token.Length == 0 || !AdminCrypto.Equal(token, expected))
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        return await next(invocation);
    }

    private static string? Binding(HttpContext context)
    {
        var value = context.Request.Cookies[AdminCookie] ?? context.Request.Cookies[CsrfCookie];
        return string.IsNullOrEmpty(value) || value.Length > 128 ? null : value;
    }

    private static bool HasText(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= MaxFieldLength;

    private static CookieOptions CookieSettings(TimeSpan? maxAge) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        IsEssential = true,
        MaxAge = maxAge
    };
}
