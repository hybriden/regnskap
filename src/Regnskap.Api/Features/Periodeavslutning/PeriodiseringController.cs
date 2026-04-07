using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Periodeavslutning;
using Regnskap.Domain.Features.Periodeavslutning;

namespace Regnskap.Api.Features.Periodeavslutning;

[ApiController]
[Route("api/periodiseringer")]
[Authorize]
public class PeriodiseringController : ControllerBase
{
    private readonly IPeriodiseringsService _service;

    public PeriodiseringController(IPeriodiseringsService service)
    {
        _service = service;
    }

    /// <summary>
    /// Opprett ny periodisering.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PeriodiseringDto>> OpprettPeriodisering(
        [FromBody] OpprettPeriodiseringRequest request, CancellationToken ct)
    {
        try
        {
            var resultat = await _service.OpprettPeriodiseringAsync(request, ct);
            return CreatedAtAction(nameof(HentPeriodiseringer), null, resultat);
        }
        catch (PeriodiseringsException ex)
        {
            return BadRequest(new { Kode = "PERIODISERING_FEIL", Melding = ex.Message });
        }
    }

    /// <summary>
    /// List periodiseringer.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<PeriodiseringDto>>> HentPeriodiseringer(
        [FromQuery] bool? aktive = true, CancellationToken ct = default)
    {
        var resultat = await _service.HentPeriodiseringerAsync(aktive, ct);
        return Ok(resultat);
    }

    /// <summary>
    /// Bokforer periodiseringer for en gitt periode.
    /// </summary>
    [HttpPost("bokfor")]
    public async Task<ActionResult<PeriodiseringBokforingDto>> BokforPeriodiseringer(
        [FromBody] BokforPeriodiseringerRequest request, CancellationToken ct)
    {
        try
        {
            var resultat = await _service.BokforPeriodiseringerAsync(request.Ar, request.Periode, ct);
            return Ok(resultat);
        }
        catch (PeriodiseringsException ex)
        {
            return BadRequest(new { Kode = "PERIODISERING_FEIL", Melding = ex.Message });
        }
        catch (DuplikatPeriodiseringException ex)
        {
            return Conflict(new { Kode = "DUPLIKAT_PERIODISERING", Melding = ex.Message });
        }
    }

    /// <summary>
    /// Deaktiver en periodisering.
    /// </summary>
    [HttpPost("{id:guid}/deaktiver")]
    public async Task<IActionResult> DeaktiverPeriodisering(Guid id, CancellationToken ct)
    {
        try
        {
            await _service.DeaktiverPeriodiseringAsync(id, ct);
            return NoContent();
        }
        catch (PeriodiseringIkkeFunnetException)
        {
            return NotFound(new { Kode = "PERIODISERING_IKKE_FUNNET", Melding = $"Periodisering {id} finnes ikke." });
        }
    }
}
