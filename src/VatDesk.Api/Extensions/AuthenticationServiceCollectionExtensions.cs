using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using VatDesk.Api.Auth;

namespace VatDesk.Api.Extensions;

/// <summary>JWT bearer authentication, including the fail-fast signing-key check. That check and the bearer setup share the same jwtKey/jwtIssuer/jwtAudience locals — kept as one method so they can't drift apart.</summary>
public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        var jwtKey = configuration[$"{JwtOptions.SectionName}:Key"];
        if (string.IsNullOrEmpty(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
        {
            // Fail fast: never start with a missing or under-strength signing key. Set via the
            // Jwt__Key env var (docker-compose / Azure App Settings) or dotnet user-secrets locally.
            throw new InvalidOperationException(
                "Jwt:Key is missing or shorter than 32 bytes. Set the Jwt__Key environment variable " +
                "(or dotnet user-secrets for local dev) to a signing key of at least 32 bytes.");
        }

        var jwtIssuer = configuration[$"{JwtOptions.SectionName}:Issuer"] ?? "VatDesk";
        var jwtAudience = configuration[$"{JwtOptions.SectionName}:Audience"] ?? "VatDesk";

        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    // Explicit pin (security checklist #4): only the one algorithm we ever sign
                    // with is accepted for verification — no algorithm-confusion surface, even
                    // though a symmetric-only key already rules out the classic RS256->HS256 attack.
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                };
            });

        return services;
    }
}
