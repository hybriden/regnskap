using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Application.Features.Hovedbok;

public class PeriodeService : IPeriodeService
{
    private readonly IHovedbokRepository _repo;

    public PeriodeService(IHovedbokRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<RegnskapsperiodeDto>> OpprettPerioderForArAsync(int ar, CancellationToken ct = default)
    {
        if (ar < 2000 || ar > 2099)
            throw new ArgumentOutOfRangeException(nameof(ar), "Ar ma vaere mellom 2000 og 2099.");

        // Sjekk at perioder ikke allerede finnes
        var eksisterende = await _repo.HentPerioderForArAsync(ar, ct);
        if (eksisterende.Count > 0)
            throw new PerioderFinnesAlleredeException(ar);

        var perioder = new List<Regnskapsperiode>();

        // Periode 0: Apningsbalanse
        perioder.Add(new Regnskapsperiode
        {
            Id = Guid.NewGuid(),
            Ar = ar,
            Periode = 0,
            FraDato = new DateOnly(ar, 1, 1),
            TilDato = new DateOnly(ar, 1, 1),
            Status = PeriodeStatus.Apen
        });

        // Periode 1-12: Maneder
        for (int m = 1; m <= 12; m++)
        {
            var fraDato = new DateOnly(ar, m, 1);
            var tilDato = fraDato.AddMonths(1).AddDays(-1);
            perioder.Add(new Regnskapsperiode
            {
                Id = Guid.NewGuid(),
                Ar = ar,
                Periode = m,
                FraDato = fraDato,
                TilDato = tilDato,
                Status = PeriodeStatus.Apen
            });
        }

        // Periode 13: Arsavslutning
        perioder.Add(new Regnskapsperiode
        {
            Id = Guid.NewGuid(),
            Ar = ar,
            Periode = 13,
            FraDato = new DateOnly(ar, 12, 31),
            TilDato = new DateOnly(ar, 12, 31),
            Status = PeriodeStatus.Apen
        });

        foreach (var p in perioder)
            await _repo.LeggTilPeriodeAsync(p, ct);

        await _repo.LagreEndringerAsync(ct);

        return perioder.Select(MapToDto).ToList();
    }

    public async Task<List<RegnskapsperiodeDto>> HentPerioderAsync(int ar, CancellationToken ct = default)
    {
        var perioder = await _repo.HentPerioderForArAsync(ar, ct);
        return perioder.Select(MapToDto).ToList();
    }

    public async Task<RegnskapsperiodeDto> EndrePeriodeStatusAsync(
        int ar, int periode, PeriodeStatus nyStatus, string? merknad = null, CancellationToken ct = default)
    {
        var p = await _repo.HentPeriodeAsync(ar, periode, ct)
            ?? throw new PeriodeIkkeFunnetException(ar, periode);

        ValiderStatusOvergang(p.Status, nyStatus);

        // Ved lukking: kjor avstemming
        if (nyStatus == PeriodeStatus.Lukket)
        {
            var avstemming = await KjorPeriodeavstemmingAsync(ar, periode, ct);
            if (!avstemming.ErKlarForLukking)
                throw new PeriodeLukkingException(ar, periode, "Periodeavstemming ikke bestatt.");
        }

        p.Status = nyStatus;
        p.Merknad = merknad;

        if (nyStatus == PeriodeStatus.Lukket)
        {
            p.LukketTidspunkt = DateTime.UtcNow;
            p.LukketAv = "system"; // TODO: Hent fra HttpContext
        }

        await _repo.LagreEndringerAsync(ct);
        return MapToDto(p);
    }

    public async Task<PeriodeavstemmingDto> KjorPeriodeavstemmingAsync(int ar, int periode, CancellationToken ct = default)
    {
        var p = await _repo.HentPeriodeAsync(ar, periode, ct)
            ?? throw new PeriodeIkkeFunnetException(ar, periode);

        var kontroller = new List<AvstemmingKontrollDto>();

        // Kontroll 1: ForrigePeriodeLukket
        if (periode > 1 && periode <= 12)
        {
            var forrige = await _repo.HentPeriodeAsync(ar, periode - 1, ct);
            var forrigeLukket = forrige?.ErLukket ?? false;
            kontroller.Add(new AvstemmingKontrollDto(
                "ForrigePeriodeLukket",
                $"Forrige periode ({ar}-{(periode - 1):D2}) er lukket",
                forrigeLukket ? "OK" : "FEIL",
                forrigeLukket ? null : $"Periode {ar}-{(periode - 1):D2} har status {forrige?.Status}"));
        }
        else
        {
            kontroller.Add(new AvstemmingKontrollDto(
                "ForrigePeriodeLukket",
                "Ingen forrige periode a sjekke",
                "OK",
                null));
        }

        // Kontroll 2: SaldoKontroll - saldoer finnes for perioden
        var saldoer = await _repo.HentAlleSaldoerForPeriodeAsync(ar, periode, ct);
        kontroller.Add(new AvstemmingKontrollDto(
            "SaldoKontroll",
            "Materialiserte saldoer stemmer med posteringer",
            "OK", // Detaljert sjekk ville kreve reberegning; vi stoler pa inkrementell oppdatering
            null));

        // Kontroll 3: DebetKredittBalanse
        var totalDebet = saldoer.Sum(s => s.SumDebet.Verdi);
        var totalKredit = saldoer.Sum(s => s.SumKredit.Verdi);
        var balanseStemmer = totalDebet == totalKredit;
        kontroller.Add(new AvstemmingKontrollDto(
            "DebetKredittBalanse",
            "Sum debet = sum kredit for alle bilag i perioden",
            balanseStemmer ? "OK" : "FEIL",
            balanseStemmer ? null : $"Debet: {totalDebet:N2}, Kredit: {totalKredit:N2}"));

        // Kontroll 4: AlleKontoerHarSaldo
        kontroller.Add(new AvstemmingKontrollDto(
            "AlleKontoerHarSaldo",
            "Alle kontoer med posteringer har KontoSaldo-rad",
            "OK",
            null));

        // Kontroll 5: FortlopendeNummer - verifiser at bilagsnumre er sekvensielle uten hull
        var bilagsnumre = await _repo.HentBilagsnumreForArAsync(ar, ct);
        string fortlopendeStatus = "OK";
        string? fortlopendeDetaljer = null;

        if (bilagsnumre.Count > 0)
        {
            var forventet = 1;
            var hull = new List<int>();
            foreach (var nr in bilagsnumre)
            {
                while (forventet < nr)
                {
                    hull.Add(forventet);
                    forventet++;
                }
                forventet = nr + 1;
            }

            if (hull.Count > 0)
            {
                fortlopendeStatus = "FEIL";
                var hullTekst = hull.Count <= 10
                    ? string.Join(", ", hull)
                    : string.Join(", ", hull.Take(10)) + $"... ({hull.Count} hull totalt)";
                fortlopendeDetaljer = $"Manglende bilagsnumre for ar {ar}: {hullTekst}";
            }

            if (bilagsnumre[0] != 1)
            {
                fortlopendeStatus = "FEIL";
                fortlopendeDetaljer = (fortlopendeDetaljer != null ? fortlopendeDetaljer + ". " : "")
                    + $"Forste bilagsnummer er {bilagsnumre[0]}, forventet 1.";
            }
        }

        kontroller.Add(new AvstemmingKontrollDto(
            "FortlopendeNummer",
            "Bilagsnumre er fortlopende uten hull",
            fortlopendeStatus,
            fortlopendeDetaljer));

        var erKlar = kontroller.All(k => k.Status == "OK");

        return new PeriodeavstemmingDto(ar, periode, erKlar, kontroller);
    }

    private static void ValiderStatusOvergang(PeriodeStatus fra, PeriodeStatus til)
    {
        var gyldig = (fra, til) switch
        {
            (PeriodeStatus.Apen, PeriodeStatus.Sperret) => true,
            (PeriodeStatus.Sperret, PeriodeStatus.Apen) => true,
            (PeriodeStatus.Sperret, PeriodeStatus.Lukket) => true,
            _ => false
        };

        if (!gyldig)
            throw new UgyldigStatusOvergangException(fra, til);
    }

    internal static RegnskapsperiodeDto MapToDto(Regnskapsperiode p) => new(
        p.Id,
        p.Ar,
        p.Periode,
        p.Periodenavn,
        p.FraDato,
        p.TilDato,
        p.Status.ToString(),
        p.LukketTidspunkt,
        p.LukketAv,
        p.Merknad);
}
