using FluentAssertions;
using Regnskap.Application.Features.Bilagsregistrering;
using Regnskap.Application.Features.Kundereskontro;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kundereskontro;

namespace Regnskap.Tests.Features.Kundereskontro;

public class KundeFakturaServiceTests
{
    private readonly FakeKundeReskontroRepository _repo = new();
    private readonly KidService _kidService = new();
    private readonly FakeBilagRegistreringService _bilagService = new();
    private readonly KundeFakturaService _service;

    public KundeFakturaServiceTests()
    {
        _service = new KundeFakturaService(_repo, _kidService, _bilagService);
    }

    [Fact]
    public async Task RegistrerFaktura_GyldigRequest_OppretterFaktura()
    {
        var kunde = LagKunde();
        _repo.Kunder.Add(kunde);

        var request = LagFakturaRequest(kunde.Id);
        var result = await _service.RegistrerFakturaAsync(request);

        result.Fakturanummer.Should().Be(1);
        result.BelopEksMva.Should().Be(10000m);
        result.MvaBelop.Should().Be(2500m); // 25% MVA
        result.BelopInklMva.Should().Be(12500m);
        result.GjenstaendeBelop.Should().Be(12500m);
        result.KidNummer.Should().NotBeNullOrEmpty();
        result.Status.Should().Be(KundeFakturaStatus.Utstedt);
    }

    [Fact]
    public async Task RegistrerFaktura_SperretKunde_KasterException()
    {
        var kunde = LagKunde();
        kunde.ErSperret = true;
        _repo.Kunder.Add(kunde);

        var act = () => _service.RegistrerFakturaAsync(LagFakturaRequest(kunde.Id));

        await act.Should().ThrowAsync<KundeSperretException>();
    }

    [Fact]
    public async Task RegistrerFaktura_InaktivKunde_KasterException()
    {
        var kunde = LagKunde();
        kunde.ErAktiv = false;
        _repo.Kunder.Add(kunde);

        var act = () => _service.RegistrerFakturaAsync(LagFakturaRequest(kunde.Id));

        await act.Should().ThrowAsync<KundeSperretException>();
    }

    [Fact]
    public async Task RegistrerFaktura_UtenLinjer_KasterException()
    {
        var kunde = LagKunde();
        _repo.Kunder.Add(kunde);

        var request = LagFakturaRequest(kunde.Id) with { Linjer = new List<KundeFakturaLinjeRequest>() };
        var act = () => _service.RegistrerFakturaAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*fakturalinje*");
    }

    [Fact]
    public async Task RegistrerFaktura_KredittgrenseOverskredet_KasterException()
    {
        var kunde = LagKunde();
        kunde.Kredittgrense = new Belop(5000m); // Lav grense
        _repo.Kunder.Add(kunde);

        // Legg til eksisterende faktura
        _repo.Fakturaer.Add(new KundeFaktura
        {
            Id = Guid.NewGuid(),
            KundeId = kunde.Id,
            Fakturanummer = 0,
            GjenstaendeBelop = new Belop(4000m),
            Status = KundeFakturaStatus.Utstedt,
            Beskrivelse = "Eksisterende",
            Kunde = kunde
        });

        var request = LagFakturaRequest(kunde.Id);
        var act = () => _service.RegistrerFakturaAsync(request);

        await act.Should().ThrowAsync<KredittgrenseOverskredetException>();
    }

    [Fact]
    public async Task RegistrerInnbetaling_FullBetaling_SetterStatusBetalt()
    {
        var kunde = LagKunde();
        _repo.Kunder.Add(kunde);

        var faktura = LagFaktura(kunde, 12500m);
        _repo.Fakturaer.Add(faktura);

        var result = await _service.RegistrerInnbetalingAsync(new RegistrerInnbetalingRequest(
            faktura.Id,
            DateOnly.FromDateTime(DateTime.UtcNow),
            12500m,
            "REF123",
            null,
            "Bank"));

        result.Belop.Should().Be(12500m);
        faktura.Status.Should().Be(KundeFakturaStatus.Betalt);
        faktura.GjenstaendeBelop.Verdi.Should().Be(0m);
    }

