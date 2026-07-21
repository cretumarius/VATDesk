using QuestPDF.Infrastructure;
using VatDesk.Api.Extensions;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---
builder.AddApiInfrastructure();
builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddDomainServices();
builder.Services.AddAuthenticationServices(builder.Configuration);
builder.Services.AddAuthorizationPolicies();
builder.Services.AddApiSecurity(builder.Configuration);

var app = builder.Build();

// --- Startup tasks ---
await app.ApplyMigrationsAsync();

// --- Pipeline ---
app.UseVatDeskPipeline();

app.Run();

public partial class Program;
