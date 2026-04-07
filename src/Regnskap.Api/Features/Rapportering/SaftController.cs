using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Rapportering;

namespace Regnskap.Api.Features.Rapportering;

[ApiController]
[Authorize]
[Route("api/saft")]
public class SaftController : ControllerBase
{
    private readonly ISaftEksportService _saftService;

    public SaftController(ISaftEksportService saftService)
    {
        _saftService = saftService;
    }

    /// <summary>
    /// Genererer komplett SAF-T Financial XML v1.30.
    /// </summary>
    [HttpGet("eksport")]
    public async Task<IActionResult> Eksport(
        [FromQuery] int ar,
        [FromQuery] int fraPeriode = 1,
        [FromQuery] int tilPeriode = 12,
        [FromQuery] string taxAccountingBasis = "A",
        CancellationToken ct = default)
    {
        var stream = await _saftService.GenererSaftXmlAsync(ar, fraPeriode, tilPeriode, taxAccountingBasis, ct);

        var filename = $"SAF-T_{ar}_{fraPeriode:D2}-{tilPeriode:D2}.xml";

        return File(stream, "application/xml", filename);
    }

    /// <summary>
    /// Validerer en SAF-T XML-fil.
    /// </summary>
    [HttpPost("valider")]
    public async Task<ActionResult<List<string>>> Valider(
        IFormFile fil,
        CancellationToken ct = default)
    {
        if (fil == null || fil.Length == 0)
            return BadRequest("Ingen fil lastet opp.");

        using var stream = fil.OpenReadStream();
        var feil = await _saftService.ValiderSaftXmlAsync(stream, ct);

        return Ok(feil);
    }
}
