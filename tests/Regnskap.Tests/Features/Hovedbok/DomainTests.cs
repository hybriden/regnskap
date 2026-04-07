using FluentAssertions;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Tests.Features.Hovedbok;

public class BilagDomainTests
{
    [Fact]
    public void ValiderBalanse_BalansertBilag_KasterIkke()
    {
        var bilag = LagBalansertBilag(10000m);

        var act = () => bilag.ValiderBalanse();

        act.Should().NotThrow();
    }

    [Fact]
    public void ValiderBalanse_UbalansertBilag_KasterAccountingBalanceException()
    {
        var bilag = new Bilag { Ar = 2026, Bilagsnummer = 1 };
        bilag.Posteringer.Add(LagPostering(BokforingSide.Debet, 10000m));
        bilag.Posteringer.Add(LagPostering(BokforingSide.Kredit, 9000m));

        var act = () => bilag.ValiderBalanse();

        act.Should().Throw<AccountingBalanceException>();
    }

    [Fact]
    public void ValiderBalanse_MindreEnnToLinjer_KasterBilagValideringException()
    {
        var bilag = new Bilag { Ar = 2026, Bilagsnummer = 1 };
        bilag.Posteringer.Add(LagPostering(BokforingSide.Debet, 10000m));

        var act = () => bilag.ValiderBalanse();

        act.Should().Throw<BilagValideringException>();
    }

    [Fact]
    public void SumDebet_BeregnesKorrekt()
    {
        var bilag = new Bilag { Ar = 2026, Bilagsnummer = 1 };
        bilag.Posteringer.Add(LagPostering(BokforingSide.Debet, 8000m));
        bilag.Posteringer.Add(LagPostering(BokforingSide.Debet, 2000m));
        bilag.Posteringer.Add(LagPostering(BokforingSide.Kredit, 10000m));

        bilag.SumDebet().Verdi.Should().Be(10000m);
    }

    [Fact]
    public void SumKredit_BeregnesKorrekt()
    {
        var bilag = new Bilag { Ar = 2026, Bilagsnummer = 1 };
        bilag.Posteringer.Add(LagPostering(BokforingSide.Debet, 10000m));
        bilag.Posteringer.Add(LagPostering(BokforingSide.Kredit, 6000m));
        bilag.Posteringer.Add(LagPostering(BokforingSide.Kredit, 4000m));

        bilag.SumKredit().Verdi.Should().Be(10000m);
    }

    [Fact]
    public void BilagsId_FormateresKorrekt()
    {
        var bilag = new Bilag { Ar = 2026, Bilagsnummer = 42 };

        bilag.BilagsId.Should().Be("2026-00042");
    }

    private static Bilag LagBalansertBilag(decimal belop)
    {
        var bilag = new Bilag { Ar = 2026, Bilagsnummer = 1 };
        bilag.Posteringer.Add(LagPostering(BokforingSide.Debet, belop));
        bilag.Posteringer.Add(LagPostering(BokforingSide.Kredit, belop));
        return bilag;
    }

    private static Postering LagPostering(BokforingSide side, decimal belop)
    {
        return new Postering
        {
            Id = Guid.NewGuid(),
            Side = side,
            Belop = new Belop(belop),
            Kontonummer = "1920",
            Beskrivelse = "Test"
        };
    }
}

public class KontoSaldoDomainTests
{
    [Fact]
    public void LeggTilPostering_Debet_OkerSumDebet()
    {
        var saldo = LagNySaldo();

        saldo.LeggTilPostering(BokforingSide.Debet, new Belop(5000m));

        saldo.SumDebet.Verdi.Should().Be(5000m);
        saldo.SumKredit.Verdi.Should().Be(0m);
        saldo.AntallPosteringer.Should().Be(1);
    }

    [Fact]
    public void LeggTilPostering_Kredit_OkerSumKredit()
    {
        var saldo = LagNySaldo();

        saldo.LeggTilPostering(BokforingSide.Kredit, new Belop(3000m));

        saldo.SumKredit.Verdi.Should().Be(3000m);
        saldo.SumDebet.Verdi.Should().Be(0m);
    }

    [Fact]
    public void UtgaendeBalanse_BeregnesKorrektMedIB()
    {
        var saldo = LagNySaldo(ib: 100000m);
        saldo.LeggTilPostering(BokforingSide.Debet, new Belop(50000m));
        saldo.LeggTilPostering(BokforingSide.Kredit, new Belop(20000m));

        saldo.UtgaendeBalanse.Verdi.Should().Be(130000m); // 100000 + 50000 - 20000
    }

    [Fact]
    public void Endring_ErDifferansenMellomDebetOgKredit()
    {
        var saldo = LagNySaldo();
        saldo.LeggTilPostering(BokforingSide.Debet, new Belop(10000m));
        saldo.LeggTilPostering(BokforingSide.Kredit, new Belop(3000m));

        saldo.Endring.Verdi.Should().Be(7000m);
    }

    private static KontoSaldo LagNySaldo(decimal ib = 0m)
    {
        return new KontoSaldo
        {
            Id = Guid.NewGuid(),
            Kontonummer = "1920",
            KontoId = Guid.NewGuid(),
            Ar = 2026,
            Periode = 1,
            RegnskapsperiodeId = Guid.NewGuid(),
            InngaendeBalanse = new Belop(ib)
        };
    }
}

public class RegnskapsperiodeDomainTests
{
    [Fact]
    public void ValiderApen_ApenPeriode_KasterIkke()
    {
        var periode = new Regnskapsperiode { Status = PeriodeStatus.Apen, Ar = 2026, Periode = 1 };

        var act = () => periode.ValiderApen();

        act.Should().NotThrow();
    }

    [Fact]
    public void ValiderApen_SperretPeriode_KasterPeriodeLukketException()
    {
        var periode = new Regnskapsperiode { Status = PeriodeStatus.Sperret, Ar = 2026, Periode = 1 };

        var act = () => periode.ValiderApen();

        act.Should().Throw<PeriodeLukketException>();
    }

    [Fact]
    public void ValiderApen_LukketPeriode_KasterPeriodeLukketException()
    {
        var periode = new Regnskapsperiode { Status = PeriodeStatus.Lukket, Ar = 2026, Periode = 1 };

        var act = () => periode.ValiderApen();

        act.Should().Throw<PeriodeLukketException>();
    }

    [Fact]
    public void DatoErInnenforPeriode_DatoInnenfor_ReturnererTrue()
    {
        var periode = new Regnskapsperiode
        {
            FraDato = new DateOnly(2026, 3, 1),
            TilDato = new DateOnly(2026, 3, 31)
        };

        periode.DatoErInnenforPeriode(new DateOnly(2026, 3, 15)).Should().BeTrue();
    }

    [Fact]
    public void DatoErInnenforPeriode_DatoUtenfor_ReturnererFalse()
    {
        var periode = new Regnskapsperiode
        {
            FraDato = new DateOnly(2026, 3, 1),
            TilDato = new DateOnly(2026, 3, 31)
        };

        periode.DatoErInnenforPeriode(new DateOnly(2026, 4, 1)).Should().BeFalse();
    }

    [Fact]
    public void Periodenavn_MaanedsPeriode_FormateresKorrekt()
    {
        var periode = new Regnskapsperiode { Ar = 2026, Periode = 3 };

        periode.Periodenavn.Should().Be("2026-03");
    }
}
