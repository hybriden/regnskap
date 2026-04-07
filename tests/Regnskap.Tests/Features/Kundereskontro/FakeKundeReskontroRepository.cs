using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kundereskontro;

namespace Regnskap.Tests.Features.Kundereskontro;

public class FakeKundeReskontroRepository : IKundeReskontroRepository
{
    public List<Kunde> Kunder { get; } = new();
    public List<KundeFaktura> Fakturaer { get; } = new();
    public List<KundeInnbetaling> Innbetalinger { get; } = new();
    public List<Purring> PurringerList { get; } = new();
    private int _nesteFakturanummer = 1;

    // --- Kunde ---

    public Task<Kunde?> HentKundeAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Kunder.FirstOrDefault(k => k.Id == id && !k.IsDeleted));

    public Task<Kunde?> HentKundeMedNummerAsync(string kundenummer, CancellationToken ct = default)
        => Task.FromResult(Kunder.FirstOrDefault(k => k.Kundenummer == kundenummer && !k.IsDeleted));

    public Task<bool> KundenummerEksistererAsync(string kundenummer, CancellationToken ct = default)
        => Task.FromResult(Kunder.Any(k => k.Kundenummer == kundenummer && !k.IsDeleted));

    public Task LeggTilKundeAsync(Kunde kunde, CancellationToken ct = default)
    {
        Kunder.Add(kunde);
        return Task.CompletedTask;
    }

    public Task OppdaterKundeAsync(Kunde kunde, CancellationToken ct = default)
        => Task.CompletedTask;

    // --- Faktura ---

    public Task<KundeFaktura?> HentFakturaAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Fakturaer.FirstOrDefault(f => f.Id == id && !f.IsDeleted));

    public Task<KundeFaktura?> HentFakturaMedKidAsync(string kidNummer, CancellationToken ct = default)
        => Task.FromResult(Fakturaer.FirstOrDefault(f => f.KidNummer == kidNummer && !f.IsDeleted));

    public Task<int> NesteNummer(CancellationToken ct = default)
        => Task.FromResult(_nesteFakturanummer++);

    public Task LeggTilFakturaAsync(KundeFaktura faktura, CancellationToken ct = default)
    {
        Fakturaer.Add(faktura);
        return Task.CompletedTask;
    }

    public Task OppdaterFakturaAsync(KundeFaktura faktura, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<List<KundeFaktura>> HentApnePosterAsync(DateOnly? dato = null, CancellationToken ct = default)
    {
        var q = Fakturaer.Where(f => !f.IsDeleted && f.GjenstaendeBelop.Verdi != 0 &&
            f.Status != KundeFakturaStatus.Betalt && f.Status != KundeFakturaStatus.Tap);
        if (dato.HasValue)
            q = q.Where(f => f.Fakturadato <= dato.Value);
        return Task.FromResult(q.ToList());
    }

    public Task<List<KundeFaktura>> HentForfalteFakturaerAsync(DateOnly dato, int minimumDagerForfalt, CancellationToken ct = default)
    {
        var grenseDato = dato.AddDays(-minimumDagerForfalt);
        return Task.FromResult(Fakturaer.Where(f => !f.IsDeleted &&
            f.Forfallsdato <= grenseDato &&
            f.GjenstaendeBelop.Verdi > 0 &&
            f.Status != KundeFakturaStatus.Betalt &&
            f.Status != KundeFakturaStatus.Tap).ToList());
    }

    public Task<(List<KundeFaktura> Data, int TotaltAntall)> SokFakturaerAsync(Guid? kundeId, KundeFakturaStatus? status, int side, int antall, CancellationToken ct = default)
    {
        var q = Fakturaer.Where(f => !f.IsDeleted).AsEnumerable();
        if (kundeId.HasValue) q = q.Where(f => f.KundeId == kundeId.Value);
        if (status.HasValue) q = q.Where(f => f.Status == status.Value);
        var list = q.ToList();
        return Task.FromResult((list.Skip((side - 1) * antall).Take(antall).ToList(), list.Count));
    }

    public Task<List<KundeFaktura>> HentFakturaerForKundeAsync(Guid kundeId, DateOnly? fraDato, DateOnly? tilDato, CancellationToken ct = default)
    {
        var q = Fakturaer.Where(f => f.KundeId == kundeId && !f.IsDeleted).AsEnumerable();
        if (fraDato.HasValue) q = q.Where(f => f.Fakturadato >= fraDato.Value);
        if (tilDato.HasValue) q = q.Where(f => f.Fakturadato <= tilDato.Value);
        return Task.FromResult(q.OrderBy(f => f.Fakturadato).ToList());
    }

    // --- Innbetaling ---

    public Task LeggTilInnbetalingAsync(KundeInnbetaling innbetaling, CancellationToken ct = default)
    {
        Innbetalinger.Add(innbetaling);
        return Task.CompletedTask;
    }

    // --- Purring ---

    public Task LeggTilPurringAsync(Purring purring, CancellationToken ct = default)
    {
        PurringerList.Add(purring);
        return Task.CompletedTask;
    }

    public Task<Purring?> HentSistePurringAsync(Guid fakturaId, CancellationToken ct = default)
        => Task.FromResult(PurringerList
            .Where(p => p.KundeFakturaId == fakturaId)
            .OrderByDescending(p => p.Purringsdato)
            .FirstOrDefault());

    public Task<(List<Kunde> Data, int TotaltAntall)> SokKunderAsync(string? query, int side, int antall, CancellationToken ct = default)
    {
        var q = Kunder.Where(k => !k.IsDeleted).AsEnumerable();
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(k => k.Kundenummer.Contains(query) || k.Navn.Contains(query));
        var list = q.ToList();
        return Task.FromResult((list.Skip((side - 1) * antall).Take(antall).ToList(), list.Count));
    }

    public Task<List<Purring>> HentPurringerAsync(int side, int antall, CancellationToken ct = default)
        => Task.FromResult(PurringerList.Skip((side - 1) * antall).Take(antall).ToList());

    public Task LagreEndringerAsync(CancellationToken ct = default) => Task.CompletedTask;
}
