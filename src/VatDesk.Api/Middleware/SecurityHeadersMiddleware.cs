namespace VatDesk.Api.Middleware;

/// <summary>
/// Adds the headers security checklist item 8 asks for to every response — API JSON,
/// static files, and the SPA fallback alike, since this runs before UseStaticFiles.
/// HSTS is handled separately via the framework's own UseHsts() (prod-only, standard
/// pattern); this covers the three that ASP.NET Core has no built-in middleware for.
/// </summary>
public static class SecurityHeadersMiddleware
{
    // Deliberately scoped to what this app actually loads, verified against the built
    // frontend (frontend/dist/index.html) rather than guessed:
    //   - script-src 'self' only: the one inline script the Vite build used to emit (the
    //     pre-paint theme-flash-prevention snippet) was moved to public/theme-init.js so
    //     it's served from 'self' — no 'unsafe-inline' needed for scripts.
    //   - style-src needs 'unsafe-inline': Radix UI (dropdown menu, tooltip) sets inline
    //     `style` attributes at runtime for floating-element positioning; blocking that
    //     wouldn't break the app outright but would silently mis-position those elements.
    //   - font-src/style-src also allow Google Fonts' two hosts — the only cross-origin
    //     resource the app actually loads (IBM Plex Sans/Mono).
    //   - connect-src 'self': every fetch() call goes to a relative /api/... path.
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers.ContentSecurityPolicy = ContentSecurityPolicy;

            await next();
        });
}
