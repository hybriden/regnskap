using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Api.Features.Kontoplan.Dtos;
using Regnskap.Application.Features.Kontoplan;
using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Api.Features.Kontoplan;

[ApiController]
[Authorize]
[Route("api/mva-koder")]
public class MvaKodeController : ControllerBase
{
    private readonly IMvaKodeService _mvaKodeService;

    public MvaKodeController(IMvaKodeService mvaKodeService)
    {
        _mvaKodeService = mvaKodeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<MvaKodeListeDto>>> HentAlle(
        [FromQuery] bool? erAktiv,
        [FromQuery] string? retning,
        CancellationToken ct = default)
    {
        MvaRetning? retningEnum = null;
        if (!string.IsNullOrEmpty(retning) && Enum.TryParse<MvaRetning>(retning, true, out var parsed))
            retningEnum = parsed;

        var koder = await _mvaKodeService.HentAlleMvaKoderAsync(erAktiv, retningEnum, ct);
        var dtos = koder.Select(MapToListeDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("{kode}")]
    public async Task<ActionResult<MvaKodeListeDto>> HentEnkel(string kode, CancellationToken ct)
    {
        var mvaKode = await _mvaKodeService.HentMvaKodeAsync(kode, ct);
        if (mvaKode is null)
            return NotFound();

        return Ok(MapToListeDto(mvaKode));
    }

    [HttpPost]
    public async Task<ActionResult<MvaKodeListeDto>> Opprett([FromBody] MvaKodeOpprettRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<MvaRetning>(request.Retning, true, out var retning))
            return BadRequest(new { kode = "UGYLDIG_RETNING", melding = "Ugyldig MVA-retning." });

        try
        {
            var mvaKode = await _mvaKodeService.OpprettMvaKodeAsync(new OpprettMvaKodeRequest(
                request.Kode,
                request.Beskrivelse,
                request.BeskrivelseEn,
                request.StandardTaxCode,
                request.Sats,
                retning,
                request.UtgaendeKontonummer,
                request.InngaendeKontonummer
            ), ct);

            return CreatedAtAction(nameof(HentEnkel),
                new { kode = mvaKode.Kode },
                MapToListeDto(mvaKode));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { kode = "VALIDERING_FEIL", melding = ex.Message });
        }
        catch (KontoIkkeFunnetException ex)
        {
            return BadRequest(new { kode = "KONTO_IKKE_FUNNET", melding = ex.Message });
        }
    }

    [HttpPut("{kode}")]
    public async Task<ActionResult> Oppdater(string kode, [FromBody] MvaKodeOppdaterRequest request, CancellationToken ct)
    {
        try
        {
            await _mvaKodeService.OppdaterMvaKodeAsync(kode, new OppdaterMvaKodeRequest(
                request.Beskrivelse,
                request.BeskrivelseEn,
                request.Sats,
                request.ErAktiv,
                request.UtgaendeKontonummer,
                request.InngaendeKontonummer
            ), ct);
            return Ok();
        }
        catch (MvaKodeIkkeFunnetException)
        {
            return NotFound();
        }
        catch (KontoIkkeFunnetException ex)
        {
            return BadRequest(new { kode = "KONTO_IKKE_FUNNET", melding = ex.Message });
        }
    }

    private static MvaKodeListeDto MapToListeDto(MvaKode m) => new(
        m.Id,
        m.Kode,
        m.Beskrivelse,
        m.BeskrivelseEn,
        m.StandardTaxCode,
        m.Sats,
        m.Retning.ToString(),
        m.UtgaendeKonto?.Kontonummer,
        m.InngaendeKonto?.Kontonummer,
        m.ErAktiv,
        m.ErSystemkode
    );
}
