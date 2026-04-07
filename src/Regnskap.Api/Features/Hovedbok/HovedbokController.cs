using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Hovedbok;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Api.Features.Hovedbok;

[ApiController]
[Authorize]
public class HovedbokController : ControllerBase
{
    private readonly IHovedbokService _hovedbokService;

    public HovedbokController(IHovedbokService hovedbokService)
    {
        _hovedbokService = hovedbokService;
    }

    /// <summary>
    /// Kontoutskrift (kontospesifikasjon) — alle posteringer for en konto i et datointervall.
    /// Bokforingsforskriften 3-1.
    /// </summary>
    [HttpGet("api/kontoutskrift/{kontonummer}")]
    public async Task<ActionResult<KontoutskriftDto>> HentKontoutskrift(
        string kontonummer,
        [FromQuery] DateOnly? fraDato,
        [FromQuery] DateOnly? tilDato,
        [FromQuery] int side = 1,
        [FromQuery] int antall = 100,
        CancellationToken ct = default)
    {
        var fra = fraDato ?? new DateOnly(DateTime.UtcNow.Year, 1, 1);
        var til = tilDato ?? DateOnly.FromDateTime(DateTime.UtcNow);

        try
        {
            var result = await _hovedbokService.HentKontoutskriftAsync(
                kontonummer, fra, til, side, antall, ct);
            return Ok(result);
        }
        catch (KontoIkkeFunnetException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Saldobalanse for alle kontoer med aktivitet i en gitt periode.
    /// </summary>
    [HttpGet("api/saldobalanse/{ar:int}/{periode:int}")]
    public async Task<ActionResult<SaldobalanseDto>> HentSaldobalanse(
        int ar, int periode,
        [FromQuery] bool inkluderNullsaldo = false,
        [FromQuery] int? kontoklasse = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _hovedbokService.HentSaldobalanseAsync(
                ar, periode, inkluderNullsaldo, kontoklasse, ct);
            return Ok(result);
        }
        catch (PeriodeIkkeFunnetException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Saldooppslag for en konto, eventuelt over et periodespenn.
    /// </summary>
    [HttpGet("api/saldo/{kontonummer}")]
    public async Task<ActionResult<KontoSaldoOppslagDto>> HentKontoSaldo(
        string kontonummer,
        [FromQuery] int? ar,
        [FromQuery] int? fraPeriode,
        [FromQuery] int? tilPeriode,
        CancellationToken ct = default)
    {
        var aar = ar ?? DateTime.UtcNow.Year;

        try
        {
            var result = await _hovedbokService.HentKontoSaldoAsync(
                kontonummer, aar, fraPeriode, tilPeriode, ct);
            return Ok(result);
        }
        catch (KontoIkkeFunnetException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
