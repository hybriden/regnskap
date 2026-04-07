using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Leverandorreskontro;

namespace Regnskap.Api.Features.Leverandor;

[ApiController]
[Route("api/leverandorer")]
[Authorize]
public class LeverandorController : ControllerBase
{
    private readonly ILeverandorService _service;

    public LeverandorController(ILeverandorService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult> HentAlle(
        [FromQuery] string? q = null,
        [FromQuery] int side = 1,
        [FromQuery] int antall = 50,
        CancellationToken ct = default)
    {
        var result = await _service.SokAsync(new LeverandorSokRequest(q, side, antall), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeverandorDto>> Hent(Guid id, CancellationToken ct = default)
    {
        var result = await _service.HentAsync(id, ct);
        return Ok(result);
    }

    [HttpGet("sok")]
    public async Task<ActionResult> Sok(
        [FromQuery] string? q = null,
        [FromQuery] int side = 1,
        [FromQuery] int antall = 50,
        CancellationToken ct = default)
    {
        var result = await _service.SokAsync(new LeverandorSokRequest(q, side, antall), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<LeverandorDto>> Opprett(
        [FromBody] OpprettLeverandorRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.OpprettAsync(request, ct);
        return CreatedAtAction(nameof(Hent), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LeverandorDto>> Oppdater(
        Guid id,
        [FromBody] OppdaterLeverandorRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.OppdaterAsync(id, request, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Slett(Guid id, CancellationToken ct = default)
    {
        await _service.SlettAsync(id, ct);
        return NoContent();
    }
}
