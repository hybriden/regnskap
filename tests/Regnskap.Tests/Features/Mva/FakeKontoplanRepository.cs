using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Tests.Features.Mva;

/// <summary>
/// Minimal fake for IKontoplanRepository, only implements methods needed by MVA tests.
/// </summary>
public class FakeKontoplanRepository : IKontoplanRepository
{
    public List<MvaKode> MvaKoder { get; } = new();

    public Task<List<MvaKode>> HentAlleMvaKoderAsync(bool? erAktiv = null, MvaRetning? retning = null, CancellationToken ct = default)
    {
        var query = MvaKoder.AsEnumerable();
        if (erAktiv.HasValue) query = query.Where(k => k.ErAktiv == erAktiv.Value);
        if (retning.HasValue) query = query.Where(k => k.Retning == retning.Value);
        return Task.FromResult(query.ToList());
    }

    // --- Not implemented for MVA tests ---

    public Task<List<Kontogruppe>> HentAlleKontogrupperAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<Kontogruppe?> HentKontogruppeAsync(int gruppekode, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<Konto?> HentKontoAsync(string kontonummer, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<Konto?> HentKontoMedDetaljerAsync(string kontonummer, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<bool> KontoFinnesAsync(string kontonummer, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<List<Konto>> HentKontoerAsync(
        int? kontoklasse = null, Kontotype? kontotype = null, int? gruppekode = null,
        bool? erAktiv = null, bool? erBokforbar = null, string? sok = null,
        int side = 1, int antall = 50, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<int> TellKontoerAsync(
        int? kontoklasse = null, Kontotype? kontotype = null, int? gruppekode = null,
        bool? erAktiv = null, bool? erBokforbar = null, string? sok = null,
        CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<List<Konto>> SokKontoerAsync(string query, int antall = 10, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task LeggTilKontoAsync(Konto konto, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<bool> KontoHarPosteringerAsync(string kontonummer, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<bool> KontoHarAktiveUnderkontoerAsync(string kontonummer, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<MvaKode?> HentMvaKodeAsync(string kode, CancellationToken ct = default)
        => Task.FromResult(MvaKoder.FirstOrDefault(k => k.Kode == kode));

    public Task<bool> MvaKodeFinnesAsync(string kode, CancellationToken ct = default)
        => Task.FromResult(MvaKoder.Any(k => k.Kode == kode));

    public Task LeggTilMvaKodeAsync(MvaKode mvaKode, CancellationToken ct = default)
    {
        MvaKoder.Add(mvaKode);
        return Task.CompletedTask;
    }

    public Task LagreEndringerAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
