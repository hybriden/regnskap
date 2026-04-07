namespace Regnskap.Application.Features.Rapportering;

using Regnskap.Domain.Features.Rapportering;

public interface IRapporteringRepository
{
    // Budsjett
    Task<Budsjett?> HentBudsjettLinjeAsync(string kontonummer, int ar, int periode, string versjon, CancellationToken ct = default);
    Task<List<Budsjett>> HentBudsjettForArAsync(int ar, string versjon, CancellationToken ct = default);
    Task LeggTilBudsjettAsync(Budsjett budsjett, CancellationToken ct = default);
    Task SlettBudsjettForArAsync(int ar, string versjon, CancellationToken ct = default);

    // Konfigurasjon
    Task<RapportKonfigurasjon?> HentKonfigurasjonAsync(CancellationToken ct = default);
    Task LagreKonfigurasjonAsync(RapportKonfigurasjon konfigurasjon, CancellationToken ct = default);

    // Rapportlogg
    Task LeggTilRapportLoggAsync(RapportLogg logg, CancellationToken ct = default);
    Task<List<RapportLogg>> HentRapportLoggerAsync(int ar, RapportType? type = null, CancellationToken ct = default);

    // Aggregeringssporringer (ytelsesoptimalisert)
    Task<List<KontoSaldoAggregat>> HentAggregerteSaldoerAsync(
        int ar, int fraPeriode, int tilPeriode,
        int? kontoklasse = null,
        CancellationToken ct = default);

    Task<List<DimensjonsSaldoAggregat>> HentDimensjonsSaldoerAsync(
        int ar, int fraPeriode, int tilPeriode,
        string dimensjon,
        string? kode = null,
        int? kontoklasse = null,
        CancellationToken ct = default);

    Task LagreEndringerAsync(CancellationToken ct = default);
}
