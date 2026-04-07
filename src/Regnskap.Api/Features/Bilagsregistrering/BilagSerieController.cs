using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Bilagsregistrering;

namespace Regnskap.Api.Features.Bilagsregistrering;

[ApiController]
[Authorize]
public class BilagSerieController : ControllerBase
{
    private readonly IBilagRegistreringService _bilagService;

    public BilagSerieController(IBilagRegistreringService bilagService)
    {
        _bilagService = bilagService;
    }

    /// <summary>
    /// Hent alle bilagserier.
    /// </summary>
    [HttpGet("api/bilagserier")]
    public async Task<ActionResult<List<BilagSerieDto>>> HentAlleSerier(CancellationToken ct)
    {
        var result = await _bilagService.HentAlleBilagSerierAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Hent bilagserie.
    /// </summary>
    [HttpGet("api/bilagserier/{kode}")]
    public async Task<ActionResult<BilagSerieDto>> HentSerie(string kode, CancellationToken ct)
    {
        try
        {
            var result = await _bilagService.HentBilagSerieAsync(kode, ct);
            return Ok(result);
        }
        catch (SerieIkkeFunnetException)
        {
            return NotFound(new { error = "SERIE_IKKE_FUNNET" });
        }
    }

    /// <summary>
    /// Opprett ny bilagserie.
    /// </summary>
    [HttpPost("api/bilagserier")]
    public async Task<ActionResult<BilagSerieDto>> OpprettSerie(
        [FromBody] OpprettBilagSerieRequest request, CancellationToken ct)
    {
        var result = await _bilagService.OpprettBilagSerieAsync(request, ct);
        return CreatedAtAction(nameof(HentSerie), new { kode = result.Kode }, result);
    }

    /// <summary>
    /// Oppdater bilagserie.
    /// </summary>
    [HttpPut("api/bilagserier/{kode}")]
    public async Task<ActionResult<BilagSerieDto>> OppdaterSerie(
        string kode, [FromBody] OppdaterBilagSerieRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _bilagService.OppdaterBilagSerieAsync(kode, request, ct);
            return Ok(result);
        }
        catch (SerieIkkeFunnetException)
        {
            return NotFound(new { error = "SERIE_IKKE_FUNNET" });
        }
        catch (SystemserieKanIkkeDeaktiveresException ex)
        {
            return BadRequest(new { error = "SYSTEMSERIE_KAN_IKKE_DEAKTIVERES", message = ex.Message });
        }
    }
}
