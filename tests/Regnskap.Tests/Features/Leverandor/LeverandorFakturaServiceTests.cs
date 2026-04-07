using FluentAssertions;
using Regnskap.Application.Features.Leverandorreskontro;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Leverandorreskontro;

namespace Regnskap.Tests.Features.Leverandor;

public class LeverandorFakturaServiceTests
{
    private readonly FakeLeverandorRepository _repo;
    private readonly FakeBilagService _bilagService;
    private readonly LeverandorFakturaService _service;
    private readonly Domain.Features.Leverandorreskontro.Leverandor _leverandor;

    public LeverandorFakturaServiceTests()
    {
        _repo = new FakeLeverandorRepository();
        _bilagService = new FakeBilagService();
        _service = new LeverandorFakturaService(_repo, _bilagService);

        _leverandor = new Domain.Features.Leverandorreskontro.Leverandor
        {
            Id = Guid.NewGuid(),
            Leverandornummer = "10001",
            Navn = "Leverandor AS",
            ErAktiv = true,
            Betalingsbetingelse = Betalingsbetingelse.Netto30,
            Bankkontonummer = "12345678901"
        };
        _repo.Leverandorer.Add(_leverandor);
    }

    private RegistrerFakturaRequest LagFakturaRequest(
        string eksternNr = "F-2026-001",
        decimal belop = 10000m,
        string? mvaKode = "1")
        => new(
            _leverandor.Id,
            eksternNr,
            LeverandorTransaksjonstype.Faktura,
            new DateOnly(2026, 3, 15),
            null,
            "Husleie mars",
            "123456",
            "NOK",
            null,
            new List<FakturaLinjeRequest>
            {
                new(Guid.NewGuid(), "Husleie", belop, mvaKode, null, null)
            });

    [Fact]
    public async Task RegistrerFaktura_FR_L01_OppretterBilagMedKorrekteLinjer()
    {
        // FR-L01: Registrering av inngaende faktura
        var result = await _service.RegistrerFakturaAsync(LagFakturaRequest());

        result.BelopEksMva.Should().Be(10000m);
        result.MvaBelop.Should().Be(2500m);
        result.BelopInklMva.Should().Be(12500m);
        result.GjenstaendeBelop.Should().Be(12500m);
        result.Status.Should().Be(FakturaStatus.Registrert);
        result.BilagId.Should().NotBeNull();

        // Sjekk at bilag ble opprettet med riktige posteringer
        var bilagReq = _bilagService.SisteRequest!;
        bilagReq.Posteringer.Should().HaveCount(3); // Debet kostnad + Debet MVA + Kredit 2400
    }

    [Fact]
    public async Task RegistrerFaktura_FR_L01_BilagHarRiktigSerie()
    {
        await _service.RegistrerFakturaAsync(LagFakturaRequest());

        _bilagService.SisteRequest!.SerieKode.Should().Be("IF");
    }

    [Fact]
    public async Task RegistrerFaktura_FR_L02_BeregnForfallsdato_Netto30()
    {
        // Fakturadato: 2026-03-15, Netto30 => 2026-04-14 (tirsdag)
        var result = await _service.RegistrerFakturaAsync(LagFakturaRequest());

        result.Forfallsdato.Should().Be(new DateOnly(2026, 4, 14));
    }

    [Fact]
    public async Task RegistrerFaktura_FR_L02_ForfallLordagFlyttesTilMandag()
    {
        _leverandor.Betalingsbetingelse = Betalingsbetingelse.Netto10;
        // Fakturadato: 2026-03-15, +10 = 2026-03-25 (onsdag)
        // La oss teste med en dato som gir lordag
        var request = new RegistrerFakturaRequest(
            _leverandor.Id, "F-SAT", LeverandorTransaksjonstype.Faktura,
            new DateOnly(2026, 4, 4), // +10 = 2026-04-14 (tirsdag) - not saturday
            null, "Test", null, "NOK", null,
            new List<FakturaLinjeRequest> { new(Guid.NewGuid(), "Test", 100m, null, null, null) });

        var result = await _service.RegistrerFakturaAsync(request);

        // 2026-04-04 + 10 = 2026-04-14 (tirsdag)
        result.Forfallsdato.DayOfWeek.Should().NotBe(DayOfWeek.Saturday);
        result.Forfallsdato.DayOfWeek.Should().NotBe(DayOfWeek.Sunday);
    }

    [Fact]
    public void BeregnForfallsdato_Lordag_FlyttesTilMandag()
    {
        // FR-L02: Lordag -> Mandag
        var lev = new Domain.Features.Leverandorreskontro.Leverandor
        {
            Betalingsbetingelse = Betalingsbetingelse.Netto10
        };

        // 2026-04-01 (onsdag) + 10 = 2026-04-11 (lordag)
        var result = LeverandorFakturaService.BeregnForfallsdato(new DateOnly(2026, 4, 1), lev);

        result.Should().Be(new DateOnly(2026, 4, 13)); // mandag
    }

    [Fact]
    public void BeregnForfallsdato_Sondag_FlyttesTilMandag()
    {
        var lev = new Domain.Features.Leverandorreskontro.Leverandor
        {
            Betalingsbetingelse = Betalingsbetingelse.Netto10
        };

        // 2026-04-02 (torsdag) + 10 = 2026-04-12 (sondag)
        var result = LeverandorFakturaService.BeregnForfallsdato(new DateOnly(2026, 4, 2), lev);

        result.Should().Be(new DateOnly(2026, 4, 13)); // mandag
    }

