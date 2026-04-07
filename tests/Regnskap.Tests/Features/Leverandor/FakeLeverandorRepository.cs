using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Leverandorreskontro;

namespace Regnskap.Tests.Features.Leverandor;

public class FakeLeverandorRepository : ILeverandorReskontroRepository
{
    public List<Domain.Features.Leverandorreskontro.Leverandor> Leverandorer { get; } = new();
    public List<LeverandorFaktura> Fakturaer { get; } = new();
    public List<Betalingsforslag> Betalingsforslag { get; } = new();
    public List<LeverandorBetaling> Betalinger { get; } = new();
    private int _nesteInternNummer = 1;
    private int _nesteForslagsnummer = 1;

    public Task<Domain.Features.Leverandorreskontro.Leverandor?> HentLeverandorAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Leverandorer.FirstOrDefault(l => l.Id == id && !l.IsDeleted));

    public Task<Domain.Features.Leverandorreskontro.Leverandor?> HentLeverandorMedNummerAsync(string leverandornummer, CancellationToken ct = default)
        => Task.FromResult(Leverandorer.FirstOrDefault(l => l.Leverandornummer == leverandornummer && !l.IsDeleted));

    public Task<bool> LeverandornummerEksistererAsync(string leverandornummer, CancellationToken ct = default)
        => Task.FromResult(Leverandorer.Any(l => l.Leverandornummer == leverandornummer && !l.IsDeleted));

    public Task<bool> OrganisasjonsnummerEksistererAsync(string organisasjonsnummer, CancellationToken ct = default)
        => Task.FromResult(Leverandorer.Any(l => l.Organisasjonsnummer == organisasjonsnummer && !l.IsDeleted));

    public Task LeggTilLeverandorAsync(Domain.Features.Leverandorreskontro.Leverandor leverandor, CancellationToken ct = default)
    {
        Leverandorer.Add(leverandor);
        return Task.CompletedTask;
    }

    public Task OppdaterLeverandorAsync(Domain.Features.Leverandorreskontro.Leverandor leverandor, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<List<Domain.Features.Leverandorreskontro.Leverandor>> SokLeverandorerAsync(string? query, int side, int antall, CancellationToken ct = default)
    {
        var q = Leverandorer.Where(l => !l.IsDeleted).AsEnumerable();
        if (!string.IsNullOrEmpty(query))
            q = q.Where(l => l.Navn.Contains(query) || l.Leverandornummer.Contains(query));
        return Task.FromResult(q.Skip((side - 1) * antall).Take(antall).ToList());
    }

    public Task<int> TellLeverandorerAsync(string? query, CancellationToken ct = default)
    {
        var q = Leverandorer.Where(l => !l.IsDeleted).AsEnumerable();
        if (!string.IsNullOrEmpty(query))
            q = q.Where(l => l.Navn.Contains(query) || l.Leverandornummer.Contains(query));
        return Task.FromResult(q.Count());
    }

    public Task<LeverandorFaktura?> HentFakturaAsync(Guid id, CancellationToken ct = default)
    {
        var f = Fakturaer.FirstOrDefault(f => f.Id == id && !f.IsDeleted);
        if (f != null) f.Leverandor = Leverandorer.First(l => l.Id == f.LeverandorId);
        return Task.FromResult(f);
    }

    public Task<LeverandorFaktura?> HentFakturaMedLinjerAsync(Guid id, CancellationToken ct = default)
        => HentFakturaAsync(id, ct);

    public Task<bool> EksternFakturaDuplikatAsync(Guid leverandorId, string eksternNummer, CancellationToken ct = default)
        => Task.FromResult(Fakturaer.Any(f =>
            f.LeverandorId == leverandorId && f.EksternFakturanummer == eksternNummer && !f.IsDeleted));

    public Task LeggTilFakturaAsync(LeverandorFaktura faktura, CancellationToken ct = default)
    {
        Fakturaer.Add(faktura);
        return Task.CompletedTask;
    }

    public Task OppdaterFakturaAsync(LeverandorFaktura faktura, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<int> NesteInternNummerAsync(CancellationToken ct = default)
        => Task.FromResult(_nesteInternNummer++);

    public Task<List<LeverandorFaktura>> HentApnePosterAsync(DateOnly? dato, CancellationToken ct = default)
    {
        var result = Fakturaer
            .Where(f => !f.IsDeleted && f.GjenstaendeBelop.Verdi > 0 &&
                        f.Status != FakturaStatus.Betalt && f.Status != FakturaStatus.Kreditert)
            .Select(f =>
            {
                f.Leverandor = Leverandorer.First(l => l.Id == f.LeverandorId);
                return f;
            })
            .ToList();
        return Task.FromResult(result);
    }

    public Task<List<LeverandorFaktura>> HentForfalteFakturaerForBetalingAsync(
        DateOnly forfallTilOgMed, List<Guid>? leverandorIder = null, CancellationToken ct = default)
    {
        var q = Fakturaer.Where(f => !f.IsDeleted && f.Forfallsdato <= forfallTilOgMed && f.GjenstaendeBelop.Verdi > 0);
        if (leverandorIder != null && leverandorIder.Count > 0)
            q = q.Where(f => leverandorIder.Contains(f.LeverandorId));
        var result = q.Select(f =>
        {
            f.Leverandor = Leverandorer.First(l => l.Id == f.LeverandorId);
            return f;
        }).ToList();
        return Task.FromResult(result);
    }

    public Task<List<LeverandorFaktura>> HentFakturaerForLeverandorAsync(
        Guid leverandorId, DateOnly? fraDato = null, DateOnly? tilDato = null, CancellationToken ct = default)
    {
        var q = Fakturaer.Where(f => !f.IsDeleted && f.LeverandorId == leverandorId);
        if (fraDato.HasValue) q = q.Where(f => f.Fakturadato >= fraDato.Value);
        if (tilDato.HasValue) q = q.Where(f => f.Fakturadato <= tilDato.Value);
        var result = q.Select(f =>
        {
            f.Leverandor = Leverandorer.First(l => l.Id == f.LeverandorId);
            return f;
        }).ToList();
        return Task.FromResult(result);
    }

    public Task<Domain.Features.Leverandorreskontro.Betalingsforslag?> HentBetalingsforslagAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Betalingsforslag.FirstOrDefault(b => b.Id == id && !b.IsDeleted));

    public Task<Domain.Features.Leverandorreskontro.Betalingsforslag?> HentBetalingsforslagMedLinjerAsync(Guid id, CancellationToken ct = default)
    {
        var f = Betalingsforslag.FirstOrDefault(b => b.Id == id && !b.IsDeleted);
        if (f != null)
        {
            foreach (var linje in f.Linjer)
            {
                linje.Leverandor = Leverandorer.FirstOrDefault(l => l.Id == linje.LeverandorId)!;
                linje.LeverandorFaktura = Fakturaer.FirstOrDefault(fk => fk.Id == linje.LeverandorFakturaId)!;
            }
        }
        return Task.FromResult(f);
    }

    public Task LeggTilBetalingsforslagAsync(Domain.Features.Leverandorreskontro.Betalingsforslag forslag, CancellationToken ct = default)
    {
        Betalingsforslag.Add(forslag);
        return Task.CompletedTask;
    }

    public Task OppdaterBetalingsforslagAsync(Domain.Features.Leverandorreskontro.Betalingsforslag forslag, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<int> NesteForslagsnummerAsync(CancellationToken ct = default)
        => Task.FromResult(_nesteForslagsnummer++);

    public Task LeggTilBetalingAsync(LeverandorBetaling betaling, CancellationToken ct = default)
    {
        Betalinger.Add(betaling);
        return Task.CompletedTask;
    }

    public Task<LeverandorBetaling?> HentBetalingMedBankreferanseAsync(string bankreferanse, CancellationToken ct = default)
        => Task.FromResult(Betalinger.FirstOrDefault(b => b.Bankreferanse == bankreferanse));

    public Task LagreEndringerAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