    [Fact]
    public async Task RegistrerInnbetaling_DelvisBetaling_SetterStatusDelvisBetalt()
    {
        var kunde = LagKunde();
        _repo.Kunder.Add(kunde);

        var faktura = LagFaktura(kunde, 12500m);
        _repo.Fakturaer.Add(faktura);

        await _service.RegistrerInnbetalingAsync(new RegistrerInnbetalingRequest(
            faktura.Id,
            DateOnly.FromDateTime(DateTime.UtcNow),
            5000m,
            null,
            null,
            "Bank"));

        faktura.Status.Should().Be(KundeFakturaStatus.DelvisBetalt);
        faktura.GjenstaendeBelop.Verdi.Should().Be(7500m);
    }

    [Fact]
    public async Task MatchKid_GyldigKid_ReturnererInnbetaling()
    {
        var kunde = LagKunde();
        _repo.Kunder.Add(kunde);

        var kid = KidGenerator.Generer(kunde.Kundenummer, 1, KidAlgoritme.MOD10);
        var faktura = LagFaktura(kunde, 25000m);
        faktura.KidNummer = kid;
        _repo.Fakturaer.Add(faktura);

        var result = await _service.MatchKidAsync(new MatchKidRequest(
            kid, 25000m, DateOnly.FromDateTime(DateTime.UtcNow), "BANK001"));

        result.ErAutoMatchet.Should().BeTrue();
        result.KidNummer.Should().Be(kid);
        faktura.Status.Should().Be(KundeFakturaStatus.Betalt);
    }

    [Fact]
    public async Task AvskrivTap_MindreEnn3Purringer_KasterException()
    {
        var kunde = LagKunde();
        _repo.Kunder.Add(kunde);

        var faktura = LagFaktura(kunde, 10000m);
        faktura.AntallPurringer = 2;
        _repo.Fakturaer.Add(faktura);

        var act = () => _service.AvskrivTapAsync(faktura.Id, "Uerholdelig");

        await act.Should().ThrowAsync<TapAvskrivningException>()
            .WithMessage("*3 purringer*");
    }

    [Fact]
    public async Task AvskrivTap_3Purringer_SetterStatusTap()
    {
        var kunde = LagKunde();
        _repo.Kunder.Add(kunde);

        var faktura = LagFaktura(kunde, 10000m);
        faktura.AntallPurringer = 3;
        _repo.Fakturaer.Add(faktura);

        var result = await _service.AvskrivTapAsync(faktura.Id, "Uerholdelig");

        result.Status.Should().Be(KundeFakturaStatus.Tap);
    }

    [Fact]
    public void BeregnForfallsdato_Netto30_Korrekt()
    {
        var fakturadato = new DateOnly(2026, 1, 1);
        var forfall = KundeFakturaService.BeregnForfallsdato(fakturadato, KundeBetalingsbetingelse.Netto30, null);
        forfall.Should().Be(new DateOnly(2026, 1, 31));
    }

    [Fact]
    public void BeregnForfallsdato_Kontant_SammeDag()
    {
        var fakturadato = new DateOnly(2026, 3, 15);
        var forfall = KundeFakturaService.BeregnForfallsdato(fakturadato, KundeBetalingsbetingelse.Kontant, null);
        forfall.Should().Be(fakturadato);
    }

    [Fact]
    public void FlyttFraHelg_Lordag_FlyttesTilMandag()
    {
        var lordag = new DateOnly(2026, 4, 4); // Lordag
        var result = KundeFakturaService.FlyttFraHelg(lordag);
        result.DayOfWeek.Should().Be(DayOfWeek.Monday);
        result.Should().Be(new DateOnly(2026, 4, 6));
    }

