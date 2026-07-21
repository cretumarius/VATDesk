namespace VatDesk.Api.Extensions;

/// <summary>Role enforcement is entirely via [Authorize(Roles = "...")] attributes on controllers — no named AuthorizationPolicy objects exist today, so there's nothing to configure beyond the bare service registration. Kept as its own method/file so a future named policy has an obvious home.</summary>
public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization();

        return services;
    }
}
