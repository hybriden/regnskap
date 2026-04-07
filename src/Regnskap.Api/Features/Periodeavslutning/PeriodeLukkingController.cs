using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Periodeavslutning;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Periodeavslutning;

namespace Regnskap.Api.Features.Periodeavslutning;

[ApiController]
[Route("api/periodeavslutning")]
[Authorize]
public class PeriodeLukkingController : ControllerBase
{
    private readonly IPeriodeavslutningService _service;

    public PeriodeLukkingController(IPeriodeavslutningService service)
    {
        _service = service;
    }

    /// <summary>
    /// Kjorer komplett avstemming for en maned.
    /// </summary>
    [HttpPost("{ar:int}/{periode:int}/avstemming")]
    public async Task<ActionResult<AvstemmingResultatDto>> KjorAvstemming(
        int ar, int periode, CancellationToken ct)
    {
        var resultat = await _service.KjorAvstemmingAsync(ar, periode, ct);
        return Ok(resultat);
    }

    /// <summary>
    /// Lukker en maned slik at ingen flere posteringer kan legges til.
    /// </summary>
    [HttpPost("{ar:int}/{periode:int}/lukk")]
    public async Task<ActionResult<PeriodeLukkingDto>> LukkPeriode(
        int ar, int periode, [FromBody] LukkPeriodeRequest request, CancellationToken ct)
    {
        try
        {
            var resultat = await _service.LukkPeriodeAsync(ar, periode, request, ct);
            return Ok(resultat);
        }
        catch (PeriodeLukkingException ex)
        {
            return BadRequest(new { Kode = "PERIODE_LUKKING_FEIL", Melding = ex.Message });
        }
    }

    /// <summary>
    /// Gjenapner en lukket periode.
    /// </summary>
    [HttpPost("{ar:int}/{periode:int}/gjenapne")]
    public async Task<ActionResult<PeriodeLukkingDto>> GjenapnePeriode(
        int ar, int periode, [FromBody] GjenapnePeriodeRequest request, CancellationToken ct)
    {
        try
        {
            var resultat = await _service.GjenapnePeriodeAsync(ar, periode, request, ct);
            return Ok(resultat);
        }
        catch (PeriodeLukkingException ex)
        {
            return BadRequest(new { Kode = "GJENAPNING_FEIL", Melding = ex.Message });
        }
    }

    /// <summary>
    /// Starter og gjennomforer arsavslutningsprosessen.
    /// </summary>
    [HttpPost("{ar:int}/arsavslutning")]
    public async Task<ActionResult<ArsavslutningDto>> KjorArsavslutning(
        int ar, [FromBody] ArsavslutningRequest request, CancellationToken ct)
    {
        try
        {
            var resultat = await _service.KjorArsavslutningAsync(ar, request, ct);
            return Ok(resultat);
        }
        catch (ArsavslutningException ex)
        {
            return BadRequest(new { Kode = "ARSAVSLUTNING_FEIL", Melding = ex.Message });
        }
    }

    /// <summary>
    /// Hent status for arsavslutning.
    /// </summary>
    [HttpGet("{ar:int}/arsavslutning/status")]
    public async Task<ActionResult<ArsavslutningStatus>> HentArsavslutningStatus(
        int ar, CancellationToken ct)
    {
        var status = await _service.HentArsavslutningStatusAsync(ar, ct);
        return Ok(status);
    }

    /// <summary>
    /// Sjekker om arsregnskapet er klart for innsending.
    /// </summary>
    [HttpGet("{ar:int}/klargjoring")]
    public async Task<ActionResult<ArsregnskapsklarDto>> SjekkKlargjoring(
        int ar, CancellationToken ct)
    {
        var resultat = await _service.SjekkKlargjoringAsync(ar, ct);
        return Ok(resultat);
    }
}
