using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Leverandorreskontro;

namespace Regnskap.Api.Features.Leverandor;

[ApiController]
[Route("api/rapporter/leverandor")]
[Authorize]
public class LeverandorRapportController : ControllerBase
{
    private readonly ILeverandorFakturaService _fakturaService;

    public LeverandorRapportController(ILeverandorFakturaService fakturaService)
    {
        _fakturaService = fakturaService;
    }

    [HttpGet("aldersfordeling")]
    public async Task<ActionResult<AldersfordelingDto>> Aldersfordeling(
        [FromQuery] DateOnly? dato = null,
        CancellationToken ct = default)
    {
        var rapportDato = dato ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _fakturaService.HentAldersfordelingAsync(rapportDato, ct);
        return Ok(result);
    }

    [HttpGet("apne-poster")]
    public async Task<ActionResult<List<LeverandorFakturaDto>>> ApnePoster(
        [FromQuery] DateOnly? dato = null,
        CancellationToken ct = default)
    {
        var result = await _fakturaService.HentApnePosterAsync(dato, ct);
        return Ok(result);
    }

    [HttpGet("utskrift/{leverandorId:guid}")]
    public async Task<ActionResult<LeverandorutskriftDto>> Utskrift(
        Guid leverandorId,
        [FromQuery] DateOnly fra,
        [FromQuery] DateOnly til,
        CancellationToken ct = default)
    {
        var result = await _fakturaService.HentUtskriftAsync(leverandorId, fra, til, ct);
        return Ok(result);
    }
}
