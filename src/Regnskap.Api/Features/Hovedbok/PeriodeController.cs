using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Api.Features.Hovedbok.Dtos;
using Regnskap.Application.Features.Hovedbok;
using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Api.Features.Hovedbok;

[ApiController]
[Authorize]
[Route("api/perioder")]
public class PeriodeController : ControllerBase
{
    private readonly IPeriodeService _periodeService;

    public PeriodeController(IPeriodeService periodeService)
    {
        _periodeService = periodeService;
    }

    /// <summary>
    /// Oppretter alle perioder (0-13) for et regnskapsar.
    /// </summary>
    [HttpPost("opprett-ar")]
    public async Task<ActionResult<PerioderListeResponse>> OpprettPerioder(
        [FromBody] OpprettPerioderApiRequest request, CancellationToken ct)
    {
        try
        {
            var perioder = await _periodeService.OpprettPerioderForArAsync(request.Ar, ct);
            return Created($"/api/perioder/{request.Ar}",
                new PerioderListeResponse(request.Ar, perioder));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (PerioderFinnesAlleredeException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Henter alle perioder for et regnskapsar.
    /// </summary>
    [HttpGet("{ar:int}")]
    public async Task<ActionResult<PerioderListeResponse>> HentPerioder(int ar, CancellationToken ct)
    {
        var perioder = await _periodeService.HentPerioderAsync(ar, ct);
        return Ok(new PerioderListeResponse(ar, perioder));
    }

    /// <summary>
    /// Endre status for en periode (sperr, gjenapne, eller lukk).
    /// </summary>
    [HttpPut("{ar:int}/{periode:int}/status")]
    public async Task<ActionResult<RegnskapsperiodeDto>> EndrePeriodeStatus(
        int ar, int periode, [FromBody] EndrePeriodeStatusApiRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<PeriodeStatus>(request.NyStatus, true, out var nyStatus))
            return BadRequest(new { error = $"Ugyldig status: {request.NyStatus}" });

        try
        {
            var result = await _periodeService.EndrePeriodeStatusAsync(
                ar, periode, nyStatus, request.Merknad, ct);
            return Ok(result);
        }
        catch (PeriodeIkkeFunnetException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UgyldigStatusOvergangException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (PeriodeLukkingException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Kjorer periodeavstemming for a sjekke om perioden er klar for lukking.
    /// </summary>
    [HttpGet("{ar:int}/{periode:int}/avstemming")]
    public async Task<ActionResult<PeriodeavstemmingDto>> KjorAvstemming(
        int ar, int periode, CancellationToken ct)
    {
        try
        {
            var result = await _periodeService.KjorPeriodeavstemmingAsync(ar, periode, ct);
            return Ok(result);
        }
        catch (PeriodeIkkeFunnetException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
