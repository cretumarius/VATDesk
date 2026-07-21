using Microsoft.AspNetCore.HttpOverrides;
using VatDesk.Api.Auth;
using VatDesk.Api.Middleware;

namespace VatDesk.Api.Extensions;

/// <summary>
/// The middleware pipeline, in one place, in the exact order it must run. This order is a
/// contract, not a style choice — every comment below explains a real dependency on what
/// ran before it. If you need to reorder something, re-derive why the current order is
/// correct first (see docs/PLAN.md's Stage A inventory from the Program.cs decomposition
/// session for the full reasoning), and re-verify security-headers-on-static-files/
/// SPA-fallback and auth-before-rate-limiter still hold.
/// </summary>
public static class PipelineExtensions
{
    public static WebApplication UseVatDeskPipeline(this WebApplication app)
    {
        // Outermost: must wrap every later middleware/endpoint to catch unhandled exceptions.
        app.UseExceptionHandler();

        // Turns bare 401/403/404 responses from auth/authorization middleware into ProblemDetails
        // bodies via the IProblemDetailsService registered above.
        app.UseStatusCodePages();

        // UseHsts() (below) only ever sets the header when Request.IsHttps is true — verified
        // empirically, not assumed. The deployment target (Azure Web App for Containers /
        // Container Apps) terminates TLS at the platform edge and forwards plain HTTP to this
        // container, so without reading X-Forwarded-Proto, IsHttps would always be false and HSTS
        // would be silently dead in the one place it actually matters. Confirmed live: curl with
        // X-Forwarded-Proto: https and a real hostname (not "localhost" — HstsMiddleware skips
        // that specific Host value regardless of scheme) returns the header; without either one,
        // it doesn't. KnownNetworks/KnownProxies are cleared because the specific edge IPs aren't
        // known in advance — an accepted tradeoff for this PaaS deployment model
        // (docs/SECURITY.md): a trusted proxy is assumed to sit in front in any real deployment;
        // direct internet exposure of this container (bypassing Azure's edge) is out of scope for
        // a demo app. The same forwarded X-Forwarded-For also makes the login rate limiter's
        // per-IP partitioning see the real client IP instead of Azure's edge IP for every request.
        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        };
        // The default KnownNetworks/KnownProxies (loopback only) would reject Azure's edge as an
        // untrusted proxy — cleared, not left at their default, since "= { }" on an
        // already-populated collection property adds nothing rather than clearing it.
        forwardedHeadersOptions.KnownNetworks.Clear();
        forwardedHeadersOptions.KnownProxies.Clear();
        app.UseForwardedHeaders(forwardedHeadersOptions);

        if (!app.Environment.IsDevelopment())
        {
            // Standard pattern: HSTS tells browsers to only ever use HTTPS for this host, so it's
            // pointless (and would make local http:// docker-compose testing look broken) in dev.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseSecurityHeaders();

        app.UseCors(CorsPolicies.AppOrigin);

        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Must come before UseRateLimiter: the upload policy partitions by the authenticated
        // user's claim, which only exists on HttpContext.User once UseAuthentication has run. If
        // this were reversed, that partition key would always read as unauthenticated and every
        // request would silently fall back to the IP-based branch instead — not what "per-user"
        // means. Login's policy is IP-based and pre-auth by nature, so it's unaffected either way.
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseRateLimiter();

        app.MapControllers();

        // Must be last: only catches requests no controller route or static file matched, so
        // client-side (React Router) paths fall through to the SPA instead of 404ing.
        app.MapFallbackToFile("index.html");

        return app;
    }
}
