using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Leverandorreskontro;

namespace Regnskap.Api.Features.Leverandor;

[ApiController]
[Route("api/leverandorfakturaer")]
[Authorize]
public class LeverandorFakturaController : ControllerBase
{
    private readonly ILeverandorFakturaService _service;

    public LeverandorFakturaController(ILeverandorFakturaService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeverandorFakturaDto>> Hent(Guid id, CancellationToken ct = default)
    {
        var result = await _service.HentAsync(id, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<LeverandorFakturaDto>> Registrer(
        [FromBody] RegistrerFakturaRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.RegistrerFakturaAsync(request, ct);
        return CreatedAtAction(nameof(Hent), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/godkjenn")]
    public async Task<ActionResult<LeverandorFakturaDto>> Godkjenn(Guid id, CancellationToken ct = default)
    {
        var result = await _service.GodkjennAsync(id, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/sperr")]
    public async Task<ActionResult<LeverandorFakturaDto>> Sperr(
        Guid id,
        [FromBody] SperrRequest request,
        CancellationToken ct = default)
    {
        var result = await _service.SperrAsync(id, request.Arsak, ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}/opphev-sperring")]
    public async Task<ActionResult<LeverandorFakturaDto>> OpphevSperring(Guid id, CancellationToken ct = default)
    {
        var result = await _service.OpphevSperringAsync(id, ct);
        return Ok(result);
    }

    [HttpGet("apne-poster")]
    public async Task<ActionResult<List<LeverandorFakturaDto>>> ApnePoster(
        [FromQuery] DateOnly? dato = null,
        CancellationToken ct = default)
    {
        var result = await _service.HentApnePosterAsync(dato, ct);
        return Ok(result);
    }

    [HttpGet("aldersfordeling")]
    public async Task<ActionResult<AldersfordelingDto>> Aldersfordeling(
        [FromQuery] DateOnly? dato = null,
        CancellationToken ct = default)
    {
        var rapportDato = dato ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _service.HentAldersfordelingAsync(rapportDato, ct);
        return Ok(result);
    }
}

public record SperrRequest(string Arsak);
