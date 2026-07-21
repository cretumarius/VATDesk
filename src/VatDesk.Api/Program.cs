using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using VatDesk.Api.Auth;
using VatDesk.Api.ErrorHandling;
using VatDesk.Core.Abstractions;
using VatDesk.Infrastructure;
using VatDesk.Infrastructure.Countries.Hu;
using VatDesk.Infrastructure.Parsing;
using VatDesk.Infrastructure.Pdf;
using VatDesk.Infrastructure.Persistence;
using VatDesk.Infrastructure.Persistence.Repositories;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Global safety net (security checklist item 1): caps every request, not just the ones
// that remember to add [RequestSizeLimit]. Upload also sets that attribute explicitly —
// belt and suspenders, not a substitute for this.
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = ParserFactory.MaxFileSizeBytes);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = ParserFactory.MaxFileSizeBytes);

builder.Services.AddDbContext<VatDeskDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IInvoiceParser, CsvInvoiceParser>();
builder.Services.AddSingleton<IInvoiceParser, NavXmlInvoiceParser>();
builder.Services.AddSingleton<ParserFactory>();
builder.Services.AddSingleton<IReportRenderer, QuestPdfReportRenderer>();

// v1 hard-defaults to "HU"; endpoints resolve it via [FromKeyedServices("HU")], and the
// GET /api/countries/{cc}/vat-categories endpoint resolves by the route's country code key.
builder.Services.AddCountry<HungarianVatCategoryRegistry, HungarianVatDeclarationStrategy>("HU");

builder.Services.AddScoped<DeclarationRepository>();
builder.Services.AddScoped<UserRepository>();

// --- Auth: JWT bearer + role policies ---

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtKey = builder.Configuration[$"{JwtOptions.SectionName}:Key"];
if (string.IsNullOrEmpty(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    // Fail fast: never start with a missing or under-strength signing key. Set via the
    // Jwt__Key env var (docker-compose / Azure App Settings) or dotnet user-secrets locally.
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 bytes. Set the Jwt__Key environment variable " +
        "(or dotnet user-secrets for local dev) to a signing key of at least 32 bytes.");
}

var jwtIssuer = builder.Configuration[$"{JwtOptions.SectionName}:Issuer"] ?? "VatDesk";
var jwtAudience = builder.Configuration[$"{JwtOptions.SectionName}:Audience"] ?? "VatDesk";

builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

builder.Services.AddAuthorization();

// Tight window on login specifically (security checklist item 6); partitioned by client IP
// so one attacker can't lock out every other user sharing the policy.
builder.Services.AddRateLimiter(options =>
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<VatDeskDbContext>();

    // IsRelational() guard lets integration tests swap in the EF Core InMemory provider
    // (which doesn't support migrations) without touching this startup path.
    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
}

app.UseExceptionHandler();

// Turns bare 401/403/404 responses from auth/authorization middleware into ProblemDetails
// bodies via the IProblemDetailsService registered above.
app.UseStatusCodePages();

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
