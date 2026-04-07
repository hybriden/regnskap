using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Mva;

namespace Regnskap.Api.Features.Mva;

[ApiController]
[Route("api/mva/terminer/{terminId:guid}/avstemming")]
[Authorize]
public class MvaAvstemmingController : ControllerBase
{
    private readonly IMvaAvstemmingService _avstemmingService;

    public MvaAvstemmingController(IMvaAvstemmingService avstemmingService)
    {
        _avstemmingService = avstemmingService;
    }

    [HttpPost("kjor")]
    public async Task<ActionResult<MvaAvstemmingDto>> KjorAvstemming(Guid terminId, CancellationToken ct)
    {
        try
        {
            var avstemming = await _avstemmingService.KjorAvstemmingAsync(terminId, ct);
            return Ok(avstemming);
        }
        catch (MvaTerminIkkeFunnetException)
        {
            return NotFound(new { Kode = "MVA_TERMIN_IKKE_FUNNET", Melding = $"MVA-termin {terminId} finnes ikke." });
        }
    }

    [HttpGet]
    public async Task<ActionResult<MvaAvstemmingDto>> HentAvstemming(Guid terminId, CancellationToken ct)
    {
        try
        {
            var avstemming = await _avstemmingService.HentAvstemmingAsync(terminId, ct);
            return Ok(avstemming);
        }
        catch (MvaTerminIkkeFunnetException)
        {
            return NotFound(new { Kode = "MVA_TERMIN_IKKE_FUNNET", Melding = $"MVA-termin {terminId} finnes ikke." });
        }
        catch (MvaAvstemmingIkkeFunnetException)
        {
            return NotFound(new { Kode = "MVA_AVSTEMMING_IKKE_FUNNET", Melding = $"Ingen avstemming funnet for termin {terminId}." });
        }
    }

    [HttpGet("historikk")]
    public async Task<ActionResult<List<MvaAvstemmingDto>>> HentHistorikk(Guid terminId, CancellationToken ct)
    {
        try
        {
            var historikk = await _avstemmingService.HentAvstemmingshistorikkAsync(terminId, ct);
            return Ok(historikk);
        }
        catch (MvaTerminIkkeFunnetException)
        {
            return NotFound(new { Kode = "MVA_TERMIN_IKKE_FUNNET", Melding = $"MVA-termin {terminId} finnes ikke." });
        }
    }

    [HttpPost("{id:guid}/godkjenn")]
    public async Task<ActionResult<MvaAvstemmingDto>> GodkjennAvstemming(
        Guid terminId, Guid id, [FromBody] GodkjennAvstemmingRequest? request, CancellationToken ct)
    {
        try
        {
            var avstemming = await _avstemmingService.GodkjennAvstemmingAsync(
                terminId, id, request?.Merknad, ct);
            return Ok(avstemming);
        }
        catch (MvaTerminIkkeFunnetException)
        {
            return NotFound(new { Kode = "MVA_TERMIN_IKKE_FUNNET", Melding = $"MVA-termin {terminId} finnes ikke." });
        }
        catch (MvaAvstemmingIkkeFunnetException)
        {
            return NotFound(new { Kode = "MVA_AVSTEMMING_IKKE_FUNNET", Melding = $"Avstemming ikke funnet." });
        }
    }
}

public record GodkjennAvstemmingRequest(string? Merknad);
