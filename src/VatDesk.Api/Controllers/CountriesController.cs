using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VatDesk.Api.Dtos;
using VatDesk.Core.Abstractions;

namespace VatDesk.Api.Controllers;

[ApiController]
[Route("api/countries")]
[Authorize]
public class CountriesController(IServiceProvider serviceProvider) : ControllerBase
{
    [HttpGet("{countryCode}/vat-categories")]
    public ActionResult<IReadOnlyList<VatCategoryDto>> GetVatCategories(string countryCode)
    {
        var registry = serviceProvider.GetKeyedService<IVatCategoryRegistry>(countryCode.ToUpperInvariant());
        if (registry is null)
        {
            return Problem(
                title: "Unknown country code",
                detail: $"No VAT category registry is registered for country code '{countryCode}'.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var categories = registry.All.OrderBy(c => c.SortOrder).Select(c => c.ToDto()).ToList();
        return Ok(categories);
    }
}
