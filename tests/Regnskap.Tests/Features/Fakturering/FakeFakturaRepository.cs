using Regnskap.Domain.Features.Fakturering;

namespace Regnskap.Tests.Features.Fakturering;

public class FakeFakturaRepository : IFakturaRepository
{
    public List<Faktura> Fakturaer { get; } = new();
    public Dictionary<(int, FakturaDokumenttype), int> Nummerserier { get; } = new();
    public Selskapsinfo? Selskapsinfo { get; set; }

    public Task<Faktura?> HentAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(Fakturaer.FirstOrDefault(f => f.Id == id));
    }

    public Task<Faktura?> HentMedLinjerAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(Fakturaer.FirstOrDefault(f => f.Id == id));
    }

    public Task LeggTilAsync(Faktura faktura, CancellationToken ct = default)
    {
        Fakturaer.Add(faktura);
        return Task.CompletedTask;
    }

    public Task<int> NesteNummerAsync(int aar, FakturaDokumenttype type, CancellationToken ct = default)
    {
        var key = (aar, type);
        if (!Nummerserier.ContainsKey(key))
            Nummerserier[key] = 0;
        Nummerserier[key]++;
        return Task.FromResult(Nummerserier[key]);
    }

    public Task<Selskapsinfo?> HentSelskapsinfoAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Selskapsinfo);
    }

    public Task<List<Faktura>> SokAsync(FakturaSokFilter filter, CancellationToken ct = default)
    {
        var query = Fakturaer.AsEnumerable();
        if (filter.Status.HasValue)
            query = query.Where(f => f.Status == filter.Status.Value);
        return Task.FromResult(query.ToList());
    }

    public Task<int> TellAsync(FakturaSokFilter filter, CancellationToken ct = default)
    {
        return Task.FromResult(Fakturaer.Count);
    }

    public Task LagreEndringerAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
