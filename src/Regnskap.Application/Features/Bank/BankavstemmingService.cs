namespace Regnskap.Application.Features.Bank;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Bankavstemming;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Service for bankavstemming.
/// Implementerer FR-B06 fra spesifikasjonen.
/// </summary>
public class BankavstemmingService : IBankavstemmingService
{
    private readonly IBankRepository _bankRepo;
    private readonly IHovedbokRepository _hovedbokRepo;

    public BankavstemmingService(IBankRepository bankRepo, IHovedbokRepository hovedbokRepo)
    {
        _bankRepo = bankRepo;
        _hovedbokRepo = hovedbokRepo;
    }

    public async Task<Bankavstemming> HentEllerOpprett(Guid bankkontoId, int aar, int periode)
    {
        var eksisterende = await _bankRepo.HentAvstemming(bankkontoId, aar, periode);
        if (eksisterende != null) return eksisterende;

        var bankkonto = await _bankRepo.HentBankkonto(bankkontoId)
            ?? throw new KeyNotFoundException($"Bankkonto {bankkontoId} ikke funnet.");

        // Hent saldoer
        var saldoHovedbok = await HentHovedbokSaldo(bankkonto, aar, periode);
        var saldoBank = await HentBankSaldo(bankkontoId, aar, periode);

        var avstemming = new Bankavstemming
        {
            Id = Guid.NewGuid(),
            BankkontoId = bankkontoId,
            Ar = aar,
            Periode = periode,
            Avstemmingsdato = new DateOnly(aar, periode == 0 ? 12 : periode, DateTime.DaysInMonth(aar, periode == 0 ? 12 : periode)),
            SaldoHovedbok = new Belop(saldoHovedbok),
            SaldoBank = new Belop(saldoBank),
            Differanse = new Belop(saldoBank - saldoHovedbok),
            Status = AvstemmingStatus.UnderArbeid
        };

        await _bankRepo.LeggTilAvstemming(avstemming);
        await _bankRepo.LagreEndringerAsync();

        return avstemming;
    }

    public async Task<Bankavstemming> Oppdater(Guid avstemmingId, OppdaterAvstemmingRequest request)
    {
        var avstemming = await _bankRepo.HentAvstemmingMedId(avstemmingId)
            ?? throw new KeyNotFoundException($"Avstemming {avstemmingId} ikke funnet.");

        avstemming.UtestaaendeBetalinger = new Belop(request.UtestaaendeBetalinger);
        avstemming.InnbetalingerITransitt = new Belop(request.InnbetalingerITransitt);
        avstemming.AndreDifferanser = new Belop(request.AndreDifferanser);
        avstemming.DifferanseForklaring = request.DifferanseForklaring;

        await _bankRepo.LagreEndringerAsync();
        return avstemming;
    }

    public async Task<Bankavstemming> Godkjenn(Guid avstemmingId, string godkjentAv)
    {
        var avstemming = await _bankRepo.HentAvstemmingMedId(avstemmingId)
            ?? throw new KeyNotFoundException($"Avstemming {avstemmingId} ikke funnet.");

        if (avstemming.UforklartDifferanse.Verdi != 0m && string.IsNullOrWhiteSpace(avstemming.DifferanseForklaring))
            throw new AvstemmingUforklartException();

        avstemming.Status = avstemming.Differanse.Verdi == 0m
            ? AvstemmingStatus.Avstemt
            : AvstemmingStatus.AvstemtMedDifferanse;

        avstemming.GodkjentAv = godkjentAv;
        avstemming.GodkjentTidspunkt = DateTime.UtcNow;

        await _bankRepo.LagreEndringerAsync();
        return avstemming;
    }

    public async Task<AvstemmingsrapportResponse> GenererRapport(Guid bankkontoId, DateOnly dato)
    {
        var bankkonto = await _bankRepo.HentBankkonto(bankkontoId)
            ?? throw new KeyNotFoundException($"Bankkonto {bankkontoId} ikke funnet.");

        var aar = dato.Year;
        var periode = dato.Month;

        var saldoHovedbok = await HentHovedbokSaldo(bankkonto, aar, periode);
        var saldoBank = await HentBankSaldo(bankkontoId, aar, periode);
        var differanse = saldoBank - saldoHovedbok;

        // Hent umatchede bevegelser for analyse
        var umatchede = await _bankRepo.HentUmatchedeBevegelser(bankkontoId);
        var innbetalingerITransitt = umatchede
            .Where(b => b.Retning == BankbevegelseRetning.Inn)
            .Select(b => new AvstemmingspostResponse(
                b.Bokforingsdato,
                b.Beskrivelse ?? $"Innbetaling {b.KidNummer ?? b.Motpart ?? "ukjent"}",
                b.Belop.Verdi,
                b.KidNummer ?? b.EndToEndId))
            .ToList();

        var utestaaendeBetalinger = umatchede
            .Where(b => b.Retning == BankbevegelseRetning.Ut)
            .Select(b => new AvstemmingspostResponse(
                b.Bokforingsdato,
                b.Beskrivelse ?? $"Utbetaling til {b.Motpart ?? "ukjent"}",
                b.Belop.Verdi,
                b.EndToEndId ?? b.BankReferanse))
            .ToList();

        var sumTidsavgrensninger = innbetalingerITransitt.Sum(x => x.Belop) - utestaaendeBetalinger.Sum(x => x.Belop);

        // Hent eksisterende avstemming for status
        var avstemming = await _bankRepo.HentAvstemming(bankkontoId, aar, periode);
        var status = avstemming?.Status ?? AvstemmingStatus.UnderArbeid;

        return new AvstemmingsrapportResponse(
            bankkonto.Kontonummer,
            bankkonto.Banknavn,
            bankkonto.Hovedbokkontonummer,
            dato,
            saldoHovedbok,
            saldoBank,
            differanse,
            utestaaendeBetalinger,
            innbetalingerITransitt,
            new List<AvstemmingspostResponse>(),
            sumTidsavgrensninger,
            differanse - sumTidsavgrensninger,
            status
        );
    }

    private async Task<decimal> HentHovedbokSaldo(Bankkonto bankkonto, int aar, int periode)
    {
        var saldo = await _hovedbokRepo.HentKontoSaldoAsync(bankkonto.Hovedbokkontonummer, aar, periode);
        return saldo?.UtgaendeBalanse.Verdi ?? 0m;
    }

    private async Task<decimal> HentBankSaldo(Guid bankkontoId, int aar, int periode)
    {
        var kontoutskrifter = await _bankRepo.HentKontoutskrifter(bankkontoId);
        var maned = periode == 0 ? 12 : periode;
        var periodeSlutt = new DateOnly(aar, maned, DateTime.DaysInMonth(aar, maned));

        var sisteUtskrift = kontoutskrifter
            .Where(k => k.PeriodeTil <= periodeSlutt)
            .OrderByDescending(k => k.PeriodeTil)
            .FirstOrDefault();

        return sisteUtskrift?.UtgaendeSaldo.Verdi ?? 0m;
    }
}
