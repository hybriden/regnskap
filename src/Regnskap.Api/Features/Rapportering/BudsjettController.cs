using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Rapportering;

namespace Regnskap.Api.Features.Rapportering;

[ApiController]
[Authorize]
[Route("api/budsjett")]
public class BudsjettController : ControllerBase
{
    private readonly IBudsjettService _budsjettService;

    public BudsjettController(IBudsjettService budsjettService)
    {
        _budsjettService = budsjettService;
    }

    /// <summary>
    /// Opprett/oppdater en budsjettlinje.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<BudsjettDto>> OpprettBudsjettLinje(
        [FromBody] OpprettBudsjettRequest request,
        CancellationToken ct = default)
    {
        var resultat = await _budsjettService.OpprettBudsjettLinjeAsync(request, ct);
        return Ok(resultat);
    }

    /// <summary>
    /// Masseimport av budsjettlinjer.
    /// </summary>
    [HttpPost("bulk")]
    public async Task<ActionResult<List<BudsjettDto>>> BulkImport(
        [FromBody] BudsjettBulkRequest request,
        CancellationToken ct = default)
    {
        var resultat = await _budsjettService.BulkImportAsync(request, ct);
        return Ok(resultat);
    }

    /// <summary>
    /// Hent budsjett for et ar.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<BudsjettDto>>> HentBudsjett(
        [FromQuery] int ar,
        [FromQuery] string versjon = "Opprinnelig",
        CancellationToken ct = default)
    {
        var resultat = await _budsjettService.HentBudsjettAsync(ar, versjon, ct);
        return Ok(resultat);
    }

    /// <summary>
    /// Slett budsjett for et ar og versjon.
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> SlettBudsjett(
        [FromQuery] int ar,
        [FromQuery] string versjon = "Opprinnelig",
        CancellationToken ct = default)
    {
        await _budsjettService.SlettBudsjettAsync(ar, versjon, ct);
        return NoContent();
    }
}
