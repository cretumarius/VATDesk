using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using VatDesk.Api.Dtos;
using VatDesk.Infrastructure.Persistence;

namespace VatDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(VatDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HealthResponse>> Get(CancellationToken cancellationToken)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";

        bool databaseConnected;
        try
        {
            databaseConnected = await dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            databaseConnected = false;
        }

        return Ok(new HealthResponse(version, databaseConnected));
    }
}
