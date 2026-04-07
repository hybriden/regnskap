using FluentAssertions;
using Regnskap.Application.Features.Fakturering;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Fakturering;
using Regnskap.Domain.Features.Kundereskontro;

namespace Regnskap.Tests.Features.Fakturering;

public class FakturaBeregningTests
{
    // --- FR-F02: Linjeberegning ---

    [Fact]
    public void BeregnLinje_UtenRabatt_BeregnerKorrekt()
    {
        var linje = LagLinje(antall: 10, enhetspris: 100m, mvaSats: 25m);

        FakturaBeregning.BeregnLinje(linje);

        linje.Nettobelop.Verdi.Should().Be(1000m);
        linje.MvaBelop.Verdi.Should().Be(250m);
        linje.Bruttobelop.Verdi.Should().Be(1250m);
    }

    [Fact]
    public void BeregnLinje_MedProsentRabatt_BeregnerKorrekt()
    {
        var linje = LagLinje(antall: 10, enhetspris: 100m, mvaSats: 25m);
        linje.RabattType = RabattType.Prosent;
        linje.RabattProsent = 10m; // 10%

        FakturaBeregning.BeregnLinje(linje);

        linje.RabattBelop!.Value.Verdi.Should().Be(100m); // 1000 * 10%
        linje.Nettobelop.Verdi.Should().Be(900m);
        linje.MvaBelop.Verdi.Should().Be(225m); // 900 * 25%
        linje.Bruttobelop.Verdi.Should().Be(1125m);
    }

    [Fact]
    public void BeregnLinje_MedBelopRabatt_BeregnerKorrekt()
    {
        var linje = LagLinje(antall: 5, enhetspris: 200m, mvaSats: 25m);
        linje.RabattType = RabattType.Belop;
        linje.RabattBelop = new Belop(50m);

        FakturaBeregning.BeregnLinje(linje);

        linje.Nettobelop.Verdi.Should().Be(950m); // 1000 - 50
        linje.MvaBelop.Verdi.Should().Be(237.50m); // 950 * 25%
        linje.Bruttobelop.Verdi.Should().Be(1187.50m);
    }

    [Fact]
    public void BeregnLinje_MedNullprosent_GirNullMva()
    {
        var linje = LagLinje(antall: 1, enhetspris: 500m, mvaSats: 0m);

        FakturaBeregning.BeregnLinje(linje);

        linje.Nettobelop.Verdi.Should().Be(500m);
        linje.MvaBelop.Verdi.Should().Be(0m);
        linje.Bruttobelop.Verdi.Should().Be(500m);
    }

    [Fact]
    public void BeregnLinje_MvaAvrunding_RundesRiktig()
    {
        // 3 * 33.33 = 99.99, 99.99 * 25% = 24.9975 -> skal rundes til 25.00
        var linje = LagLinje(antall: 3, enhetspris: 33.33m, mvaSats: 25m);

        FakturaBeregning.BeregnLinje(linje);

        linje.Nettobelop.Verdi.Should().Be(99.99m);
        linje.MvaBelop.Verdi.Should().Be(25.00m); // AwayFromZero
        linje.Bruttobelop.Verdi.Should().Be(124.99m);
    }

    [Fact]
    public void BeregnLinje_LavSats15Prosent_BeregnerKorrekt()
    {
        var linje = LagLinje(antall: 2, enhetspris: 150m, mvaSats: 15m);

        FakturaBeregning.BeregnLinje(linje);

        linje.Nettobelop.Verdi.Should().Be(300m);
        linje.MvaBelop.Verdi.Should().Be(45m);
        linje.Bruttobelop.Verdi.Should().Be(345m);
    }

    // --- FR-F03: Totalberegning ---

    [Fact]
    public void BeregnTotaler_FlereLinjer_SummererKorrekt()
    {
        var faktura = LagFaktura();
        var linje1 = LagLinje(antall: 10, enhetspris: 100m, mvaSats: 25m);
        var linje2 = LagLinje(antall: 5, enhetspris: 200m, mvaSats: 15m);
        FakturaBeregning.BeregnLinje(linje1);
        FakturaBeregning.BeregnLinje(linje2);
        faktura.Linjer.Add(linje1);
        faktura.Linjer.Add(linje2);

        FakturaBeregning.BeregnTotaler(faktura);

        faktura.BelopEksMva.Verdi.Should().Be(2000m); // 1000 + 1000
        faktura.MvaBelop.Verdi.Should().Be(400m); // 250 + 150
        faktura.BelopInklMva.Verdi.Should().Be(2400m);
    }

    [Fact]
    public void BeregnTotaler_EnkeltLinje_BeregnerKorrekt()
    {
        var faktura = LagFaktura();
        var linje = LagLinje(antall: 1, enhetspris: 10000m, mvaSats: 25m);
        FakturaBeregning.BeregnLinje(linje);
        faktura.Linjer.Add(linje);

        FakturaBeregning.BeregnTotaler(faktura);

        faktura.BelopEksMva.Verdi.Should().Be(10000m);
        faktura.MvaBelop.Verdi.Should().Be(2500m);
        faktura.BelopInklMva.Verdi.Should().Be(12500m);
    }