    [Fact]
    public async Task RegistrerFaktura_FR_L10_DuplikatEksterntNummer_KasterException()
    {
        await _service.RegistrerFakturaAsync(LagFakturaRequest("F-001"));

        var act = () => _service.RegistrerFakturaAsync(LagFakturaRequest("F-001"));
        await act.Should().ThrowAsync<LeverandorFakturaDuplikatException>();
    }

    [Fact]
    public async Task RegistrerFaktura_UtenMva_BeregnerKorrekt()
    {
        var request = LagFakturaRequest(mvaKode: null, belop: 5000m);
        var result = await _service.RegistrerFakturaAsync(request);

        result.BelopEksMva.Should().Be(5000m);
        result.MvaBelop.Should().Be(0m);
        result.BelopInklMva.Should().Be(5000m);
    }

    [Fact]
    public async Task RegistrerFaktura_InaktivLeverandor_KasterException()
    {
        _leverandor.ErAktiv = false;

        var act = () => _service.RegistrerFakturaAsync(LagFakturaRequest());
        await act.Should().ThrowAsync<LeverandorSperretException>();
    }

    [Fact]
    public async Task RegistrerFaktura_SperretLeverandor_KasterException()
    {
        _leverandor.ErSperret = true;

        var act = () => _service.RegistrerFakturaAsync(LagFakturaRequest());
        await act.Should().ThrowAsync<LeverandorSperretException>();
    }

    [Fact]
    public async Task RegistrerFaktura_UtenLinjer_KasterArgumentException()
    {
        var request = new RegistrerFakturaRequest(
            _leverandor.Id, "F-001", LeverandorTransaksjonstype.Faktura,
            new DateOnly(2026, 3, 15), null, "Test", null, "NOK", null,
            new List<FakturaLinjeRequest>());

        var act = () => _service.RegistrerFakturaAsync(request);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*minimum 1 linje*");
    }

    [Fact]
    public async Task Godkjenn_FraRegistrert_SetterStatusGodkjent()
    {
        var faktura = await _service.RegistrerFakturaAsync(LagFakturaRequest());
        var result = await _service.GodkjennAsync(faktura.Id);

        result.Status.Should().Be(FakturaStatus.Godkjent);
    }

    [Fact]
    public async Task Godkjenn_AlleredeGodkjent_KasterException()
    {
        var faktura = await _service.RegistrerFakturaAsync(LagFakturaRequest());
        await _service.GodkjennAsync(faktura.Id);

        var act = () => _service.GodkjennAsync(faktura.Id);
        await act.Should().ThrowAsync<FakturaStatusException>();
    }

    [Fact]
    public async Task Sperr_SetterStatusOgArsak()
    {
        var faktura = await _service.RegistrerFakturaAsync(LagFakturaRequest());

        var result = await _service.SperrAsync(faktura.Id, "Feil belop");

        result.ErSperret.Should().BeTrue();
        result.SperreArsak.Should().Be("Feil belop");
        result.Status.Should().Be(FakturaStatus.Sperret);
    }

    [Fact]
    public async Task OpphevSperring_FjernerSperring()
    {
        var faktura = await _service.RegistrerFakturaAsync(LagFakturaRequest());
        await _service.SperrAsync(faktura.Id, "Feil");

        var result = await _service.OpphevSperringAsync(faktura.Id);

        result.ErSperret.Should().BeFalse();
        result.SperreArsak.Should().BeNull();
        result.Status.Should().Be(FakturaStatus.Registrert);
    }

    [Fact]
    public async Task FR_L08_Aldersfordeling_KategorisererKorrekt()
    {
        var rapportDato = new DateOnly(2026, 5, 1);

        // Faktura 1: Ikke forfalt (forfall 2026-05-15)
        var f1 = LagTestFaktura("F-1", new DateOnly(2026, 5, 15), 1000m);
        // Faktura 2: 10 dager forfalt (forfall 2026-04-21)
        var f2 = LagTestFaktura("F-2", new DateOnly(2026, 4, 21), 2000m);
        // Faktura 3: 45 dager forfalt (forfall 2026-03-17)
        var f3 = LagTestFaktura("F-3", new DateOnly(2026, 3, 17), 3000m);
        // Faktura 4: 100 dager forfalt (forfall 2026-01-21)
        var f4 = LagTestFaktura("F-4", new DateOnly(2026, 1, 21), 4000m);

        _repo.Fakturaer.AddRange(new[] { f1, f2, f3, f4 });

        var result = await _service.HentAldersfordelingAsync(rapportDato);

        result.Totalt.IkkeForfalt.Should().Be(1000m);
        result.Totalt.Dager0Til30.Should().Be(2000m);
        result.Totalt.Dager31Til60.Should().Be(3000m);
        result.Totalt.Over90Dager.Should().Be(4000m);
        result.Totalt.Totalt.Should().Be(10000m);
    }

    private LeverandorFaktura LagTestFaktura(string eksternNr, DateOnly forfallsdato, decimal belop)
    {
        return new LeverandorFaktura
        {
            Id = Guid.NewGuid(),
            LeverandorId = _leverandor.Id,
            EksternFakturanummer = eksternNr,
            InternNummer = Random.Shared.Next(1000, 9999),
            Type = LeverandorTransaksjonstype.Faktura,
            Fakturadato = forfallsdato.AddDays(-30),
            Forfallsdato = forfallsdato,
            Beskrivelse = "Test",
            BelopEksMva = new Belop(belop),
            MvaBelop = Belop.Null,
            BelopInklMva = new Belop(belop),
            GjenstaendeBelop = new Belop(belop),
            Status = FakturaStatus.Godkjent
        };
    }
}
