using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Api.Features.Kontoplan.Dtos;
using Regnskap.Application.Features.Kontoplan;
using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Api.Features.Kontoplan;

[ApiController]
[Authorize]
[Route("api/kontoer")]
public class KontoController : ControllerBase
{
    private readonly IKontoService _kontoService;

    public KontoController(IKontoService kontoService)
    {
        _kontoService = kontoService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginertResultat<KontoListeDto>>> HentAlle(
        [FromQuery] int? kontoklasse,
        [FromQuery] string? kontotype,
        [FromQuery] int? gruppekode,
        [FromQuery] bool? erAktiv,
        [FromQuery] bool? erBokforbar,
        [FromQuery] string? sok,
        [FromQuery] int side = 1,
        [FromQuery] int antall = 50,
        CancellationToken ct = default)
    {
        Kontotype? kontotypeEnum = null;
        if (!string.IsNullOrEmpty(kontotype) && Enum.TryParse<Kontotype>(kontotype, true, out var parsed))
            kontotypeEnum = parsed;

        var (data, totalt) = await _kontoService.HentKontoerAsync(
            kontoklasse, kontotypeEnum, gruppekode, erAktiv, erBokforbar, sok, side, antall, ct);

        var dtos = data.Select(MapToListeDto).ToList();
        return Ok(new PaginertResultat<KontoListeDto>(dtos, side, antall, totalt));
    }

    [HttpGet("{kontonummer}")]
    public async Task<ActionResult<KontoDetaljerDto>> HentEnkel(string kontonummer, CancellationToken ct)
    {
        var konto = await _kontoService.HentKontoMedDetaljerAsync(kontonummer, ct);
        if (konto is null)
            return NotFound();

        var dto = new KontoDetaljerDto(
            konto.Id,
            konto.Kontonummer,
            konto.Navn,
            konto.NavnEn,
            konto.Kontotype.ToString(),
            konto.Normalbalanse.ToString(),
            (int)konto.Kontoklasse,
            konto.Kontogruppe?.Gruppekode ?? 0,
            konto.Kontogruppe?.Navn ?? "",
            konto.StandardAccountId,
            konto.GrupperingsKategori?.ToString(),
            konto.GrupperingsKode,
            konto.ErAktiv,
            konto.ErSystemkonto,
            konto.ErBokforbar,
            konto.StandardMvaKode,
            konto.Beskrivelse,
            konto.KreverAvdeling,
            konto.KreverProsjekt,
            konto.Underkontoer.Select(u => new UnderkontoDto(u.Kontonummer, u.Navn, u.ErAktiv)).ToList()
        );

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<KontoOpprettetDto>> Opprett([FromBody] KontoOpprettRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<Kontotype>(request.Kontotype, true, out var kontotype))
            return BadRequest(new { kode = "KONTO_NUMMER_UGYLDIG", melding = "Ugyldig kontotype." });

        GrupperingsKategori? grupperingsKategori = null;
        if (!string.IsNullOrEmpty(request.GrupperingsKategori))
        {
            if (!Enum.TryParse<GrupperingsKategori>(request.GrupperingsKategori, true, out var gk))
                return BadRequest(new { kode = "UGYLDIG_GRUPPERING", melding = "Ugyldig GrupperingsKategori." });
            grupperingsKategori = gk;
        }

        try
        {
            var konto = await _kontoService.OpprettKontoAsync(new OpprettKontoRequest(
                request.Kontonummer,
                request.Navn,
                request.NavnEn,
                kontotype,
                request.Gruppekode,
                request.StandardAccountId,
                grupperingsKategori,
                request.GrupperingsKode,
                request.ErBokforbar,
                request.StandardMvaKode,
                request.Beskrivelse,
                request.OverordnetKontonummer,
                request.KreverAvdeling,
                request.KreverProsjekt
            ), ct);

            return CreatedAtAction(nameof(HentEnkel),
                new { kontonummer = konto.Kontonummer },
                new KontoOpprettetDto(konto.Id, konto.Kontonummer, konto.Navn));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { kode = "VALIDERING_FEIL", melding = ex.Message });
        }
    }

