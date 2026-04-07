using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Application.Features.Hovedbok;

/// <summary>
/// Service for hovedbok-operasjoner: saldooppslag, kontoutskrift, saldobalanse.
/// </summary>
public interface IHovedbokService
{
    // --- Kontoutskrift ---
    Task<KontoutskriftDto> HentKontoutskriftAsync(
        string kontonummer, DateOnly fraDato, DateOnly tilDato,
        int side = 1, int antall = 100, CancellationToken ct = default);

    // --- Saldobalanse ---
    Task<SaldobalanseDto> HentSaldobalanseAsync(
        int ar, int periode,
        bool inkluderNullsaldo = false,
        int? kontoklasse = null,
        CancellationToken ct = default);

    // --- Saldooppslag ---
    Task<KontoSaldoOppslagDto> HentKontoSaldoAsync(
        string kontonummer, int ar,
        int? fraPeriode = null, int? tilPeriode = null,
        CancellationToken ct = default);
}