    // --- MVA-linjer ---

    [Fact]
    public void BeregnMvaLinjer_GrupperPerMvaKode()
    {
        var faktura = LagFaktura();
        var linje1 = LagLinje(antall: 10, enhetspris: 100m, mvaSats: 25m, mvaKode: "3");
        var linje2 = LagLinje(antall: 5, enhetspris: 200m, mvaSats: 25m, mvaKode: "3");
        var linje3 = LagLinje(antall: 2, enhetspris: 150m, mvaSats: 15m, mvaKode: "31");
        FakturaBeregning.BeregnLinje(linje1);
        FakturaBeregning.BeregnLinje(linje2);
        FakturaBeregning.BeregnLinje(linje3);
        faktura.Linjer.Add(linje1);
        faktura.Linjer.Add(linje2);
        faktura.Linjer.Add(linje3);

        var mvaLinjer = FakturaBeregning.BeregnMvaLinjer(faktura);

        mvaLinjer.Should().HaveCount(2);

        var mva25 = mvaLinjer.Single(m => m.MvaKode == "3");
        mva25.Grunnlag.Verdi.Should().Be(2000m);
        mva25.MvaBelop.Verdi.Should().Be(500m);
        mva25.EhfTaxCategoryId.Should().Be("S");

        var mva15 = mvaLinjer.Single(m => m.MvaKode == "31");
        mva15.Grunnlag.Verdi.Should().Be(300m);
        mva15.MvaBelop.Verdi.Should().Be(45m);
    }

    // --- FR-F04: Forfallsdato ---

    [Fact]
    public void BeregnForfallsdato_Netto30_GirKorrektDato()
    {
        var fakturadato = new DateOnly(2026, 1, 5); // mandag

        var forfall = FakturaBeregning.BeregnForfallsdato(fakturadato, KundeBetalingsbetingelse.Netto30);

        forfall.Should().Be(new DateOnly(2026, 2, 4)); // onsdag
    }

    [Fact]
    public void BeregnForfallsdato_FlyttesHelgTilMandag()
    {
        // 2026-01-03 (lordag) + 14 dager = 2026-01-17 (lordag) -> 2026-01-19 (mandag)
        var fakturadato = new DateOnly(2026, 1, 3); // lordag
        // Men fakturadato + 14 = 17. jan 2026. La oss sjekke dag.
        var forfall = FakturaBeregning.BeregnForfallsdato(fakturadato, KundeBetalingsbetingelse.Netto14);

        forfall.DayOfWeek.Should().NotBe(DayOfWeek.Saturday);
        forfall.DayOfWeek.Should().NotBe(DayOfWeek.Sunday);
    }

    [Fact]
    public void BeregnForfallsdato_Kontant_GirSammeDag()
    {
        var fakturadato = new DateOnly(2026, 3, 10);

        var forfall = FakturaBeregning.BeregnForfallsdato(fakturadato, KundeBetalingsbetingelse.Kontant);

        forfall.Should().Be(fakturadato);
    }

    // --- EHF TaxCategory mapping ---

    [Theory]
    [InlineData("3", 25, "S")]
    [InlineData("31", 15, "S")]
    [InlineData("33", 12, "S")]
    [InlineData("5", 0, "Z")]
    [InlineData("6", 0, "E")]
    public void MapTilEhfTaxCategory_MapperRiktig(string mvaKode, decimal sats, string forventet)
    {
        FakturaBeregning.MapTilEhfTaxCategory(mvaKode, sats).Should().Be(forventet);
    }

    // --- Enhet til UBL ---

    [Theory]
    [InlineData(Enhet.Stykk, "EA")]
    [InlineData(Enhet.Timer, "HUR")]
    [InlineData(Enhet.Kilogram, "KGM")]
    [InlineData(Enhet.Liter, "LTR")]
    [InlineData(Enhet.Meter, "MTR")]
    public void EnhetTilUblKode_MapperKorrekt(Enhet enhet, string forventet)
    {
        FakturaBeregning.EnhetTilUblKode(enhet).Should().Be(forventet);
    }

    // --- Helpers ---

    private static FakturaLinje LagLinje(decimal antall, decimal enhetspris, decimal mvaSats, string mvaKode = "3")
    {
        return new FakturaLinje
        {
            Id = Guid.NewGuid(),
            FakturaId = Guid.NewGuid(),
            Linjenummer = 1,
            Beskrivelse = "Testprodukt",
            Antall = antall,
            Enhet = Enhet.Stykk,
            Enhetspris = new Belop(enhetspris),
            MvaKode = mvaKode,
            MvaSats = mvaSats,
            KontoId = Guid.NewGuid(),
            Kontonummer = "3000"
        };
    }

    private static Faktura LagFaktura()
    {
        return new Faktura
        {
            Id = Guid.NewGuid(),
            KundeId = Guid.NewGuid(),
            Status = FakturaStatus.Utkast
        };
    }
}
