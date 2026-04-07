namespace Regnskap.Application.Features.Rapportering;

using Regnskap.Domain.Features.Rapportering;

public interface IRapporteringService
{
    Task<ResultatregnskapDto> GenererResultatregnskapAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        ResultatregnskapFormat format = ResultatregnskapFormat.Artsinndelt,
        bool inkluderForrigeAr = true,
        CancellationToken ct = default);

    Task<BalanseDto> GenererBalanseAsync(
        int ar, int periode = 12,
        bool inkluderForrigeAr = true,
        CancellationToken ct = default);

    Task<KontantstromDto> GenererKontantstromAsync(
        int ar,
        bool inkluderForrigeAr = true,
        CancellationToken ct = default);

    Task<SaldobalanseRapportDto> GenererSaldobalanseRapportAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        bool inkluderNullsaldo = false,
        int? kontoklasse = null,
        bool gruppert = false,
        CancellationToken ct = default);

    Task<HovedboksutskriftDto> GenererHovedboksutskriftAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        string? fraKonto = null, string? tilKonto = null,
        CancellationToken ct = default);

    Task<DimensjonsrapportDto> GenererDimensjonsrapportAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        string dimensjon = "avdeling",
        string? kode = null,
        int? kontoklasse = null,
        CancellationToken ct = default);

    Task<SammenligningDto> GenererSammenligningAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        string type = "forrige_ar",
        string budsjettVersjon = "Opprinnelig",
        int? kontoklasse = null,
        CancellationToken ct = default);

    Task<NokkeltallRapportDto> GenererNokkeltallAsync(
        int ar, int periode = 12,
        bool inkluderForrigeAr = true,
        CancellationToken ct = default);
}
