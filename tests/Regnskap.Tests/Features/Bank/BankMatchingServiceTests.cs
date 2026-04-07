using FluentAssertions;
using Regnskap.Application.Features.Bank;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Bankavstemming;
using Regnskap.Domain.Features.Kundereskontro;
using Regnskap.Domain.Features.Leverandorreskontro;

namespace Regnskap.Tests.Features.Bank;

public class BankMatchingServiceTests
{
    private readonly FakeBankRepository _bankRepo;
    private readonly FakeKundeRepoForBank _kundeRepo;
    private readonly FakeLeverandorRepoForBank _leverandorRepo;
    private readonly FakeBilagRegistreringServiceForBank _bilagService;
    private readonly BankMatchingService _sut;
    private readonly Guid _bankkontoId = Guid.NewGuid();

    public BankMatchingServiceTests()
    {
        _bankRepo = new FakeBankRepository();
        _kundeRepo = new FakeKundeRepoForBank();
        _leverandorRepo = new FakeLeverandorRepoForBank();
        _bilagService = new FakeBilagRegistreringServiceForBank();
        _sut = new BankMatchingService(_bankRepo, _kundeRepo, _leverandorRepo, _bilagService);

        _bankRepo.Bankkontoer.Add(new Bankkonto
        {
            Id = _bankkontoId,
            Kontonummer = "12345678903",
            Banknavn = "DNB",
            Beskrivelse = "Drift",
            Hovedbokkontonummer = "1920",
            HovedbokkkontoId = Guid.NewGuid()
        });
    }

    private Bankbevegelse LagBevegelse(BankbevegelseRetning retning, decimal belop,
        string? kid = null, string? endToEndId = null, DateOnly? dato = null)
    {
        var bev = new Bankbevegelse
        {
            Id = Guid.NewGuid(),
            BankkontoId = _bankkontoId,
            KontoutskriftId = Guid.NewGuid(),
            Retning = retning,
            Belop = new Belop(belop),
            Bokforingsdato = dato ?? new DateOnly(2026, 3, 15),
            KidNummer = kid,
            EndToEndId = endToEndId,
            Status = BankbevegelseStatus.IkkeMatchet
        };
        _bankRepo.Bevegelser.Add(bev);
        return bev;
    }

    [Fact]
    public async Task AutoMatch_KidMatch_SetterStatusAutoMatchet()
    {
        var kid = "0001000000013";
        LagBevegelse(BankbevegelseRetning.Inn, 12500m, kid: kid);
        _kundeRepo.Fakturaer.Add(new KundeFaktura
        {
            Id = Guid.NewGuid(),
            Fakturanummer = 1,
            KidNummer = kid,
            GjenstaendeBelop = new Belop(12500m),
            Beskrivelse = "Test",
            Forfallsdato = new DateOnly(2026, 3, 10)
        });

        var antall = await _sut.AutoMatch(_bankkontoId);

        antall.Should().Be(1);
        _bankRepo.Bevegelser[0].Status.Should().Be(BankbevegelseStatus.AutoMatchet);
        _bankRepo.Bevegelser[0].MatcheType.Should().Be(MatcheType.Kid);
        _bankRepo.Bevegelser[0].MatcheKonfidens.Should().Be(1.0m);
    }

    [Fact]
    public async Task AutoMatch_KidMatchDelbetaling_Konfidens095()
    {
        var kid = "0001000000013";
        LagBevegelse(BankbevegelseRetning.Inn, 5000m, kid: kid);
        _kundeRepo.Fakturaer.Add(new KundeFaktura
        {
            Id = Guid.NewGuid(),
            Fakturanummer = 1,
            KidNummer = kid,
            GjenstaendeBelop = new Belop(12500m),
            Beskrivelse = "Test",
            Forfallsdato = new DateOnly(2026, 3, 10)
        });

        var antall = await _sut.AutoMatch(_bankkontoId);

        antall.Should().Be(1);
        _bankRepo.Bevegelser[0].MatcheKonfidens.Should().Be(0.95m);
    }

    [Fact]
    public async Task AutoMatch_EndToEndId_MatcherLeverandorBetaling()
    {
        var e2eId = "PAY-2026-001";
        LagBevegelse(BankbevegelseRetning.Ut, 8500m, endToEndId: e2eId);
        _leverandorRepo.Betalinger.Add(new LeverandorBetaling
        {
            Id = Guid.NewGuid(),
            LeverandorFakturaId = Guid.NewGuid(),
            Bankreferanse = e2eId,
            Belop = new Belop(8500m),
            Betalingsdato = new DateOnly(2026, 3, 14)
        });

        var antall = await _sut.AutoMatch(_bankkontoId);

        antall.Should().Be(1);
        _bankRepo.Bevegelser[0].MatcheType.Should().Be(MatcheType.Referanse);
        _bankRepo.Bevegelser[0].MatcheKonfidens.Should().Be(0.9m);
    }

