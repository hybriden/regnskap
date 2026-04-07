using FluentAssertions;
using Regnskap.Application.Features.Kundereskontro;
using Regnskap.Domain.Features.Kundereskontro;

namespace Regnskap.Tests.Features.Kundereskontro;

public class KundeServiceTests
{
    private readonly FakeKundeReskontroRepository _repo = new();
    private readonly KundeService _service;

    public KundeServiceTests()
    {
        _service = new KundeService(_repo);
    }

    [Fact]
    public async Task OpprettKunde_GyldigRequest_ReturnererDto()
    {
        var request = LagOpprettKundeRequest();

        var result = await _service.OpprettAsync(request);

        result.Kundenummer.Should().Be("10001");
        result.Navn.Should().Be("Test Bedrift AS");
        result.ErBedrift.Should().BeTrue();
        _repo.Kunder.Should().HaveCount(1);
    }

    [Fact]
    public async Task OpprettKunde_DuplikatKundenummer_KasterException()
    {
        var request = LagOpprettKundeRequest();
        await _service.OpprettAsync(request);

        var act = () => _service.OpprettAsync(request);

        await act.Should().ThrowAsync<KundenummerEksistererException>();
    }

    [Fact]
    public async Task OpprettKunde_BedriftUtenOrgnr_KasterException()
    {
        var request = LagOpprettKundeRequest() with { Organisasjonsnummer = null };

        var act = () => _service.OpprettAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Organisasjonsnummer*");
    }

    [Fact]
    public async Task OpprettKunde_EgendefinertBetingelsUtenFrist_KasterException()
    {
        var request = LagOpprettKundeRequest() with
        {
            Betalingsbetingelse = KundeBetalingsbetingelse.Egendefinert,
            EgendefinertBetalingsfrist = null
        };

        var act = () => _service.OpprettAsync(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*EgendefinertBetalingsfrist*");
    }

    [Fact]
    public async Task SlettKunde_SetterIsDeleted()
    {
        var request = LagOpprettKundeRequest();
        var kunde = await _service.OpprettAsync(request);

        await _service.SlettAsync(kunde.Id);

        _repo.Kunder.First().IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task HentSaldo_ReturnerSumGjenstaende()
    {
        var request = LagOpprettKundeRequest();
        var kunde = await _service.OpprettAsync(request);

        // Legg til apne fakturaer
        _repo.Fakturaer.Add(new KundeFaktura
        {
            Id = Guid.NewGuid(),
            KundeId = kunde.Id,
            Fakturanummer = 1,
            GjenstaendeBelop = new Domain.Common.Belop(25000m),
            Status = KundeFakturaStatus.Utstedt,
            Beskrivelse = "Faktura 1",
            Kunde = _repo.Kunder.First()
        });
        _repo.Fakturaer.Add(new KundeFaktura
        {
            Id = Guid.NewGuid(),
            KundeId = kunde.Id,
            Fakturanummer = 2,
            GjenstaendeBelop = new Domain.Common.Belop(15000m),
            Status = KundeFakturaStatus.DelvisBetalt,
            Beskrivelse = "Faktura 2",
            Kunde = _repo.Kunder.First()
        });

        var saldo = await _service.HentSaldoAsync(kunde.Id);

        saldo.Should().Be(40000m);
    }

    private static OpprettKundeRequest LagOpprettKundeRequest() => new(
        Kundenummer: "10001",
        Navn: "Test Bedrift AS",
        ErBedrift: true,
        Organisasjonsnummer: "123456789",
        Fodselsnummer: null,
        Adresse1: "Testveien 1",
        Adresse2: null,
        Postnummer: "0123",
        Poststed: "Oslo",
        Landkode: "NO",
        Kontaktperson: "Ola Nordmann",
        Telefon: "12345678",
        Epost: "ola@test.no",
        Betalingsbetingelse: KundeBetalingsbetingelse.Netto30,
        EgendefinertBetalingsfrist: null,
        StandardKontoId: null,
        StandardMvaKode: "3",
        Kredittgrense: 100000m,
        PeppolId: null,
        KanMottaEhf: false);
}
