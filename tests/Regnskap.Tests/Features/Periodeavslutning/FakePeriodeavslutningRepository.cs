using Regnskap.Application.Features.Periodeavslutning;
using Regnskap.Domain.Features.Periodeavslutning;

namespace Regnskap.Tests.Features.Periodeavslutning;

public class FakePeriodeavslutningRepository : IPeriodeavslutningRepository
{
    public List<Anleggsmiddel> Anleggsmidler { get; } = new();
    public List<AvskrivningHistorikk> AvskrivningHistorikker { get; } = new();
    public List<Periodisering> Periodiseringer { get; } = new();
    public List<PeriodiseringsHistorikk> PeriodiseringsHistorikker { get; } = new();
    public List<PeriodeLukkingLogg> Logger { get; } = new();
    public List<ArsavslutningStatus> ArsavslutningStatuser { get; } = new();
    public int SaveChangesCalled { get; private set; }

    // Anleggsmidler

    public Task<Anleggsmiddel?> HentAnleggsmiddelAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(Anleggsmidler.FirstOrDefault(a => a.Id == id && !a.IsDeleted));
    }

    public Task<List<Anleggsmiddel>> HentAnleggsmidlerAsync(
        bool? aktive = null, string? kontonummer = null, CancellationToken ct = default)
    {
        var query = Anleggsmidler.Where(a => !a.IsDeleted).AsEnumerable();
        if (aktive.HasValue) query = query.Where(a => a.ErAktivt == aktive.Value);
        if (!string.IsNullOrEmpty(kontonummer)) query = query.Where(a => a.BalanseKontonummer == kontonummer);
        return Task.FromResult(query.ToList());
    }

    public Task LeggTilAnleggsmiddelAsync(Anleggsmiddel anleggsmiddel, CancellationToken ct = default)
    {
        Anleggsmidler.Add(anleggsmiddel);
        return Task.CompletedTask;
    }

    public Task<bool> AvskrivningFinnesAsync(Guid anleggsmiddelId, int ar, int periode, CancellationToken ct = default)
    {
        return Task.FromResult(AvskrivningHistorikker
            .Any(a => a.AnleggsmiddelId == anleggsmiddelId && a.Ar == ar && a.Periode == periode && !a.IsDeleted));
    }

    public Task LeggTilAvskrivningHistorikkAsync(AvskrivningHistorikk historikk, CancellationToken ct = default)
    {
        AvskrivningHistorikker.Add(historikk);
        return Task.CompletedTask;
    }

    // Periodiseringer

    public Task<Periodisering?> HentPeriodiseringAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(Periodiseringer.FirstOrDefault(p => p.Id == id && !p.IsDeleted));
    }

    public Task<List<Periodisering>> HentPeriodiseringerAsync(bool? aktive = null, CancellationToken ct = default)
    {
        var query = Periodiseringer.Where(p => !p.IsDeleted).AsEnumerable();
        if (aktive.HasValue) query = query.Where(p => p.ErAktiv == aktive.Value);
        return Task.FromResult(query.ToList());
    }

    public Task<List<Periodisering>> HentAktivePeriodiseringerForPeriodeAsync(
        int ar, int periode, CancellationToken ct = default)
    {
        var periodeNr = ar * 12 + periode;
        var result = Periodiseringer
            .Where(p => !p.IsDeleted && p.ErAktiv
                && (p.FraAr * 12 + p.FraPeriode) <= periodeNr
                && (p.TilAr * 12 + p.TilPeriode) >= periodeNr)
            .ToList();
        return Task.FromResult(result);
    }

    public Task LeggTilPeriodiseringAsync(Periodisering periodisering, CancellationToken ct = default)
    {
        Periodiseringer.Add(periodisering);
        return Task.CompletedTask;
    }

    public Task<bool> PeriodiseringsHistorikkFinnesAsync(
        Guid periodiseringId, int ar, int periode, CancellationToken ct = default)
    {
        return Task.FromResult(PeriodiseringsHistorikker
            .Any(h => h.PeriodiseringId == periodiseringId && h.Ar == ar && h.Periode == periode && !h.IsDeleted));
    }

    public Task LeggTilPeriodiseringsHistorikkAsync(
        PeriodiseringsHistorikk historikk, CancellationToken ct = default)
    {
        PeriodiseringsHistorikker.Add(historikk);
        return Task.CompletedTask;
    }

    // Logg

    public Task LeggTilPeriodeLukkingLoggAsync(PeriodeLukkingLogg logg, CancellationToken ct = default)
    {
        Logger.Add(logg);
        return Task.CompletedTask;
    }

    public Task<List<PeriodeLukkingLogg>> HentPeriodeLukkingLoggerAsync(
        int ar, int periode, CancellationToken ct = default)
    {
        return Task.FromResult(Logger.Where(l => l.Ar == ar && l.Periode == periode).ToList());
    }

    // Arsavslutning

    public Task<ArsavslutningStatus?> HentArsavslutningStatusAsync(int ar, CancellationToken ct = default)
    {
        return Task.FromResult(ArsavslutningStatuser.FirstOrDefault(s => s.Ar == ar && !s.IsDeleted));
    }

    public Task LagreArsavslutningStatusAsync(ArsavslutningStatus status, CancellationToken ct = default)
    {
        var existing = ArsavslutningStatuser.FirstOrDefault(s => s.Ar == status.Ar);
        if (existing == null)
            ArsavslutningStatuser.Add(status);
        return Task.CompletedTask;
    }

    public Task LagreEndringerAsync(CancellationToken ct = default)
    {
        SaveChangesCalled++;
        return Task.CompletedTask;
    }
}
