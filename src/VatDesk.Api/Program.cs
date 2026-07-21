using Microsoft.EntityFrameworkCore;
using VatDesk.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<VatDeskDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Country strategies register here per country code once real implementations exist, e.g.:
//   builder.Services.AddCountry<HungarianVatCategoryRegistry, HungarianVatDeclarationStrategy>("HU");
// Next session: HU registry + strategy + parsers land in VatDesk.Infrastructure.

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
