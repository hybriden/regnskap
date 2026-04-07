using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Kundereskontro;
using Regnskap.Domain.Features.Kundereskontro;

namespace Regnskap.Api.Features.Kundereskontro;

[ApiController]
[Authorize]
[Route("api/rapporter/kunde")]
public class KundeRapportController : ControllerBase
{
    private readonly IKundeFakturaService _fakturaService;

    public KundeRapportController(IKundeFakturaService fakturaService)
    {
        _fakturaService = fakturaService;
    }

    [HttpGet("aldersfordeling")]
    public async Task<ActionResult<KundeAldersfordelingDto>> HentAldersfordeling(
        [FromQuery] DateOnly? dato,
        CancellationToken ct = default)
    {
        var d = dato ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _fakturaService.HentAldersfordelingAsync(d, ct);
        return Ok(result);
    }

    [HttpGet("aldersfordeling/{kundeId:guid}")]
    public async Task<ActionResult<KundeAldersfordelingDto>> HentAldersfordelingForKunde(
        Guid kundeId,
        [FromQuery] DateOnly? dato,
        CancellationToken ct = default)
    {
        var d = dato ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _fakturaService.HentAldersfordelingAsync(d, ct);
        // Filtrer til bare denne kunden
        var kundeData = result.Kunder.Where(k => k.KundeId == kundeId).ToList();
        var summary = new AldersfordelingSummaryDto(
            kundeData.Sum(k => k.IkkeForfalt),
            kundeData.Sum(k => k.Dager0Til30),
            kundeData.Sum(k => k.Dager31Til60),
            kundeData.Sum(k => k.Dager61Til90),
            kundeData.Sum(k => k.Over90Dager),
            kundeData.Sum(k => k.Totalt));
        return Ok(new KundeAldersfordelingDto(kundeData, summary, d));
    }

    [HttpGet("apne-poster")]
    public async Task<ActionResult<List<KundeFakturaDto>>> HentApnePoster(
        [FromQuery] DateOnly? dato,
        CancellationToken ct = default)
    {
        var poster = await _fakturaService.HentApnePosterAsync(dato, ct);
        return Ok(poster);
    }

    [HttpGet("utskrift/{kundeId:guid}")]
    public async Task<ActionResult<KundeutskriftDto>> HentUtskrift(
        Guid kundeId,
        [FromQuery] DateOnly fra,
        [FromQuery] DateOnly til,
        CancellationToken ct = default)
    {
        try
        {
            var utskrift = await _fakturaService.HentUtskriftAsync(kundeId, fra, til, ct);
            return Ok(utskrift);
        }
        catch (KundeIkkeFunnetException)
        {
            return NotFound();
        }
    }
}
