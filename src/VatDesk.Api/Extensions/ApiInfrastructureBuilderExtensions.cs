using Microsoft.AspNetCore.Http.Features;
using VatDesk.Api.ErrorHandling;
using VatDesk.Infrastructure.Parsing;

namespace VatDesk.Api.Extensions;

/// <summary>
/// Controllers, ProblemDetails, and the request-size limits — grouped because the two size
/// limits (Kestrel + multipart) are one concern split across two config surfaces
/// (WebHost vs. Services), so this takes the whole WebApplicationBuilder rather than just
/// IServiceCollection like the other Add* extensions.
/// </summary>
public static class ApiInfrastructureBuilderExtensions
{
    public static WebApplicationBuilder AddApiInfrastructure(this WebApplicationBuilder builder)
    {
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

        return builder;
    }
}
