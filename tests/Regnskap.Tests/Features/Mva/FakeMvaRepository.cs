using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Mva;

namespace Regnskap.Tests.Features.Mva;

/// <summary>
/// In-memory fake for unit tests. Does not require EF Core.
/// </summary>
public class FakeMvaRepository : IMvaRepository
{
    public List<MvaTermin> Terminer { get; } = new();
    public List<MvaOppgjor> Oppgjor { get; } = new();
    public List<MvaAvstemming> Avstemminger { get; } = new();
    public List<MvaAggregeringDto> Aggregeringer { get; } = new();
    public List<MvaPosteringDetalj> Posteringsdetaljer { get; } = new();
    public List<MvaKontoSaldoDto> KontoSaldoer { get; } = new();
    public List<MvaKontoBeregnetDto> BeregnetPerKonto { get; } = new();

    // --- Terminer ---

    public Task<List<MvaTermin>> HentTerminerForArAsync(int ar, CancellationToken ct = default)
        => Task.FromResult(Terminer.Where(t => t.Ar == ar && !t.IsDeleted).OrderBy(t => t.Termin).ToList());

    public Task<MvaTermin?> HentTerminAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Terminer.FirstOrDefault(t => t.Id == id && !t.IsDeleted));

    public Task<MvaTermin?> HentTerminForDatoAsync(DateOnly dato, CancellationToken ct = default)
        => Task.FromResult(Terminer.FirstOrDefault(t => t.FraDato <= dato && t.TilDato >= dato && !t.IsDeleted));

    public Task<MvaTermin?> HentTerminAsync(int ar, int termin, CancellationToken ct = default)
        => Task.FromResult(Terminer.FirstOrDefault(t => t.Ar == ar && t.Termin == termin && !t.IsDeleted));

    public Task<bool> TerminerFinnesForArAsync(int ar, CancellationToken ct = default)
        => Task.FromResult(Terminer.Any(t => t.Ar == ar && !t.IsDeleted));

    public Task LeggTilTerminAsync(MvaTermin termin, CancellationToken ct = default)
    {
        Terminer.Add(termin);
        return Task.CompletedTask;
    }

    public Task LeggTilTerminerAsync(IEnumerable<MvaTermin> terminer, CancellationToken ct = default)
    {
        Terminer.AddRange(terminer);
        return Task.CompletedTask;
    }

    // --- Oppgjor ---

    public Task<MvaOppgjor?> HentOppgjorForTerminAsync(Guid terminId, CancellationToken ct = default)
        => Task.FromResult(Oppgjor.FirstOrDefault(o => o.MvaTerminId == terminId && !o.IsDeleted));

    public Task<MvaOppgjor?> HentOppgjorMedLinjerAsync(Guid oppgjorId, CancellationToken ct = default)
        => Task.FromResult(Oppgjor.FirstOrDefault(o => o.Id == oppgjorId && !o.IsDeleted));

    public Task LeggTilOppgjorAsync(MvaOppgjor oppgjor, CancellationToken ct = default)
    {
        Oppgjor.Add(oppgjor);
        return Task.CompletedTask;
    }

    // --- Avstemming ---

    public Task<MvaAvstemming?> HentSisteAvstemmingForTerminAsync(Guid terminId, CancellationToken ct = default)
        => Task.FromResult(Avstemminger
            .Where(a => a.MvaTerminId == terminId && !a.IsDeleted)
            .OrderByDescending(a => a.AvstemmingTidspunkt)
            .FirstOrDefault());

    public Task<List<MvaAvstemming>> HentAvstemmingshistorikkAsync(Guid terminId, CancellationToken ct = default)
        => Task.FromResult(Avstemminger
            .Where(a => a.MvaTerminId == terminId && !a.IsDeleted)
            .OrderByDescending(a => a.AvstemmingTidspunkt)
            .ToList());

    public Task LeggTilAvstemmingAsync(MvaAvstemming avstemming, CancellationToken ct = default)
    {
        Avstemminger.Add(avstemming);
        return Task.CompletedTask;
    }

    // --- Posteringsaggregering ---

    public Task<List<MvaAggregeringDto>> HentMvaAggregertForPeriodeAsync(
        DateOnly fraDato, DateOnly tilDato, CancellationToken ct = default)
        => Task.FromResult(Aggregeringer.ToList());

    public Task<List<MvaPosteringDetalj>> HentMvaPosteringerForPeriodeAsync(
        DateOnly fraDato, DateOnly tilDato, string? mvaKode = null, CancellationToken ct = default)
    {
        var result = mvaKode != null
            ? Posteringsdetaljer.Where(p => p.MvaKode == mvaKode).ToList()
            : Posteringsdetaljer.ToList();
        return Task.FromResult(result);
    }

    public Task<List<MvaKontoSaldoDto>> HentMvaKontoSaldoerAsync(
        int ar, int fraPeriode, int tilPeriode, CancellationToken ct = default)
        => Task.FromResult(KontoSaldoer.ToList());

    public Task<List<MvaKontoBeregnetDto>> HentBeregnetMvaPerKontoAsync(
        DateOnly fraDato, DateOnly tilDato, CancellationToken ct = default)
        => Task.FromResult(BeregnetPerKonto.ToList());

    public Task LagreEndringerAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
