using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Api.Features.Kontoplan.Dtos;
using Regnskap.Api.Features.Kundereskontro.Dtos;
using Regnskap.Application.Features.Kundereskontro;
using Regnskap.Domain.Features.Kundereskontro;

namespace Regnskap.Api.Features.Kundereskontro;

[ApiController]
[Authorize]
[Route("api/kunder")]
public class KundeController : ControllerBase
{
    private readonly IKundeService _kundeService;

    public KundeController(IKundeService kundeService)
    {
        _kundeService = kundeService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginertResultat<KundeDto>>> HentAlle(
        [FromQuery] string? q,
        [FromQuery] int side = 1,
        [FromQuery] int antall = 50,
        CancellationToken ct = default)
    {
        var (data, totalt) = await _kundeService.SokAsync(new KundeSokRequest(q, side, antall), ct);
        return Ok(new PaginertResultat<KundeDto>(data, side, antall, totalt));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<KundeDto>> Hent(Guid id, CancellationToken ct)
    {
        try
        {
            var kunde = await _kundeService.HentAsync(id, ct);
            return Ok(kunde);
        }
        catch (KundeIkkeFunnetException)
        {
            return NotFound();
        }
    }

    [HttpGet("sok")]
    public async Task<ActionResult<PaginertResultat<KundeDto>>> Sok(
        [FromQuery] string? q,
        [FromQuery] int side = 1,
        [FromQuery] int antall = 50,
        CancellationToken ct = default)
    {
        var (data, totalt) = await _kundeService.SokAsync(new KundeSokRequest(q, side, antall), ct);
        return Ok(new PaginertResultat<KundeDto>(data, side, antall, totalt));
    }

    [HttpPost]
    public async Task<ActionResult<KundeDto>> Opprett([FromBody] OpprettKundeApiRequest request, CancellationToken ct)
    {
        try
        {
            var kunde = await _kundeService.OpprettAsync(new OpprettKundeRequest(
                request.Kundenummer,
                request.Navn,
                request.ErBedrift,
                request.Organisasjonsnummer,
                request.Fodselsnummer,
                request.Adresse1,
                request.Adresse2,
                request.Postnummer,
                request.Poststed,
                request.Landkode,
                request.Kontaktperson,
                request.Telefon,
                request.Epost,
                request.Betalingsbetingelse,
                request.EgendefinertBetalingsfrist,
                request.StandardKontoId,
                request.StandardMvaKode,
                request.Kredittgrense,
                request.PeppolId,
                request.KanMottaEhf), ct);

            return CreatedAtAction(nameof(Hent), new { id = kunde.Id }, kunde);
        }
        catch (KundenummerEksistererException ex)
        {
            return Conflict(new { kode = "KUNDENUMMER_EKSISTERER", melding = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { kode = "VALIDERING_FEIL", melding = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<KundeDto>> Oppdater(Guid id, [FromBody] OppdaterKundeApiRequest request, CancellationToken ct)
    {
        try
        {
            var kunde = await _kundeService.OppdaterAsync(id, new OppdaterKundeRequest(
                request.Navn,
                request.Organisasjonsnummer,
                request.Fodselsnummer,
                request.Adresse1,
                request.Adresse2,
                request.Postnummer,
                request.Poststed,
                request.Landkode,
                request.Kontaktperson,
                request.Telefon,
                request.Epost,
                request.Betalingsbetingelse,
                request.EgendefinertBetalingsfrist,
                request.StandardKontoId,
                request.StandardMvaKode,
                request.Kredittgrense,
                request.PeppolId,
                request.KanMottaEhf,
                request.ErAktiv,
                request.ErSperret), ct);

            return Ok(kunde);
        }
        catch (KundeIkkeFunnetException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Slett(Guid id, CancellationToken ct)
    {
        try
        {
            await _kundeService.SlettAsync(id, ct);
            return NoContent();
        }
        catch (KundeIkkeFunnetException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id:guid}/saldo")]
    public async Task<ActionResult<object>> HentSaldo(Guid id, CancellationToken ct)
    {
        try
        {
            var saldo = await _kundeService.HentSaldoAsync(id, ct);
            return Ok(new { kundeId = id, saldo });
        }
        catch (KundeIkkeFunnetException)
        {
            return NotFound();
        }
    }
}
