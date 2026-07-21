using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VatDesk.Api.Auth;
using VatDesk.Api.Dtos;
using VatDesk.Core.Abstractions;
using VatDesk.Infrastructure.Parsing;
using VatDesk.Infrastructure.Persistence.Repositories;

namespace VatDesk.Api.Controllers;

// Class-level [Authorize] requires any authenticated user on every action; Upload adds
// Roles = "Admin" on top (both apply — authenticated AND Admin), per architecture.md's
// API table: reads/PDF for any authenticated user, POST for Admin only.
[ApiController]
[Route("api/declarations")]
[Authorize]
public class DeclarationsController(
    ParserFactory parserFactory,
    [FromKeyedServices("HU")] IVatDeclarationStrategy strategy,
    [FromKeyedServices("HU")] IVatCategoryRegistry registry,
    DeclarationRepository repository,
    IReportRenderer reportRenderer) : ControllerBase
{
    private static readonly string[] AllowedExtensions = [".csv", ".xml"];

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [RequestSizeLimit(ParserFactory.MaxFileSizeBytes)]
    [EnableRateLimiting(RateLimiterPolicies.Upload)]
    public async Task<ActionResult<DeclarationDto>> Upload(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Problem(
                title: "Empty file",
                detail: "No file was uploaded, or the uploaded file is empty.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return Problem(
                title: "Unsupported file type",
                detail: "Only .csv and .xml files are accepted.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);

        ParseResult parseResult;
        try
        {
            parseResult = await parserFactory.ParseAsync(buffer, cancellationToken);
        }
        catch (InvoiceParseException ex)
        {
            return Problem(
                title: "File could not be parsed",
                detail: ex.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }

        var summary = strategy.BuildDeclaration(parseResult.Lines, parseResult.Issues, parseResult.Format);
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var entity = await repository.SaveAsync(file.FileName, parseResult.Format, strategy.CountryCode, summary, userId, cancellationToken);

        return Ok(entity.ToDto(registry));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DeclarationListItemDto>>> List(CancellationToken cancellationToken)
    {
        var declarations = await repository.ListAsync(cancellationToken);
        return Ok(declarations.Select(d => d.ToListItemDto()).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeclarationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return Problem(title: "Declaration not found", statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(entity.ToDto(registry));
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return Problem(title: "Declaration not found", statusCode: StatusCodes.Status404NotFound);
        }

        var summary = entity.ToSummary(registry);
        var metadata = new DeclarationMetadata(entity.Id, entity.SourceFilename, entity.CreatedAt);
        var pdfBytes = await reportRenderer.RenderAsync(summary, metadata, cancellationToken);

        return File(pdfBytes, "application/pdf", $"vatdesk-declaration-{entity.Id}.pdf");
    }

    // No CSV/spreadsheet export exists in v1 (security checklist item 10 is N/A). If one
    // is ever added here, every cell must be checked for a leading =, +, -, or @ and
    // prefixed with a ' before writing — formula-injection defense for anyone who opens
    // the export in Excel/Sheets. Don't skip this just because it "worked" without it.
}
