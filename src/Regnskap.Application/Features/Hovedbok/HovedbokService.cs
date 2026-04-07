using Regnskap.Application.Features.Kontoplan;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Application.Features.Hovedbok;

public class HovedbokService : IHovedbokService
{
    private readonly IHovedbokRepository _repo;
    private readonly IKontoService _kontoService;

    public HovedbokService(IHovedbokRepository repo, IKontoService kontoService)
    {
        _repo = repo;
        _kontoService = kontoService;
    }

    public async Task<KontoutskriftDto> HentKontoutskriftAsync(
        string kontonummer, DateOnly fraDato, DateOnly tilDato,
        int side = 1, int antall = 100, CancellationToken ct = default)
    {
        var konto = await _kontoService.HentKontoEllerKastAsync(kontonummer, ct);

        var posteringer = await _repo.HentPosteringerForKontoAsync(
            kontonummer, fraDato, tilDato, side, antall, ct);
        var totaltAntall = await _repo.TellPosteringerForKontoAsync(
            kontonummer, fraDato, tilDato, ct);

        // Beregn inngaende balanse fra KontoSaldo for forrige periode (effektivt, unngår full tabell-scan)
        var ib = 0m;
        var fraPeriode = await _repo.HentPeriodeForDatoAsync(fraDato, ct);
        if (fraPeriode != null && fraPeriode.Periode > 0)
        {
            var forrigeSaldo = await _repo.HentKontoSaldoAsync(
                kontonummer, fraPeriode.Ar, fraPeriode.Periode - 1, ct);
            if (forrigeSaldo != null)
                ib = forrigeSaldo.UtgaendeBalanse.Verdi;
        }
        else if (fraPeriode != null && fraPeriode.Periode == 0)
        {
            // Apningsbalanse-perioden: IB er 0
            ib = 0m;
        }

        // Bygg kontoutskrift-linjer med lopende balanse
        var linjer = new List<KontoutskriftLinjeDto>();
        var lopendeBalanse = ib;

        foreach (var p in posteringer)
        {
            var endring = p.Side == BokforingSide.Debet ? p.Belop.Verdi : -p.Belop.Verdi;
            lopendeBalanse += endring;

            linjer.Add(new KontoutskriftLinjeDto(
                p.Bilagsdato,
                p.Bilag?.BilagsId ?? $"{p.Bilagsdato.Year}-?????",
                p.Bilag?.Beskrivelse ?? "",
                p.Linjenummer,
                p.Beskrivelse,
                p.Side.ToString(),
                p.Belop.Verdi,
                lopendeBalanse));
        }

        var sumDebet = posteringer
            .Where(p => p.Side == BokforingSide.Debet)
            .Sum(p => p.Belop.Verdi);
        var sumKredit = posteringer
            .Where(p => p.Side == BokforingSide.Kredit)
            .Sum(p => p.Belop.Verdi);

        return new KontoutskriftDto(
            konto.Kontonummer,
            konto.Navn,
            konto.Kontotype.ToString(),
            konto.Normalbalanse.ToString(),
            fraDato,
            tilDato,
            ib,
            linjer,
            sumDebet,
            sumKredit,
            ib + sumDebet - sumKredit,
            totaltAntall,
            side,
            antall);
    }

    public async Task<SaldobalanseDto> HentSaldobalanseAsync(
        int ar, int periode,
        bool inkluderNullsaldo = false,
        int? kontoklasse = null,
        CancellationToken ct = default)
    {
        var p = await _repo.HentPeriodeAsync(ar, periode, ct)
            ?? throw new PeriodeIkkeFunnetException(ar, periode);

        var saldoer = await _repo.HentAlleSaldoerForPeriodeAsync(ar, periode, ct);

        if (!inkluderNullsaldo)
        {
            saldoer = saldoer.Where(s =>
                s.SumDebet.Verdi != 0 || s.SumKredit.Verdi != 0 ||
                s.InngaendeBalanse.Verdi != 0).ToList();
        }

        if (kontoklasse.HasValue)
        {
            var klasse = kontoklasse.Value.ToString();
            saldoer = saldoer.Where(s => s.Kontonummer.StartsWith(klasse)).ToList();
        }

        var linjer = saldoer
            .OrderBy(s => s.Kontonummer)
            .Select(s => new SaldobalanseLinjeDto(
                s.Kontonummer,
                s.Konto?.Navn ?? s.Kontonummer,
                s.Konto?.Kontotype.ToString() ?? "",
                s.InngaendeBalanse.Verdi,
                s.SumDebet.Verdi,
                s.SumKredit.Verdi,
                s.Endring.Verdi,
                s.UtgaendeBalanse.Verdi))
            .ToList();

        var totalDebet = linjer.Sum(l => l.SumDebet);
        var totalKredit = linjer.Sum(l => l.SumKredit);

        return new SaldobalanseDto(
            ar,
            periode,
            p.Periodenavn,
            linjer,
            totalDebet,
            totalKredit,
            totalDebet == totalKredit);
    }

    public async Task<KontoSaldoOppslagDto> HentKontoSaldoAsync(
        string kontonummer, int ar,
        int? fraPeriode = null, int? tilPeriode = null,
        CancellationToken ct = default)
    {
        var konto = await _kontoService.HentKontoEllerKastAsync(kontonummer, ct);

        var saldoer = await _repo.HentSaldoHistorikkForKontoAsync(kontonummer, ar, ct);

        if (fraPeriode.HasValue)
            saldoer = saldoer.Where(s => s.Periode >= fraPeriode.Value).ToList();
        if (tilPeriode.HasValue)
            saldoer = saldoer.Where(s => s.Periode <= tilPeriode.Value).ToList();

        var perioder = saldoer
            .OrderBy(s => s.Periode)
            .Select(s => new KontoSaldoPeriodeDto(
                s.Periode,
                s.InngaendeBalanse.Verdi,
                s.SumDebet.Verdi,
                s.SumKredit.Verdi,
                s.UtgaendeBalanse.Verdi,
                s.AntallPosteringer))
            .ToList();

        var totalIB = perioder.FirstOrDefault()?.InngaendeBalanse ?? 0m;
        var totalDebet = perioder.Sum(p => p.SumDebet);
        var totalKredit = perioder.Sum(p => p.SumKredit);
        var totalUB = perioder.LastOrDefault()?.UtgaendeBalanse ?? 0m;

        return new KontoSaldoOppslagDto(
            konto.Kontonummer,
            konto.Navn,
            ar,
            perioder,
            totalIB,
            totalDebet,
            totalKredit,
            totalUB);
    }
}
