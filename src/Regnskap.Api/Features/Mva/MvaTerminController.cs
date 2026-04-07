using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Mva;

namespace Regnskap.Api.Features.Mva;

[ApiController]
[Route("api/mva/terminer")]
[Authorize]
public class MvaTerminController : ControllerBase
{
    private readonly IMvaTerminService _terminService;

    public MvaTerminController(IMvaTerminService terminService)
    {
        _terminService = terminService;
    }

    [HttpGet]
    public async Task<ActionResult<List<MvaTerminDto>>> HentTerminer(
        [FromQuery] int ar, CancellationToken ct)
    {
        var terminer = await _terminService.HentTerminerAsync(ar, ct);
        return Ok(terminer);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MvaTerminDto>> HentTermin(Guid id, CancellationToken ct)
    {
        try
        {
            var termin = await _terminService.HentTerminAsync(id, ct);
            return Ok(termin);
        }
        catch (MvaTerminIkkeFunnetException)
        {
            return NotFound(new { Kode = "MVA_TERMIN_IKKE_FUNNET", Melding = $"MVA-termin {id} finnes ikke." });
        }
    }

    [HttpPost("generer")]
    public async Task<ActionResult<List<MvaTerminDto>>> GenererTerminer(
        [FromBody] GenererTerminerRequest request, CancellationToken ct)
    {
        try
        {
            var terminer = await _terminService.GenererTerminerAsync(request.Ar, request.Type, ct);
            return CreatedAtAction(nameof(HentTerminer), new { ar = request.Ar }, terminer);
        }
        catch (MvaTerminerFinnesException ex)
        {
            return Conflict(new { Kode = "MVA_TERMINER_FINNES", Melding = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Kode = "UGYLDIG_INPUT", Melding = ex.Message });
        }
    }
}