    [HttpPut("{kontonummer}")]
    public async Task<ActionResult> Oppdater(string kontonummer, [FromBody] KontoOppdaterRequest request, CancellationToken ct)
    {
        GrupperingsKategori? grupperingsKategori = null;
        if (!string.IsNullOrEmpty(request.GrupperingsKategori))
        {
            if (!Enum.TryParse<GrupperingsKategori>(request.GrupperingsKategori, true, out var gk))
                return BadRequest(new { kode = "UGYLDIG_GRUPPERING", melding = "Ugyldig GrupperingsKategori." });
            grupperingsKategori = gk;
        }

        try
        {
            await _kontoService.OppdaterKontoAsync(kontonummer, new OppdaterKontoRequest(
                request.Navn,
                request.NavnEn,
                request.ErAktiv,
                request.ErBokforbar,
                request.StandardMvaKode,
                request.Beskrivelse,
                grupperingsKategori,
                request.GrupperingsKode,
                request.KreverAvdeling,
                request.KreverProsjekt
            ), ct);
            return Ok();
        }
        catch (KontoIkkeFunnetException)
        {
            return NotFound(new { kode = "KONTO_IKKE_FUNNET", melding = $"Konto {kontonummer} finnes ikke." });
        }
        catch (SystemkontoFeltEndringException ex)
        {
            return BadRequest(new { kode = "KONTO_SYSTEM_FELT_ENDRING", melding = ex.Message });
        }
    }

    [HttpDelete("{kontonummer}")]
    public async Task<ActionResult> Slett(string kontonummer, CancellationToken ct)
    {
        try
        {
            await _kontoService.SlettKontoAsync(kontonummer, ct);
            return NoContent();
        }
        catch (KontoIkkeFunnetException)
        {
            return NotFound();
        }
        catch (SystemkontoSlettingException ex)
        {
            return BadRequest(new { kode = "KONTO_ER_SYSTEMKONTO", melding = ex.Message });
        }
        catch (KontoHarPosteringerException ex)
        {
            return BadRequest(new { kode = "KONTO_HAR_POSTERINGER", melding = ex.Message });
        }
        catch (KontoHarUnderkontoerException ex)
        {
            return BadRequest(new { kode = "KONTO_HAR_UNDERKONTOER", melding = ex.Message });
        }
    }

    [HttpPost("{kontonummer}/deaktiver")]
    public async Task<ActionResult> Deaktiver(string kontonummer, CancellationToken ct)
    {
        try
        {
            await _kontoService.DeaktiverKontoAsync(kontonummer, ct);
            return Ok();
        }
        catch (KontoIkkeFunnetException)
        {
            return NotFound();
        }
    }

    [HttpPost("{kontonummer}/aktiver")]
    public async Task<ActionResult> Aktiver(string kontonummer, CancellationToken ct)
    {
        try
        {
            await _kontoService.AktiverKontoAsync(kontonummer, ct);
            return Ok();
        }
        catch (KontoIkkeFunnetException)
        {
            return NotFound();
        }
    }

    [HttpGet("oppslag")]
    public async Task<ActionResult<List<KontoOppslagDto>>> Oppslag(
        [FromQuery] string q,
        [FromQuery] int antall = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(new List<KontoOppslagDto>());

        var kontoer = await _kontoService.SokKontoerAsync(q, antall, ct);
        var dtos = kontoer.Select(k => new KontoOppslagDto(
            k.Kontonummer, k.Navn, k.Kontotype.ToString(), k.StandardMvaKode
        )).ToList();

        return Ok(dtos);
    }

    private static KontoListeDto MapToListeDto(Konto k) => new(
        k.Id,
        k.Kontonummer,
        k.Navn,
        k.NavnEn,
        k.Kontotype.ToString(),
        k.Normalbalanse.ToString(),
        (int)k.Kontoklasse,
        k.Kontogruppe?.Gruppekode ?? 0,
        k.Kontogruppe?.Navn ?? "",
        k.StandardAccountId,
        k.GrupperingsKategori?.ToString(),
        k.GrupperingsKode,
        k.ErAktiv,
        k.ErSystemkonto,
        k.ErBokforbar,
        k.StandardMvaKode,
        k.KreverAvdeling,
        k.KreverProsjekt,
        k.Underkontoer.Any(),
        k.OverordnetKonto?.Kontonummer
    );
}
