using FluentAssertions;
using Regnskap.Application.Features.Fakturering;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Fakturering;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kundereskontro;

namespace Regnskap.Tests.Features.Fakturering;

public class FaktureringServiceTests
{
    private readonly FakeFakturaRepository _fakturaRepo;
    private readonly FakeKundeRepo _kundeRepo;
    private readonly FakeHovedbokRepo _hovedbokRepo;
    private readonly FakeBilagRegistreringServiceForFaktura _bilagService;
    private readonly FaktureringService _service;
    private readonly Kunde _testKunde;

    public FaktureringServiceTests()
    {
        _fakturaRepo = new FakeFakturaRepository();
        _kundeRepo = new FakeKundeRepo();
        _hovedbokRepo = new FakeHovedbokRepo();
        _bilagService = new FakeBilagRegistreringServiceForFaktura();
        _service = new FaktureringService(_fakturaRepo, _hovedbokRepo, _kundeRepo, _bilagService);

        _testKunde = new Kunde
        {
            Id = Guid.NewGuid(),
            Kundenummer = "000100",
            Navn = "Test AS",
            ErAktiv = true,
            ErSperret = false,
            Betalingsbetingelse = KundeBetalingsbetingelse.Netto30,
            Landkode = "NO"
        };
        _kundeRepo.Kunder.Add(_testKunde);

        _fakturaRepo.Selskapsinfo = new Selskapsinfo
        {
            Id = Guid.NewGuid(),
            Navn = "Mitt Firma AS",
            Organisasjonsnummer = "123456789",
            Bankkontonummer = "12345678901",
            Adresse1 = "Testgata 1",
            Postnummer = "0123",
            Poststed = "Oslo",
            ErMvaRegistrert = true
        };

        // Aapen periode
        _hovedbokRepo.PeriodeForDato = new Regnskapsperiode
        {
            Id = Guid.NewGuid(),
            Ar = DateTime.Today.Year,
            Periode = DateTime.Today.Month,
            FraDato = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1),
            TilDato = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month)),
            Status = PeriodeStatus.Apen
        };
    }

    [Fact]
    public async Task OpprettFaktura_MedGyldige_Data_OpprretterUtkast()
    {
        var request = LagOpprettRequest();

        var faktura = await _service.OpprettFakturaAsync(request);

        faktura.Should().NotBeNull();
        faktura.Status.Should().Be(FakturaStatus.Utkast);
        faktura.KundeId.Should().Be(_testKunde.Id);
        faktura.Linjer.Should().HaveCount(1);
        faktura.BelopEksMva.Verdi.Should().Be(1000m);
        faktura.MvaBelop.Verdi.Should().Be(250m);
        faktura.BelopInklMva.Verdi.Should().Be(1250m);
        faktura.Fakturanummer.Should().BeNull(); // Skal ikke ha nummer som utkast
    }

    [Fact]
    public async Task OpprettFaktura_SperretKunde_KasterException()
    {
        _testKunde.ErSperret = true;
        var request = LagOpprettRequest();

        var act = () => _service.OpprettFakturaAsync(request);

        await act.Should().ThrowAsync<FakturaKundeSperretException>();
    }

    [Fact]
    public async Task OpprettFaktura_IngenLinjer_KasterException()
    {
        var request = new OpprettFakturaRequest(
            KundeId: _testKunde.Id,
            Leveringsdato: null,
            LeveringsperiodeSlutt: null,
            Bestillingsnummer: null,
            KjopersReferanse: null,
            VaarReferanse: null,
            EksternReferanse: null,
            Merknad: null,
            Linjer: new List<FakturaLinjeRequest>()
        );

        var act = () => _service.OpprettFakturaAsync(request);

        await act.Should().ThrowAsync<FakturaIngenLinjerException>();
    }

    [Fact]
    public async Task OpprettFaktura_IkkeEksisterendeKunde_KasterException()
    {
        var request = new OpprettFakturaRequest(
            KundeId: Guid.NewGuid(),
            Leveringsdato: null,
            LeveringsperiodeSlutt: null,
            Bestillingsnummer: null,
            KjopersReferanse: null,
            VaarReferanse: null,
            EksternReferanse: null,
            Merknad: null,
            Linjer: new List<FakturaLinjeRequest>
            {
                new("Test", 1, Enhet.Stykk, 100m, "3", Guid.NewGuid())
            }
        );

        var act = () => _service.OpprettFakturaAsync(request);

        await act.Should().ThrowAsync<FakturaKundeIkkeFunnetException>();
    }

    [Fact]
    public async Task UtstedeFaktura_TildelNummerOgKid()
    {
        var request = LagOpprettRequest();
        var faktura = await _service.OpprettFakturaAsync(request);

        var utstedt = await _service.UtstedeFakturaAsync(faktura.Id);

        utstedt.Status.Should().Be(FakturaStatus.Utstedt);
        utstedt.Fakturanummer.Should().Be(1);
        utstedt.FakturanummerAr.Should().Be(DateTime.Today.Year);
        utstedt.KidNummer.Should().NotBeNullOrEmpty();
        utstedt.Fakturadato.Should().NotBeNull();
        utstedt.Forfallsdato.Should().NotBeNull();
        utstedt.Bankkontonummer.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UtstedeFaktura_AlleredeUtstedt_KasterException()
    {
        var request = LagOpprettRequest();
        var faktura = await _service.OpprettFakturaAsync(request);
        await _service.UtstedeFakturaAsync(faktura.Id);

        var act = () => _service.UtstedeFakturaAsync(faktura.Id);

        await act.Should().ThrowAsync<FakturaIkkeUtkastException>();
    }

    [Fact]
    public async Task UtstedeFaktura_LukketPeriode_KasterException()
    {
        _hovedbokRepo.PeriodeForDato!.Status = PeriodeStatus.Lukket;
        var request = LagOpprettRequest();
        var faktura = await _service.OpprettFakturaAsync(request);

        var act = () => _service.UtstedeFakturaAsync(faktura.Id);

        await act.Should().ThrowAsync<FakturaPeriodeLukketException>();
    }

    [Fact]
    public async Task OpprettKreditnota_FullKreditering()
    {
        var request = LagOpprettRequest();
        var faktura = await _service.OpprettFakturaAsync(request);
        await _service.UtstedeFakturaAsync(faktura.Id);

        var kreditnota = await _service.OpprettKreditnotaAsync(faktura.Id, new OpprettKreditnotaRequest(
            Krediteringsaarsak: "Feil vare levert",
            KjopersReferanse: null,
            Linjer: null // Full kreditering
        ));

        kreditnota.Dokumenttype.Should().Be(FakturaDokumenttype.Kreditnota);
        kreditnota.KreditertFakturaId.Should().Be(faktura.Id);
        kreditnota.BelopEksMva.Verdi.Should().Be(faktura.BelopEksMva.Verdi);
        kreditnota.BelopInklMva.Verdi.Should().Be(faktura.BelopInklMva.Verdi);
        kreditnota.Linjer.Should().HaveCount(faktura.Linjer.Count);
    }

    [Fact]
    public async Task KansellerFaktura_UtkastKanKanselleres()
    {
        var request = LagOpprettRequest();
        var faktura = await _service.OpprettFakturaAsync(request);

        await _service.KansellerFakturaAsync(faktura.Id);

        faktura.Status.Should().Be(FakturaStatus.Kansellert);
        faktura.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task KansellerFaktura_UtstedtKanIkkeKanselleres()
    {
        var request = LagOpprettRequest();
        var faktura = await _service.OpprettFakturaAsync(request);
        await _service.UtstedeFakturaAsync(faktura.Id);

        var act = () => _service.KansellerFakturaAsync(faktura.Id);

        await act.Should().ThrowAsync<FakturaIkkeUtkastException>();
    }

    [Fact]
    public async Task UtstedeFaktura_FortlopendeNummerering()
    {
        var req1 = LagOpprettRequest();
        var req2 = LagOpprettRequest();
        var f1 = await _service.OpprettFakturaAsync(req1);
        var f2 = await _service.OpprettFakturaAsync(req2);

        await _service.UtstedeFakturaAsync(f1.Id);
        await _service.UtstedeFakturaAsync(f2.Id);

        f1.Fakturanummer.Should().Be(1);
        f2.Fakturanummer.Should().Be(2);
    }

    private OpprettFakturaRequest LagOpprettRequest() => new(
        KundeId: _testKunde.Id,
        Leveringsdato: null,
        LeveringsperiodeSlutt: null,
        Bestillingsnummer: null,
        KjopersReferanse: "REF-001",
        VaarReferanse: null,
        EksternReferanse: null,
        Merknad: null,
        Linjer: new List<FakturaLinjeRequest>
        {
            new("Konsulenttime", 10, Enhet.Timer, 100m, "3", Guid.NewGuid())
        }
    );
}