    [Fact]
    public void FlyttFraHelg_Sondag_FlyttesTilMandag()
    {
        var sondag = new DateOnly(2026, 4, 5); // Sondag
        var result = KundeFakturaService.FlyttFraHelg(sondag);
        result.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public void FlyttFraHelg_Hverdag_IngenEndring()
    {
        var onsdag = new DateOnly(2026, 4, 1); // Onsdag
        var result = KundeFakturaService.FlyttFraHelg(onsdag);
        result.Should().Be(onsdag);
    }

    [Fact]
    public async Task HentAldersfordeling_KategorisererKorrekt()
    {
        var kunde = LagKunde();
        _repo.Kunder.Add(kunde);

        var iDag = new DateOnly(2026, 4, 1);

        // Ikke forfalt
        _repo.Fakturaer.Add(LagFakturaMedForfall(kunde, 1000m, iDag.AddDays(10)));
        // 0-30 dager forfalt
        _repo.Fakturaer.Add(LagFakturaMedForfall(kunde, 2000m, iDag.AddDays(-15)));
        // 31-60 dager forfalt
        _repo.Fakturaer.Add(LagFakturaMedForfall(kunde, 3000m, iDag.AddDays(-45)));
        // Over 90 dager forfalt
        _repo.Fakturaer.Add(LagFakturaMedForfall(kunde, 5000m, iDag.AddDays(-100)));

        var result = await _service.HentAldersfordelingAsync(iDag);

        result.Totalt.IkkeForfalt.Should().Be(1000m);
        result.Totalt.Dager0Til30.Should().Be(2000m);
        result.Totalt.Dager31Til60.Should().Be(3000m);
        result.Totalt.Over90Dager.Should().Be(5000m);
        result.Totalt.Totalt.Should().Be(11000m);
    }

    // --- Hjelpemetoder ---

    private static Kunde LagKunde() => new()
    {
        Id = Guid.NewGuid(),
        Kundenummer = "10001",
        Navn = "Test Bedrift AS",
        ErBedrift = true,
        ErAktiv = true,
        Kredittgrense = new Belop(100000m),
        Betalingsbetingelse = KundeBetalingsbetingelse.Netto30,
    };

    private static RegistrerKundeFakturaRequest LagFakturaRequest(Guid kundeId) => new(
        KundeId: kundeId,
        Type: KundeTransaksjonstype.Faktura,
        Fakturadato: DateOnly.FromDateTime(DateTime.UtcNow),
        Forfallsdato: null,
        Leveringsdato: null,
        Beskrivelse: "Testfaktura",
        EksternReferanse: null,
        Bestillingsnummer: null,
        Valutakode: "NOK",
        Valutakurs: null,
        Linjer: new List<KundeFakturaLinjeRequest>
        {
            new(Guid.NewGuid(), "Konsulenttjenester", 10, 1000m, "3", 0, null, null)
        });

    private static KundeFaktura LagFaktura(Kunde kunde, decimal gjenstaaende) => new()
    {
        Id = Guid.NewGuid(),
        KundeId = kunde.Id,
        Kunde = kunde,
        Fakturanummer = 1,
        Type = KundeTransaksjonstype.Faktura,
        Fakturadato = DateOnly.FromDateTime(DateTime.UtcNow),
        Forfallsdato = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
        Beskrivelse = "Test",
        BelopInklMva = new Belop(gjenstaaende),
        GjenstaendeBelop = new Belop(gjenstaaende),
        Status = KundeFakturaStatus.Utstedt,
    };

    private static KundeFaktura LagFakturaMedForfall(Kunde kunde, decimal belop, DateOnly forfallsdato) => new()
    {
        Id = Guid.NewGuid(),
        KundeId = kunde.Id,
        Kunde = kunde,
        Fakturanummer = new Random().Next(100, 9999),
        Type = KundeTransaksjonstype.Faktura,
        Fakturadato = forfallsdato.AddDays(-30),
        Forfallsdato = forfallsdato,
        Beskrivelse = "Test",
        BelopInklMva = new Belop(belop),
        GjenstaendeBelop = new Belop(belop),
        Status = KundeFakturaStatus.Utstedt,
    };
}
