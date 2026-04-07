using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Rapportering;
using Regnskap.Domain.Features.Rapportering;

namespace Regnskap.Api.Features.Rapportering;

[ApiController]
[Authorize]
[Route("api/rapporter")]
public class RapportController : ControllerBase
{
    private readonly IRapporteringService _rapporteringService;

    public RapportController(IRapporteringService rapporteringService)
    {
        _rapporteringService = rapporteringService;
    }

    /// <summary>
    /// Genererer resultatregnskap ihht Regnskapsloven 3-2.
    /// </summary>
    [HttpGet("resultatregnskap")]
    public async Task<ActionResult<ResultatregnskapDto>> HentResultatregnskap(
        [FromQuery] int ar,
        [FromQuery] int fraPeriode = 1,
        [FromQuery] int tilPeriode = 12,
        [FromQuery] string format = "artsinndelt",
        [FromQuery] bool inkluderForrigeAr = true,
        CancellationToken ct = default)
    {
        var parsedFormat = format.ToLowerInvariant() == "funksjonsinndelt"
            ? ResultatregnskapFormat.Funksjonsinndelt
            : ResultatregnskapFormat.Artsinndelt;

        var resultat = await _rapporteringService.GenererResultatregnskapAsync(
            ar, fraPeriode, tilPeriode, parsedFormat, inkluderForrigeAr, ct);

        return Ok(resultat);
    }

    /// <summary>
    /// Genererer balanse ihht Regnskapsloven 3-2a.
    /// </summary>
    [HttpGet("balanse")]
    public async Task<ActionResult<BalanseDto>> HentBalanse(
        [FromQuery] int ar,
        [FromQuery] int periode = 12,
        [FromQuery] bool inkluderForrigeAr = true,
        CancellationToken ct = default)
    {
        var resultat = await _rapporteringService.GenererBalanseAsync(
            ar, periode, inkluderForrigeAr, ct);

        return Ok(resultat);
    }

    /// <summary>
    /// Genererer kontantstromoppstilling (indirekte metode).
    /// </summary>
    [HttpGet("kontantstrom")]
    public async Task<ActionResult<KontantstromDto>> HentKontantstrom(
        [FromQuery] int ar,
        [FromQuery] bool inkluderForrigeAr = true,
        CancellationToken ct = default)
    {
        var resultat = await _rapporteringService.GenererKontantstromAsync(
            ar, inkluderForrigeAr, ct);

        return Ok(resultat);
    }

    /// <summary>
    /// Genererer saldobalanse.
    /// </summary>
    [HttpGet("saldobalanse")]
    public async Task<ActionResult<SaldobalanseRapportDto>> HentSaldobalanse(
        [FromQuery] int ar,
        [FromQuery] int fraPeriode = 1,
        [FromQuery] int tilPeriode = 12,
        [FromQuery] bool inkluderNullsaldo = false,
        [FromQuery] int? kontoklasse = null,
        [FromQuery] bool gruppert = false,
        CancellationToken ct = default)
    {
        var resultat = await _rapporteringService.GenererSaldobalanseRapportAsync(
            ar, fraPeriode, tilPeriode, inkluderNullsaldo, kontoklasse, gruppert, ct);

        return Ok(resultat);
    }

    /// <summary>
    /// Genererer hovedboksutskrift (Bokforingsforskriften 3-1).
    /// </summary>
    [HttpGet("hovedboksutskrift")]
    public async Task<ActionResult<HovedboksutskriftDto>> HentHovedboksutskrift(
        [FromQuery] int ar,
        [FromQuery] int fraPeriode = 1,
        [FromQuery] int tilPeriode = 12,
        [FromQuery] string? fraKonto = null,
        [FromQuery] string? tilKonto = null,
        CancellationToken ct = default)
    {
        var resultat = await _rapporteringService.GenererHovedboksutskriftAsync(
            ar, fraPeriode, tilPeriode, fraKonto, tilKonto, ct);

        return Ok(resultat);
    }

    /// <summary>
    /// Genererer dimensjonsrapport per avdeling/prosjekt.
    /// </summary>
    [HttpGet("dimensjon")]
    public async Task<ActionResult<DimensjonsrapportDto>> HentDimensjonsrapport(
        [FromQuery] int ar,
        [FromQuery] int fraPeriode = 1,
        [FromQuery] int tilPeriode = 12,
        [FromQuery] string dimensjon = "avdeling",
        [FromQuery] string? kode = null,
        [FromQuery] int? kontoklasse = null,
        CancellationToken ct = default)
    {
        var resultat = await _rapporteringService.GenererDimensjonsrapportAsync(
            ar, fraPeriode, tilPeriode, dimensjon, kode, kontoklasse, ct);

        return Ok(resultat);
    }

    /// <summary>
    /// Genererer sammenligning mot forrige ar eller budsjett.
    /// </summary>
    [HttpGet("sammenligning")]
    public async Task<ActionResult<SammenligningDto>> HentSammenligning(
        [FromQuery] int ar,
        [FromQuery] int fraPeriode = 1,
        [FromQuery] int tilPeriode = 12,
        [FromQuery] string type = "forrige_ar",
        [FromQuery] string budsjettVersjon = "Opprinnelig",
        [FromQuery] int? kontoklasse = null,
        CancellationToken ct = default)
    {
        var resultat = await _rapporteringService.GenererSammenligningAsync(
            ar, fraPeriode, tilPeriode, type, budsjettVersjon, kontoklasse, ct);

        return Ok(resultat);
    }

    /// <summary>
    /// Beregner finansielle nokkeltall.
    /// </summary>
    [HttpGet("nokkeltall")]
    public async Task<ActionResult<NokkeltallRapportDto>> HentNokkeltall(
        [FromQuery] int ar,
        [FromQuery] int periode = 12,
        [FromQuery] bool inkluderForrigeAr = true,
        CancellationToken ct = default)
    {
        var resultat = await _rapporteringService.GenererNokkeltallAsync(
            ar, periode, inkluderForrigeAr, ct);

        return Ok(resultat);
    }
}
