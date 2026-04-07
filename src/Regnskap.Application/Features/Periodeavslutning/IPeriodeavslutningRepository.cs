namespace Regnskap.Application.Features.Periodeavslutning;

using Regnskap.Domain.Features.Periodeavslutning;

public interface IPeriodeavslutningRepository
{
    // Anleggsmidler
    Task<Anleggsmiddel?> HentAnleggsmiddelAsync(Guid id, CancellationToken ct = default);
    Task<List<Anleggsmiddel>> HentAnleggsmidlerAsync(
        bool? aktive = null, string? kontonummer = null, CancellationToken ct = default);
    Task LeggTilAnleggsmiddelAsync(Anleggsmiddel anleggsmiddel, CancellationToken ct = default);
    Task<bool> AvskrivningFinnesAsync(Guid anleggsmiddelId, int ar, int periode, CancellationToken ct = default);
    Task LeggTilAvskrivningHistorikkAsync(AvskrivningHistorikk historikk, CancellationToken ct = default);

    // Periodiseringer
    Task<Periodisering?> HentPeriodiseringAsync(Guid id, CancellationToken ct = default);
    Task<List<Periodisering>> HentPeriodiseringerAsync(bool? aktive = null, CancellationToken ct = default);
    Task<List<Periodisering>> HentAktivePeriodiseringerForPeriodeAsync(
        int ar, int periode, CancellationToken ct = default);
    Task LeggTilPeriodiseringAsync(Periodisering periodisering, CancellationToken ct = default);
    Task<bool> PeriodiseringsHistorikkFinnesAsync(
        Guid periodiseringId, int ar, int periode, CancellationToken ct = default);
    Task LeggTilPeriodiseringsHistorikkAsync(
        PeriodiseringsHistorikk historikk, CancellationToken ct = default);

    // Logg
    Task LeggTilPeriodeLukkingLoggAsync(PeriodeLukkingLogg logg, CancellationToken ct = default);
    Task<List<PeriodeLukkingLogg>> HentPeriodeLukkingLoggerAsync(
        int ar, int periode, CancellationToken ct = default);

    // Arsavslutning
    Task<ArsavslutningStatus?> HentArsavslutningStatusAsync(int ar, CancellationToken ct = default);
    Task LagreArsavslutningStatusAsync(ArsavslutningStatus status, CancellationToken ct = default);

    Task LagreEndringerAsync(CancellationToken ct = default);
}
