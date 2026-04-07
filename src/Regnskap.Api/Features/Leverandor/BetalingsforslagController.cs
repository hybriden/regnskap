using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Leverandorreskontro;

namespace Regnskap.Api.Features.Leverandor;

[ApiController]
[Route("api/betalingsforslag")]
[Authorize]
public class BetalingsforslagController : ControllerBase
{
    private readonly IBetalingsforslagService _service;

    public BetalingsforslagController(IBetalingsforslagService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BetalingsforslagDto>> Hent(Guid id, CancellationToken ct = default)
    {
        var result = await _service.HentAsync(id, ct);
        return Ok(result);
    }

    [HttpPost("generer")]
    public async Task<ActionResult<BetalingsforslagDto>> Generer(
        [FromBody] GenererBetalingsforslagRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.GenererAsync(request, ct);
        return CreatedAtAction(nameof(Hent), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/godkjenn")]
    public async Task<ActionResult<BetalingsforslagDto>> Godkjenn(
        Guid id,
        CancellationToken ct = default)
    {
        var bruker = User.Identity?.Name ?? "system";
        var result = await _service.GodkjennAsync(id, bruker, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/generer-fil")]
    public async Task<ActionResult> GenererFil(Guid id, CancellationToken ct = default)
    {
        var fil = await _service.GenererFilAsync(id, ct);
        return File(fil, "application/xml", $"pain001_{id}.xml");
    }

    [HttpPut("{id:guid}/marker-sendt")]
    public async Task<ActionResult> MarkerSendt(Guid id, CancellationToken ct = default)
    {
        await _service.MarkerSendtAsync(id, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Kanseller(Guid id, CancellationToken ct = default)
    {
        await _service.KansellerAsync(id, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/linjer/{linjeId:guid}/ekskluder")]
    public async Task<ActionResult> EkskluderLinje(Guid id, Guid linjeId, CancellationToken ct = default)
    {
        await _service.EkskluderLinjeAsync(id, linjeId, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/linjer/{linjeId:guid}/inkluder")]
    public async Task<ActionResult> InkluderLinje(Guid id, Guid linjeId, CancellationToken ct = default)
    {
        await _service.InkluderLinjeAsync(id, linjeId, ct);
        return NoContent();
    }
}
