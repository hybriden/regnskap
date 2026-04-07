using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Mva;

namespace Regnskap.Api.Features.Mva;

[ApiController]
[Route("api/saft")]
[Authorize]
public class SaftTaxTableController : ControllerBase
{
    private readonly ISaftTaxTableService _saftTaxTableService;

    public SaftTaxTableController(ISaftTaxTableService saftTaxTableService)
    {
        _saftTaxTableService = saftTaxTableService;
    }

    [HttpGet("taxtable")]
    public async Task<ActionResult<SaftTaxTableDto>> HentTaxTable(CancellationToken ct)
    {
        var taxTable = await _saftTaxTableService.GenererSaftTaxTableAsync(ct);
        return Ok(taxTable);
    }
}
