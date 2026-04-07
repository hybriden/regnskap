using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Bilagsregistrering;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Api.Features.Bilagsregistrering;

[ApiController]
[Authorize]
public class BilagController : ControllerBase
{
    private readonly IBilagRegistreringService _bilagService;
    private readonly BilagSokService _sokService;

    public BilagController(IBilagRegistreringService bilagService, BilagSokService sokService)
    {
        _bilagService = bilagService;
        _sokService = sokService;
    }

    /// <summary>
    /// Opprett og (valgfritt) bokfor bilag.
    /// </summary>
    [HttpPost("api/bilag")]
    public async Task<ActionResult<BilagDto>> OpprettBilag(
        [FromBody] OpprettBilagRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _bilagService.OpprettOgBokforBilagAsync(request, ct);
            return CreatedAtAction(nameof(HentBilag), new { id = result.Id }, result);
        }
        catch (BilagValideringException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (AccountingBalanceException ex)
        {
            return BadRequest(new { error = "BILAG_IKKE_I_BALANSE", message = ex.Message });
        }
        catch (PeriodeIkkeFunnetException ex)
        {
            return BadRequest(new { error = "PERIODE_IKKE_FUNNET", message = ex.Message });
        }
        catch (PeriodeLukketException ex)
        {
            return BadRequest(new { error = "PERIODE_LUKKET", message = ex.Message });
        }
        catch (PeriodeSperretException ex)
        {
            return BadRequest(new { error = "PERIODE_SPERRET", message = ex.Message });
        }
        catch (SerieIkkeFunnetException ex)
        {
            return BadRequest(new { error = "SERIE_IKKE_FUNNET", message = ex.Message });
        }
        catch (SerieInaktivException ex)
        {
            return BadRequest(new { error = "SERIE_INAKTIV", message = ex.Message });
        }
        catch (KontoIkkeFunnetException ex)
        {
            return BadRequest(new { error = "KONTO_IKKE_FUNNET", message = ex.Message });
        }
        catch (MvaKodeIkkeFunnetException ex)
        {
            return BadRequest(new { error = "MVA_KODE_IKKE_FUNNET", message = ex.Message });
        }
        catch (NummereringKonfliktException ex)
        {
            return Conflict(new { error = "NUMMERERING_KONFLIKT", message = ex.Message });
        }
    }

