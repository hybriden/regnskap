using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Mva;

namespace Regnskap.Api.Features.Mva;

[ApiController]
[Route("api/mva/terminer/{terminId:guid}/oppgjor")]
[Authorize]
public class MvaOppgjorController : ControllerBase
{
    private readonly IMvaOppgjorService _oppgjorService;
    private readonly IMvaMeldingService _meldingService;

    public MvaOppgjorController(
        IMvaOppgjorService oppgjorService,
        IMvaMeldingService meldingService)
    {
        _oppgjorService = oppgjorService;
        _meldingService = meldingService;
    }

    [HttpPost("beregn")]
    public async Task<ActionResult<MvaOppgjorDto>> BeregnOppgjor(Guid terminId, CancellationToken ct)
    {
        try
        {
            var oppgjor = await _oppgjorService.BeregnOppgjorAsync(terminId, ct);
            return Ok(oppgjor);
        }
        catch (MvaTerminIkkeFunnetException)
        {
            return NotFound(new { Kode = "MVA_TERMIN_IKKE_FUNNET", Melding = $"MVA-termin {terminId} finnes ikke." });
        }
        catch (MvaTerminIkkeApenException)
        {
            return UnprocessableEntity(new { Kode = "MVA_TERMIN_IKKE_APEN", Melding = $"Termin {terminId} er ikke apen for beregning." });
        }
        catch (MvaOppgjorAlleredeLastException)
        {
            return Conflict(new { Kode = "MVA_OPPGJOR_ALLEREDE_LAST", Melding = $"Oppgjoret for termin {terminId} er last." });
        }
    }

    [HttpPost("bokfor")]
    public async Task<ActionResult<MvaOppgjorDto>> BokforOppgjor(Guid terminId, CancellationToken ct)
    {
        try
        {
            var oppgjor = await _oppgjorService.BokforOppgjorAsync(terminId, ct);
            return Ok(oppgjor);
        }
        catch (MvaTerminIkkeFunnetException)
        {
            return NotFound(new { Kode = "MVA_TERMIN_IKKE_FUNNET", Melding = $"MVA-termin {terminId} finnes ikke." });
        }
        catch (MvaOppgjorManglerException)
        {
            return UnprocessableEntity(new { Kode = "MVA_OPPGJOR_MANGLER", Melding = $"Oppgjor for termin {terminId} er ikke beregnet." });
        }
        catch (MvaOppgjorAlleredeLastException)
        {
            return Conflict(new { Kode = "MVA_OPPGJOR_ALLEREDE_LAST", Melding = $"Oppgjoret for termin {terminId} er last." });
        }
        catch (MvaOppgjorAlleredeBokfortException)
        {
            return Conflict(new { Kode = "MVA_OPPGJOR_ALLEREDE_BOKFORT", Melding = $"Oppgjorsbilag for termin {terminId} er allerede bokfort." });
        }
    }

    [HttpGet]
    public async Task<ActionResult<MvaOppgjorDto>> HentOppgjor(Guid terminId, CancellationToken ct)
    {
        try
        {
            var oppgjor = await _oppgjorService.HentOppgjorAsync(terminId, ct);
            return Ok(oppgjor);
        }
        catch (MvaTerminIkkeFunnetException)
        {
            return NotFound(new { Kode = "MVA_TERMIN_IKKE_FUNNET", Melding = $"MVA-termin {terminId} finnes ikke." });
        }
        catch (MvaOppgjorManglerException)
        {
            return UnprocessableEntity(new { Kode = "MVA_OPPGJOR_MANGLER", Melding = $"Oppgjor for termin {terminId} er ikke beregnet." });
        }
    }

    [HttpGet("~/api/mva/terminer/{terminId:guid}/melding")]
    public async Task<ActionResult<MvaMeldingDto>> GenererMelding(Guid terminId, CancellationToken ct)
    {
        try
        {
            var melding = await _meldingService.GenererMvaMeldingAsync(terminId, ct);
            return Ok(melding);
        }
        catch (MvaTerminIkkeFunnetException)
        {
            return NotFound(new { Kode = "MVA_TERMIN_IKKE_FUNNET", Melding = $"MVA-termin {terminId} finnes ikke." });
        }
    }

    [HttpPost("~/api/mva/terminer/{terminId:guid}/melding/marker-innsendt")]
    public async Task<ActionResult> MarkerInnsendt(Guid terminId, CancellationToken ct)
    {
        try
        {
            await _meldingService.MarkerInnsendtAsync(terminId, ct);
            return NoContent();
        }
        catch (MvaTerminIkkeFunnetException)
        {
            return NotFound(new { Kode = "MVA_TERMIN_IKKE_FUNNET", Melding = $"MVA-termin {terminId} finnes ikke." });
        }
        catch (MvaAvstemmingIkkeGodkjentException)
        {
            return UnprocessableEntity(new { Kode = "MVA_AVSTEMMING_IKKE_GODKJENT", Melding = $"Avstemming for termin {terminId} er ikke godkjent." });
        }
        catch (MvaOppgjorManglerException)
        {
            return UnprocessableEntity(new { Kode = "MVA_OPPGJOR_MANGLER", Melding = $"Oppgjor for termin {terminId} er ikke beregnet." });
        }
    }
}
