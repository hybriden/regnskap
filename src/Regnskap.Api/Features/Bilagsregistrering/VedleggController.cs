using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Bilagsregistrering;

namespace Regnskap.Api.Features.Bilagsregistrering;

[ApiController]
[Authorize]
public class VedleggController : ControllerBase
{
    private readonly IBilagRegistreringService _bilagService;

    public VedleggController(IBilagRegistreringService bilagService)
    {
        _bilagService = bilagService;
    }

    /// <summary>
    /// Legg til vedlegg.
    /// </summary>
    [HttpPost("api/bilag/{id:guid}/vedlegg")]
    public async Task<ActionResult<VedleggDto>> LeggTilVedlegg(
        Guid id, [FromBody] LeggTilVedleggRequest request, CancellationToken ct)
    {
        try
        {
            var vedleggRequest = new LeggTilVedleggRequest(
                id, request.Filnavn, request.MimeType, request.Storrelse,
                request.LagringSti, request.HashSha256, request.Beskrivelse);
            var result = await _bilagService.LeggTilVedleggAsync(vedleggRequest, ct);
            return Created($"api/bilag/{id}/vedlegg/{result.Id}", result);
        }
        catch (BilagIkkeFunnetException)
        {
            return NotFound(new { error = "BILAG_IKKE_FUNNET" });
        }
        catch (UgyldigMimeTypeException ex)
        {
            return BadRequest(new { error = "UGYLDIG_MIME_TYPE", message = ex.Message });
        }
        catch (VedleggForStortException ex)
        {
            return BadRequest(new { error = "VEDLEGG_FOR_STORT", message = ex.Message });
        }
    }

    /// <summary>
    /// Hent alle vedlegg for bilag.
    /// </summary>
    [HttpGet("api/bilag/{id:guid}/vedlegg")]
    public async Task<ActionResult<List<VedleggDto>>> HentVedlegg(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _bilagService.HentVedleggForBilagAsync(id, ct);
            return Ok(result);
        }
        catch (BilagIkkeFunnetException)
        {
            return NotFound(new { error = "BILAG_IKKE_FUNNET" });
        }
    }

    /// <summary>
    /// Slett vedlegg (soft delete).
    /// </summary>
    [HttpDelete("api/bilag/{id:guid}/vedlegg/{vedleggId:guid}")]
    public async Task<ActionResult> SlettVedlegg(Guid id, Guid vedleggId, CancellationToken ct)
    {
        try
        {
            await _bilagService.SlettVedleggAsync(id, vedleggId, ct);
            return NoContent();
        }
        catch (BilagIkkeFunnetException)
        {
            return NotFound(new { error = "BILAG_IKKE_FUNNET" });
        }
        catch (VedleggIkkeFunnetException)
        {
            return NotFound(new { error = "VEDLEGG_IKKE_FUNNET" });
        }
        catch (VedleggPaBokfortBilagException ex)
        {
            return BadRequest(new { error = "VEDLEGG_PA_BOKFORT_BILAG", message = ex.Message });
        }
    }
}
