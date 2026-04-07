using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Api.Features.Kontoplan.Dtos;
using Regnskap.Api.Features.Kundereskontro.Dtos;
using Regnskap.Application.Features.Kundereskontro;
using Regnskap.Domain.Features.Kundereskontro;

namespace Regnskap.Api.Features.Kundereskontro;

[ApiController]
[Authorize]
[Route("api/purringer")]
public class PurringController : ControllerBase
{
    private readonly IPurringService _purringService;

    public PurringController(IPurringService purringService)
    {
        _purringService = purringService;
    }

    [HttpGet("forslag")]
    public async Task<ActionResult<List<PurreforslagDto>>> GenererForslag(
        [FromQuery] DateOnly? dato,
        [FromQuery] int minimumDagerForfalt = 14,
        [FromQuery] bool inkluderPurring1 = true,
        [FromQuery] bool inkluderPurring2 = true,
        [FromQuery] bool inkluderPurring3 = true,
        CancellationToken ct = default)
    {
        var d = dato ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var forslag = await _purringService.GenererForslagAsync(
            new PurreforslagRequest(d, minimumDagerForfalt, inkluderPurring1, inkluderPurring2, inkluderPurring3),
            ct);
        return Ok(forslag);
    }

    [HttpPost("opprett")]
    public async Task<ActionResult<List<PurringDto>>> OpprettPurringer([FromBody] OpprettPurringerApiRequest request, CancellationToken ct)
    {
        try
        {
            var purringer = await _purringService.OpprettPurringerAsync(request.FakturaIder, request.Type, ct);
            return Ok(purringer);
        }
        catch (PurringValideringException ex)
        {
            return BadRequest(new { kode = "PURRING_VALIDERING_FEIL", melding = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<PurringDto>>> HentPurringer(
        [FromQuery] int side = 1,
        [FromQuery] int antall = 50,
        CancellationToken ct = default)
    {
        var purringer = await _purringService.HentPurringerAsync(side, antall, ct);
        return Ok(purringer);
    }
}