    [Fact]
    public async Task AutoMatch_BelopOgDato_MatcherNaarEttTreff()
    {
        LagBevegelse(BankbevegelseRetning.Inn, 7500m, dato: new DateOnly(2026, 3, 15));
        _kundeRepo.Fakturaer.Add(new KundeFaktura
        {
            Id = Guid.NewGuid(),
            Fakturanummer = 10,
            GjenstaendeBelop = new Belop(7500m),
            Beskrivelse = "Test",
            Forfallsdato = new DateOnly(2026, 3, 13)
        });

        var antall = await _sut.AutoMatch(_bankkontoId);

        antall.Should().Be(1);
        _bankRepo.Bevegelser[0].MatcheType.Should().Be(MatcheType.Belop);
        _bankRepo.Bevegelser[0].MatcheKonfidens.Should().Be(0.7m);
    }

    [Fact]
    public async Task AutoMatch_BelopOgDato_MatcherIkkeNaarFlereTreff()
    {
        LagBevegelse(BankbevegelseRetning.Inn, 7500m, dato: new DateOnly(2026, 3, 15));
        _kundeRepo.Fakturaer.Add(new KundeFaktura
        {
            Id = Guid.NewGuid(), Fakturanummer = 10,
            GjenstaendeBelop = new Belop(7500m), Beskrivelse = "A",
            Forfallsdato = new DateOnly(2026, 3, 13)
        });
        _kundeRepo.Fakturaer.Add(new KundeFaktura
        {
            Id = Guid.NewGuid(), Fakturanummer = 11,
            GjenstaendeBelop = new Belop(7500m), Beskrivelse = "B",
            Forfallsdato = new DateOnly(2026, 3, 14)
        });

        var antall = await _sut.AutoMatch(_bankkontoId);

        antall.Should().Be(0);
        _bankRepo.Bevegelser[0].Status.Should().Be(BankbevegelseStatus.IkkeMatchet);
    }

    [Fact]
    public async Task ManuellMatch_SetterStatusOgOppretterMatch()
    {
        var bev = LagBevegelse(BankbevegelseRetning.Inn, 5000m);
        var fakturaId = Guid.NewGuid();

        await _sut.Match(bev.Id, new ManuellMatchRequest(fakturaId, null, null, "Manuell match test"));

        bev.Status.Should().Be(BankbevegelseStatus.ManueltMatchet);
        bev.MatcheType.Should().Be(MatcheType.Manuell);
        _bankRepo.Matchinger.Should().HaveCount(1);
        _bankRepo.Matchinger[0].KundeFakturaId.Should().Be(fakturaId);
    }

    [Fact]
    public async Task ManuellMatch_AlleredeMatchet_KasterException()
    {
        var bev = LagBevegelse(BankbevegelseRetning.Inn, 5000m);
        bev.Status = BankbevegelseStatus.AutoMatchet;

        var act = () => _sut.Match(bev.Id, new ManuellMatchRequest(Guid.NewGuid(), null, null, null));
        await act.Should().ThrowAsync<MatchAlleredeMatchetException>();
    }

    [Fact]
    public async Task Splitt_DelerOgMatcherKorrekt()
    {
        var bev = LagBevegelse(BankbevegelseRetning.Inn, 25000m);
        var faktura1 = Guid.NewGuid();
        var faktura2 = Guid.NewGuid();

        var request = new SplittMatchRequest(new List<SplittLinjeRequest>
        {
            new(15000m, faktura1, null, null, "Del 1"),
            new(10000m, faktura2, null, null, "Del 2")
        });

        await _sut.Splitt(bev.Id, request);

        bev.Status.Should().Be(BankbevegelseStatus.Splittet);
        _bankRepo.Matchinger.Should().HaveCount(2);
        _bankRepo.Matchinger.Sum(m => m.Belop.Verdi).Should().Be(25000m);
    }

    [Fact]
    public async Task Splitt_SumStemmerIkke_KasterException()
    {
        var bev = LagBevegelse(BankbevegelseRetning.Inn, 25000m);

        var request = new SplittMatchRequest(new List<SplittLinjeRequest>
        {
            new(15000m, Guid.NewGuid(), null, null, "Del 1"),
            new(5000m, Guid.NewGuid(), null, null, "Del 2")
        });

        var act = () => _sut.Splitt(bev.Id, request);
        await act.Should().ThrowAsync<SplittSumFeilException>();
    }

    [Fact]
    public async Task FjernMatch_TilbakestillerTilIkkeMatchet()
    {
        var bev = LagBevegelse(BankbevegelseRetning.Inn, 5000m);
        await _sut.Match(bev.Id, new ManuellMatchRequest(Guid.NewGuid(), null, null, null));
        bev.Status.Should().Be(BankbevegelseStatus.ManueltMatchet);

        await _sut.FjernMatch(bev.Id);

        bev.Status.Should().Be(BankbevegelseStatus.IkkeMatchet);
        bev.MatcheType.Should().BeNull();
        _bankRepo.Matchinger.Should().BeEmpty();
    }
}
