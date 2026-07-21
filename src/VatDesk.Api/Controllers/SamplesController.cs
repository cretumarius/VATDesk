using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VatDesk.Api.Controllers;

/// <summary>
/// Serves the skill's canonical sample files for download. Source of truth stays
/// .claude/skills/hungarian-vat/assets/ — VatDesk.Api.csproj copies them into the
/// build/publish output under samples/ (see the Content items there); never duplicated
/// in git. Any authenticated user (Admin or Viewer) can download samples.
/// </summary>
[ApiController]
[Route("api/samples")]
[Authorize]
public class SamplesController : ControllerBase
{
    private static readonly string SamplesDirectory = Path.Combine(AppContext.BaseDirectory, "samples");

    [HttpGet("clean.csv")]
    public IActionResult GetCleanCsv() => ServeFile("sample-clean.csv", "clean.csv", "text/csv");

    [HttpGet("invalid.csv")]
    public IActionResult GetInvalidCsv() => ServeFile("sample-invalid.csv", "invalid.csv", "text/csv");

    [HttpGet("nav.xml")]
    public IActionResult GetNavXml() => ServeFile("sample-nav.xml", "nav.xml", "application/xml");

    private IActionResult ServeFile(string sourceFileName, string downloadFileName, string contentType)
    {
        var path = Path.Combine(SamplesDirectory, sourceFileName);
        if (!System.IO.File.Exists(path))
        {
            return Problem(title: "Sample file not found", statusCode: StatusCodes.Status404NotFound);
        }

        return PhysicalFile(path, contentType, downloadFileName);
    }
}