    /// <summary>
    /// Hent bilag med posteringer og vedlegg.
    /// </summary>
    [HttpGet("api/bilag/{id:guid}")]
    public async Task<ActionResult<BilagDto>> HentBilag(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _bilagService.HentBilagDetaljertAsync(id, ct);
            return Ok(result);
        }
        catch (BilagIkkeFunnetException)
        {
            return NotFound(new { error = "BILAG_IKKE_FUNNET", message = $"Bilag {id} finnes ikke" });
        }
    }

    /// <summary>
    /// Hent bilag pa bilagsnummer.
    /// </summary>
    [HttpGet("api/bilag/nummer/{ar:int}/{bilagsnummer:int}")]
    public async Task<ActionResult<BilagDto>> HentBilagMedNummer(
        int ar, int bilagsnummer, CancellationToken ct)
    {
        try
        {
            var hovedbokResult = await _bilagService.HentBilagMedNummerAsync(ar, bilagsnummer, ct);
            return Ok(MapTilBilagDto(hovedbokResult));
        }
        catch (BilagIkkeFunnetException)
        {
            return NotFound(new { error = "BILAG_IKKE_FUNNET" });
        }
    }

    /// <summary>
    /// Hent bilag pa seriereferanse.
    /// </summary>
    [HttpGet("api/bilag/serie/{serieKode}/{ar:int}/{serieNummer:int}")]
    public async Task<ActionResult<BilagDto>> HentBilagMedSerie(
        string serieKode, int ar, int serieNummer, CancellationToken ct)
    {
        try
        {
            var result = await _bilagService.HentBilagMedSerieAsync(serieKode, ar, serieNummer, ct);
            return Ok(result);
        }
        catch (BilagIkkeFunnetException)
        {
            return NotFound(new { error = "BILAG_IKKE_FUNNET" });
        }
    }

    /// <summary>
    /// Sok i bilag.
    /// </summary>
    [HttpPost("api/bilag/sok")]
    public async Task<ActionResult<BilagSokResultatDto>> SokBilag(
        [FromBody] BilagSokRequest request, CancellationToken ct)
    {
        var result = await _sokService.SokAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Valider bilag uten a opprette.
    /// </summary>
    [HttpPost("api/bilag/valider")]
    public async Task<ActionResult<BilagValideringResultatDto>> ValiderBilag(
        [FromBody] ValiderBilagRequest request, CancellationToken ct)
    {
        var result = await _bilagService.ValiderBilagAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Bokfor et kladd-bilag.
    /// </summary>
    [HttpPost("api/bilag/{id:guid}/bokfor")]
    public async Task<ActionResult<BilagDto>> BokforBilag(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _bilagService.BokforBilagAsync(id, ct);
            return Ok(result);
        }
        catch (BilagIkkeFunnetException)
        {
            return NotFound(new { error = "BILAG_IKKE_FUNNET" });
        }
        catch (BilagAlleredeBokfortException)
        {
            return BadRequest(new { error = "BILAG_ALLEREDE_BOKFORT" });
        }
        catch (PeriodeLukketException ex)
        {
            return BadRequest(new { error = "PERIODE_LUKKET", message = ex.Message });
        }
    }

    /// <summary>
    /// Tilbakefor (reverser) et bilag.
    /// </summary>
    [HttpPost("api/bilag/{id:guid}/tilbakefor")]
    public async Task<ActionResult<BilagDto>> TilbakeforBilag(
        Guid id, [FromBody] TilbakeforBilagRequest request, CancellationToken ct)
    {
        try
        {
            var tilbakeforRequest = new TilbakeforBilagRequest(
                id, request.Tilbakeforingsdato, request.Beskrivelse);
            var result = await _bilagService.TilbakeforBilagAsync(tilbakeforRequest, ct);
            return CreatedAtAction(nameof(HentBilag), new { id = result.Id }, result);
        }
        catch (BilagIkkeFunnetException)
        {
            return NotFound(new { error = "BILAG_IKKE_FUNNET" });
        }
        catch (BilagIkkeBokfortForTilbakeforingException)
        {
            return BadRequest(new { error = "BILAG_IKKE_BOKFORT_FOR_TILBAKEFORING" });
        }
        catch (BilagAlleredeTilbakfortException)
        {
            return BadRequest(new { error = "BILAG_ALLEREDE_TILBAKFORT" });
        }
        catch (PeriodeLukketException ex)
        {
            return BadRequest(new { error = "PERIODE_LUKKET", message = ex.Message });
        }
    }

    // Enkel mapping fra Hovedbok DTO til Bilag DTO
    private static BilagDto MapTilBilagDto(Application.Features.Hovedbok.BilagDto src)
    {
        return new BilagDto(
            src.Id, src.BilagsId, null, src.Bilagsnummer, null, null,
            src.Ar, src.Type, src.Bilagsdato, src.Registreringsdato,
            src.Beskrivelse, src.EksternReferanse, src.Periode,
            src.Posteringer.Select(p => new PosteringDto(
                p.Id, p.Linjenummer, p.Kontonummer, p.Kontonavn,
                p.Side, p.Belop, p.Beskrivelse, p.MvaKode,
                p.MvaBelop, p.MvaGrunnlag, p.MvaSats,
                p.Avdelingskode, p.Prosjektkode, null, null, false)).ToList(),
            new List<VedleggDto>(),
            src.SumDebet, src.SumKredit,
            false, null, false, null, null);
    }
}
