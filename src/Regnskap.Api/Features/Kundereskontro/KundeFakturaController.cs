using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Api.Features.Kontoplan.Dtos;
using Regnskap.Api.Features.Kundereskontro.Dtos;
using Regnskap.Application.Features.Kundereskontro;
using Regnskap.Domain.Features.Kundereskontro;

namespace Regnskap.Api.Features.Kundereskontro;

[ApiController]
[Authorize]
[Route("api/kundefakturaer")]
public class KundeFakturaController : ControllerBase
{
    private readonly IKundeFakturaService _fakturaService;

    public KundeFakturaController(IKundeFakturaService fakturaService)
    {
        _fakturaService = fakturaService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginertResultat<KundeFakturaDto>>> HentAlle(
        [FromQuery] Guid? kundeId,
        [FromQuery] KundeFakturaStatus? status,
        [FromQuery] int side = 1,
        [FromQuery] int antall = 50,
        CancellationToken ct = default)
    {
        var (data, totalt) = await _fakturaService.SokAsync(new KundeFakturaSokRequest(kundeId, status, side, antall), ct);
        return Ok(new PaginertResultat<KundeFakturaDto>(data, side, antall, totalt));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<KundeFakturaDto>> Hent(Guid id, CancellationToken ct)
    {
        try
        {
            var faktura = await _fakturaService.HentAsync(id, ct);
            return Ok(faktura);
        }
        catch (KundeFakturaIkkeFunnetException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<ActionResult<KundeFakturaDto>> Registrer([FromBody] RegistrerFakturaApiRequest request, CancellationToken ct)
    {
        try
        {
            var linjer = request.Linjer.Select(l => new KundeFakturaLinjeRequest(
                l.KontoId, l.Beskrivelse, l.Antall, l.Enhetspris, l.MvaKode, l.Rabatt, l.Avdelingskode, l.Prosjektkode)).ToList();

            var faktura = await _fakturaService.RegistrerFakturaAsync(new RegistrerKundeFakturaRequest(
                request.KundeId,
                request.Type,
                request.Fakturadato,
                request.Forfallsdato,
                request.Leveringsdato,
                request.Beskrivelse,
                request.EksternReferanse,
                request.Bestillingsnummer,
                request.Valutakode,
                request.Valutakurs,
                linjer), ct);

            return CreatedAtAction(nameof(Hent), new { id = faktura.Id }, faktura);
        }
        catch (KundeIkkeFunnetException ex)
        {
            return NotFound(new { kode = "KUNDE_IKKE_FUNNET", melding = ex.Message });
        }
        catch (KundeSperretException ex)
        {
            return BadRequest(new { kode = "KUNDE_SPERRET", melding = ex.Message });
        }
        catch (KredittgrenseOverskredetException ex)
        {
            return BadRequest(new { kode = "KREDITTGRENSE_OVERSKREDET", melding = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { kode = "VALIDERING_FEIL", melding = ex.Message });
        }
    }

    [HttpGet("apne-poster")]
    public async Task<ActionResult<List<KundeFakturaDto>>> HentApnePoster(
        [FromQuery] DateOnly? dato,
        CancellationToken ct = default)
    {
        var poster = await _fakturaService.HentApnePosterAsync(dato, ct);
        return Ok(poster);
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
}
