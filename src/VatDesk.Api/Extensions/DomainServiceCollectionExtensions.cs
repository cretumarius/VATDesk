using VatDesk.Core.Abstractions;
using VatDesk.Infrastructure;
using VatDesk.Infrastructure.Countries.Hu;
using VatDesk.Infrastructure.Parsing;
using VatDesk.Infrastructure.Pdf;

namespace VatDesk.Api.Extensions;

/// <summary>Parsers, PDF rendering, and the per-country registry/strategy registration — the extensibility seam ("how to add a country" in architecture.md). Kept prominently visible here, not buried: v1 hard-defaults to "HU", but this is the one line a new country touches.</summary>
public static class DomainServiceCollectionExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddSingleton<IInvoiceParser, CsvInvoiceParser>();
        services.AddSingleton<IInvoiceParser, NavXmlInvoiceParser>();
        services.AddSingleton<ParserFactory>();
        services.AddSingleton<IReportRenderer, QuestPdfReportRenderer>();

        // v1 hard-defaults to "HU"; endpoints resolve it via [FromKeyedServices("HU")], and the
        // GET /api/countries/{cc}/vat-categories endpoint resolves by the route's country code key.
        services.AddCountry<HungarianVatCategoryRegistry, HungarianVatDeclarationStrategy>("HU");

        return services;
    }
}
