namespace Regnskap.Application.Features.Fakturering;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Fakturering;

/// <summary>
/// Statisk beregningslogikk for fakturalinjer og totaler.
/// FR-F02 og FR-F03 fra spesifikasjonen.
/// </summary>
public static class FakturaBeregning
{
    /// <summary>
    /// Beregn alle belop for en fakturalinje (FR-F02).
    /// </summary>
    public static void BeregnLinje(FakturaLinje linje)
    {
        var bruttolinjebelop = linje.Antall * linje.Enhetspris.Verdi;

        decimal rabattBelop = 0m;
        if (linje.RabattType == Domain.Features.Fakturering.RabattType.Prosent && linje.RabattProsent.HasValue)
        {
            rabattBelop = Math.Round(bruttolinjebelop * linje.RabattProsent.Value / 100m, 2, MidpointRounding.AwayFromZero);
            linje.RabattBelop = new Belop(rabattBelop);
        }
        else if (linje.RabattType == Domain.Features.Fakturering.RabattType.Belop && linje.RabattBelop.HasValue)
        {
            rabattBelop = linje.RabattBelop.Value.Verdi;
        }

        var nettobelop = bruttolinjebelop - rabattBelop;
        linje.Nettobelop = new Belop(nettobelop);

        var mvaBelop = Math.Round(nettobelop * linje.MvaSats / 100m, 2, MidpointRounding.AwayFromZero);
        linje.MvaBelop = new Belop(mvaBelop);

        linje.Bruttobelop = new Belop(nettobelop + mvaBelop);
    }

    /// <summary>
    /// Beregn fakturatotaler fra linjer (FR-F03).
    /// </summary>
    public static void BeregnTotaler(Faktura faktura)
    {
        var belopEksMva = 0m;
        var mvaBelop = 0m;

        foreach (var linje in faktura.Linjer)
        {
            belopEksMva += linje.Nettobelop.Verdi;
            mvaBelop += linje.MvaBelop.Verdi;
        }

        faktura.BelopEksMva = new Belop(belopEksMva);
        faktura.MvaBelop = new Belop(mvaBelop);
        faktura.BelopInklMva = new Belop(belopEksMva + mvaBelop);
    }

    /// <summary>
    /// Beregn MVA-linjer (oppsummering per sats) (FR-F03).
    /// </summary>
    public static List<FakturaMvaLinje> BeregnMvaLinjer(Faktura faktura)
    {
        return faktura.Linjer
            .GroupBy(l => l.MvaKode)
            .Select(g =>
            {
                var forsteLinje = g.First();
                return new FakturaMvaLinje
                {
                    Id = Guid.NewGuid(),
                    FakturaId = faktura.Id,
                    MvaKode = g.Key,
                    MvaSats = forsteLinje.MvaSats,
                    Grunnlag = new Belop(g.Sum(l => l.Nettobelop.Verdi)),
                    MvaBelop = new Belop(g.Sum(l => l.MvaBelop.Verdi)),
                    EhfTaxCategoryId = MapTilEhfTaxCategory(g.Key, forsteLinje.MvaSats)
                };
            })
            .ToList();
    }

    /// <summary>
    /// Mapper MVA-kode til EHF TaxCategory ID.
    /// </summary>
    public static string MapTilEhfTaxCategory(string mvaKode, decimal mvaSats)
    {
        // Kode 5 = eksport (Zero rated), Kode 6 = utenfor MVA (Exempt)
        if (mvaKode == "5") return "Z";
        if (mvaKode == "6") return "E";
        if (mvaSats == 0m) return "Z";
        return "S"; // Standard rate
    }

    /// <summary>
    /// Beregn forfallsdato (FR-F04).
    /// </summary>
    public static DateOnly BeregnForfallsdato(DateOnly fakturadato, Domain.Features.Kundereskontro.KundeBetalingsbetingelse betingelse, int? egendefinertDager = null)
    {
        var dager = betingelse switch
        {
            Domain.Features.Kundereskontro.KundeBetalingsbetingelse.Netto10 => 10,
            Domain.Features.Kundereskontro.KundeBetalingsbetingelse.Netto14 => 14,
            Domain.Features.Kundereskontro.KundeBetalingsbetingelse.Netto20 => 20,
            Domain.Features.Kundereskontro.KundeBetalingsbetingelse.Netto30 => 30,
            Domain.Features.Kundereskontro.KundeBetalingsbetingelse.Netto45 => 45,
            Domain.Features.Kundereskontro.KundeBetalingsbetingelse.Netto60 => 60,
            Domain.Features.Kundereskontro.KundeBetalingsbetingelse.Kontant => 0,
            Domain.Features.Kundereskontro.KundeBetalingsbetingelse.Forskudd => 0,
            Domain.Features.Kundereskontro.KundeBetalingsbetingelse.Egendefinert => egendefinertDager ?? 14,
            _ => 14
        };

        var forfallsdato = fakturadato.AddDays(dager);

        // Flytt fra helg til neste mandag
        var dayOfWeek = forfallsdato.DayOfWeek;
        if (dayOfWeek == DayOfWeek.Saturday)
            forfallsdato = forfallsdato.AddDays(2);
        else if (dayOfWeek == DayOfWeek.Sunday)
            forfallsdato = forfallsdato.AddDays(1);

        return forfallsdato;
    }

    /// <summary>
    /// Map Enhet enum til UBL unitCode.
    /// </summary>
    public static string EnhetTilUblKode(Enhet enhet) => enhet switch
    {
        Enhet.Stykk => "EA",
        Enhet.Timer => "HUR",
        Enhet.Kilogram => "KGM",
        Enhet.Liter => "LTR",
        Enhet.Meter => "MTR",
        Enhet.Kvadratmeter => "MTK",
        Enhet.Pakke => "PK",
        Enhet.Maaned => "MON",
        Enhet.Dag => "DAY",
        _ => "EA"
    };
}
