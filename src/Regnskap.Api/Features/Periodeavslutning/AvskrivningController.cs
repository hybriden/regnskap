using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Periodeavslutning;
using Regnskap.Domain.Features.Periodeavslutning;

namespace Regnskap.Api.Features.Periodeavslutning;

[ApiController]
[Route("api/anleggsmidler")]
[Authorize]
public class AvskrivningController : ControllerBase
{
    private readonly IAvskrivningService _service;

    public AvskrivningController(IAvskrivningService service)
    {
        _service = service;
    }

    /// <summary>
    /// Registrer nytt anleggsmiddel.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AnleggsmiddelDto>> OpprettAnleggsmiddel(
        [FromBody] OpprettAnleggsmiddelRequest request, CancellationToken ct)
    {
        try
        {
            var resultat = await _service.OpprettAnleggsmiddelAsync(request, ct);
            return CreatedAtAction(nameof(HentAnleggsmiddel), new { id = resultat.Id }, resultat);
        }
        catch (AvskrivningException ex)
        {
            return BadRequest(new { Kode = "ANLEGGSMIDDEL_FEIL", Melding = ex.Message });
        }
    }

    /// <summary>
    /// List anleggsmidler med status.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AnleggsmiddelDto>>> HentAnleggsmidler(
        [FromQuery] bool? aktive = true, [FromQuery] string? kontonummer = null, CancellationToken ct = default)
    {
        var resultat = await _service.HentAnleggsmidlerAsync(aktive, kontonummer, ct);
        return Ok(resultat);
    }

    /// <summary>
    /// Hent ett anleggsmiddel med avskrivningshistorikk.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AnleggsmiddelDto>> HentAnleggsmiddel(Guid id, CancellationToken ct)
    {
        try
        {
            var resultat = await _service.HentAnleggsmiddelAsync(id, ct);
            return Ok(resultat);
        }
        catch (AnleggsmiddelIkkeFunnetException)
        {
            return NotFound(new { Kode = "ANLEGGSMIDDEL_IKKE_FUNNET", Melding = $"Anleggsmiddel {id} finnes ikke." });
        }
    }

    /// <summary>
    /// Utranjer et anleggsmiddel.
    /// </summary>
    [HttpPost("{id:guid}/utranjer")]
    public async Task<IActionResult> UtranjerAnleggsmiddel(
        Guid id, [FromBody] UtranjerRequest request, CancellationToken ct)
    {
        try
        {
            await _service.UtrangerAnleggsmiddelAsync(id, request.UtrangeringsDato, ct);
            return NoContent();
        }
        catch (AnleggsmiddelIkkeFunnetException)
        {
            return NotFound(new { Kode = "ANLEGGSMIDDEL_IKKE_FUNNET", Melding = $"Anleggsmiddel {id} finnes ikke." });
        }
        catch (AvskrivningException ex)
        {
            return BadRequest(new { Kode = "UTRANGERING_FEIL", Melding = ex.Message });
        }
    }
}

/// <summary>
/// Avskrivning-spesifikke API-endepunkter.
/// </summary>
[ApiController]
[Route("api/avskrivninger")]
[Authorize]
public class AvskrivningBokforingController : ControllerBase
{
    private readonly IAvskrivningService _service;

    public AvskrivningBokforingController(IAvskrivningService service)
    {
        _service = service;
    }

    /// <summary>
    /// Beregner avskrivninger for en periode (forhåndsvisning).
    /// </summary>
    [HttpPost("beregn")]
    public async Task<ActionResult<AvskrivningBeregningDto>> BeregnAvskrivninger(
        [FromBody] BeregnAvskrivningerRequest request, CancellationToken ct)
    {
        var resultat = await _service.BeregnAvskrivningerAsync(request.Ar, request.Periode, ct);
        return Ok(resultat);
    }

    /// <summary>
    /// Bokforer beregnede avskrivninger.
    /// </summary>
    [HttpPost("bokfor")]
    public async Task<ActionResult<AvskrivningBokforingDto>> BokforAvskrivninger(
        [FromBody] BokforAvskrivningerRequest request, CancellationToken ct)
    {
        try
        {
            var resultat = await _service.BokforAvskrivningerAsync(request.Ar, request.Periode, ct);
            return Ok(resultat);
        }
        catch (AvskrivningException ex)
        {
            return BadRequest(new { Kode = "AVSKRIVNING_FEIL", Melding = ex.Message });
        }
        catch (DuplikatAvskrivningException ex)
        {
            return Conflict(new { Kode = "DUPLIKAT_AVSKRIVNING", Melding = ex.Message });
        }
    }
}

public record UtranjerRequest(DateOnly UtrangeringsDato);
