using Microsoft.EntityFrameworkCore;
using VatDesk.Core.Abstractions;
using VatDesk.Infrastructure;
using VatDesk.Infrastructure.Countries.Hu;
using VatDesk.Infrastructure.Parsing;
using VatDesk.Infrastructure.Persistence;
using VatDesk.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<VatDeskDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IInvoiceParser, CsvInvoiceParser>();
builder.Services.AddSingleton<IInvoiceParser, NavXmlInvoiceParser>();
builder.Services.AddSingleton<ParserFactory>();

// v1 hard-defaults to "HU"; endpoints resolve it via [FromKeyedServices("HU")], and the
// GET /api/countries/{cc}/vat-categories endpoint resolves by the route's country code key.
builder.Services.AddCountry<HungarianVatCategoryRegistry, HungarianVatDeclarationStrategy>("HU");

builder.Services.AddScoped<DeclarationRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<VatDeskDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
