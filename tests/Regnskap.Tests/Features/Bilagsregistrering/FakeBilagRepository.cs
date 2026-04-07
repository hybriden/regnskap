using Regnskap.Domain.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Tests.Features.Bilagsregistrering;

/// <summary>
/// In-memory fake repository for bilagsregistrering unit tests.
/// </summary>
public class FakeBilagRepository : IBilagRepository
{
    public List<BilagSerie> Serier { get; } = new();
    public List<BilagSerieNummer> SerieNummer { get; } = new();
    public List<Vedlegg> VedleggListe { get; } = new();
    public List<Bilag> BilagListe { get; } = new();
    public int SaveCount { get; private set; }

    // --- BilagSerie ---

    public Task<BilagSerie?> HentBilagSerieAsync(string kode, CancellationToken ct = default)
        => Task.FromResult(Serier.FirstOrDefault(s => s.Kode == kode));

    public Task<BilagSerie?> HentBilagSerieMedIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Serier.FirstOrDefault(s => s.Id == id));

    public Task<List<BilagSerie>> HentAlleBilagSerierAsync(CancellationToken ct = default)
        => Task.FromResult(Serier.OrderBy(s => s.Kode).ToList());

    public Task LeggTilBilagSerieAsync(BilagSerie serie, CancellationToken ct = default)
    {
        Serier.Add(serie);
        return Task.CompletedTask;
    }

    // --- BilagSerieNummer ---

    public Task<BilagSerieNummer?> HentSerieNummerAsync(string serieKode, int ar, CancellationToken ct = default)
        => Task.FromResult(SerieNummer.FirstOrDefault(s => s.SerieKode == serieKode && s.Ar == ar));

    public Task LeggTilSerieNummerAsync(BilagSerieNummer serieNummer, CancellationToken ct = default)
    {
        SerieNummer.Add(serieNummer);
        return Task.CompletedTask;
    }

    // --- Vedlegg ---

    public Task<Vedlegg?> HentVedleggAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(VedleggListe.FirstOrDefault(v => v.Id == id && !v.IsDeleted));

    public Task<List<Vedlegg>> HentVedleggForBilagAsync(Guid bilagId, CancellationToken ct = default)
        => Task.FromResult(VedleggListe.Where(v => v.BilagId == bilagId && !v.IsDeleted)
            .OrderBy(v => v.Rekkefolge).ToList());

    public Task LeggTilVedleggAsync(Vedlegg vedlegg, CancellationToken ct = default)
    {
        VedleggListe.Add(vedlegg);
        return Task.CompletedTask;
    }

    // --- Utvidet Bilag ---

    public Task<Bilag?> HentBilagMedSerieAsync(string serieKode, int ar, int serieNummer, CancellationToken ct = default)
        => Task.FromResult(BilagListe.FirstOrDefault(b =>
            b.SerieKode == serieKode && b.Ar == ar && b.SerieNummer == serieNummer));

    public Task<(List<Bilag> Data, int TotaltAntall)> SokBilagAsync(BilagSokParametre p, CancellationToken ct = default)
    {
        var query = BilagListe.AsEnumerable();
        if (p.Ar.HasValue) query = query.Where(b => b.Ar == p.Ar.Value);
        if (p.ErBokfort.HasValue) query = query.Where(b => b.ErBokfort == p.ErBokfort.Value);
        if (p.ErTilbakfort.HasValue) query = query.Where(b => b.ErTilbakfort == p.ErTilbakfort.Value);
        if (!string.IsNullOrEmpty(p.Beskrivelse)) query = query.Where(b => b.Beskrivelse.Contains(p.Beskrivelse));
        var list = query.ToList();
        return Task.FromResult((list.Skip((p.Side - 1) * p.Antall).Take(p.Antall).ToList(), list.Count));
    }

    // --- Generelt ---

    public Task LagreEndringerAsync(CancellationToken ct = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
