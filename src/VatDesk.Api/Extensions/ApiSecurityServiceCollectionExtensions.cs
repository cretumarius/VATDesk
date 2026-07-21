using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using VatDesk.Api.Auth;

namespace VatDesk.Api.Extensions;

/// <summary>CORS and rate limiting — the two cross-cutting API security concerns that aren't authn/authz.</summary>
public static class ApiSecurityServiceCollectionExtensions
{
    public static IServiceCollection AddApiSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Locked to a single configurable origin (security checklist item 7). Moot in the actual
        // prod topology — the API serves the built SPA from its own origin, so no browser request
        // is ever cross-origin — but kept explicit rather than omitted, and never "*". Only
        // relevant if the app is ever split across two origins (e.g. a separately hosted frontend).
        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicies.AppOrigin, policy =>
            {
                var allowedOrigin = configuration["Cors:AllowedOrigin"];
                if (!string.IsNullOrWhiteSpace(allowedOrigin))
                {
                    policy.WithOrigins(allowedOrigin).AllowAnyHeader().AllowAnyMethod();
                }
            });
        });

        // Tight window on login specifically (security checklist item 6); partitioned by client IP
        // so one attacker can't lock out every other user sharing the policy.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimiterPolicies.Login, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 8,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

            // Upload requires authentication, so — unlike login — partition by user id rather
            // than IP: several Admins behind the same corporate NAT shouldn't share one budget,
            // and a leaked/misbehaving Admin token shouldn't be able to hammer the parse/PDF
            // pipeline unbounded.
            options.AddPolicy(RateLimiterPolicies.Upload, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));
        });

        return services;
    }
}
