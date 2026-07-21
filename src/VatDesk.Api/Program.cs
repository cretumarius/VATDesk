using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
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

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
