using FluentAssertions;
using Regnskap.Application.Features.Leverandorreskontro;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Leverandorreskontro;

namespace Regnskap.Tests.Features.Leverandor;

public class BetalingsforslagServiceTests
{
    private readonly FakeLeverandorRepository _repo;
    private readonly BetalingsforslagService _service;
    private readonly Domain.Features.Leverandorreskontro.Leverandor _leverandor;

    public BetalingsforslagServiceTests()
    {
        _repo = new FakeLeverandorRepository();
        _service = new BetalingsforslagService(_repo);

        _leverandor = new Domain.Features.Leverandorreskontro.Leverandor
        {
            Id = Guid.NewGuid(),
            Leverandornummer = "10001",
            Navn = "Leverandor AS",
            ErAktiv = true,
            Bankkontonummer = "12345678901"
        };
        _repo.Leverandorer.Add(_leverandor);
    }

    private LeverandorFaktura LagFaktura(string eksternNr, DateOnly forfallsdato, decimal belop,
        FakturaStatus status = FakturaStatus.Godkjent)
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
            Beskrivelse = $"Faktura {eksternNr}",
            BelopEksMva = new Belop(belop),
            MvaBelop = Belop.Null,
            BelopInklMva = new Belop(belop),
            GjenstaendeBelop = new Belop(belop),
            Status = status,
            KidNummer = "1234567"
        };
    }

    [Fact]
    public async Task FR_L04_GenererForslag_InkludererForfaltGodkjenteFakturaer()
    {
        var f1 = LagFaktura("F-1", new DateOnly(2026, 4, 1), 5000m);
        var f2 = LagFaktura("F-2", new DateOnly(2026, 4, 15), 3000m);
        var f3 = LagFaktura("F-3", new DateOnly(2026, 5, 1), 2000m); // Etter grensedato
        _repo.Fakturaer.AddRange(new[] { f1, f2, f3 });

        var request = new GenererBetalingsforslagRequest(
            new DateOnly(2026, 4, 15),
            new DateOnly(2026, 4, 20),
            null, "98765432101", true, null);

        var result = await _service.GenererAsync(request);

        result.AntallBetalinger.Should().Be(2);
        result.TotalBelop.Should().Be(8000m);
        result.Status.Should().Be(BetalingsforslagStatus.Utkast);
    }

    [Fact]
    public async Task FR_L04_GenererForslag_EkskludererSperretLeverandor()
    {
        _leverandor.ErSperret = true;
        var f1 = LagFaktura("F-1", new DateOnly(2026, 4, 1), 5000m);
        _repo.Fakturaer.Add(f1);

        var request = new GenererBetalingsforslagRequest(
            new DateOnly(2026, 4, 15), new DateOnly(2026, 4, 20),
            null, null, true, null);

        var result = await _service.GenererAsync(request);

        result.AntallBetalinger.Should().Be(0);
    }

    [Fact]
    public async Task FR_L04_GenererForslag_EkskludererSperretFaktura()
    {
        var f1 = LagFaktura("F-1", new DateOnly(2026, 4, 1), 5000m);
        f1.ErSperret = true;
        _repo.Fakturaer.Add(f1);

        var request = new GenererBetalingsforslagRequest(
            new DateOnly(2026, 4, 15), new DateOnly(2026, 4, 20),
            null, null, true, null);

        var result = await _service.GenererAsync(request);

        result.AntallBetalinger.Should().Be(0);
    }

    [Fact]
    public async Task Godkjenn_FraUtkast_SetterStatusGodkjent()
    {
        var f1 = LagFaktura("F-1", new DateOnly(2026, 4, 1), 5000m);
        _repo.Fakturaer.Add(f1);

        var genRequest = new GenererBetalingsforslagRequest(
            new DateOnly(2026, 4, 15), new DateOnly(2026, 4, 20),
            null, null, true, null);
        var forslag = await _service.GenererAsync(genRequest);

        var result = await _service.GodkjennAsync(forslag.Id, "admin");

        result.Status.Should().Be(BetalingsforslagStatus.Godkjent);
        result.GodkjentAv.Should().Be("admin");
    }

    [Fact]
    public async Task Kanseller_KunFraUtkast()
    {
        var f1 = LagFaktura("F-1", new DateOnly(2026, 4, 1), 5000m);
        _repo.Fakturaer.Add(f1);

        var forslag = await _service.GenererAsync(new GenererBetalingsforslagRequest(
            new DateOnly(2026, 4, 15), new DateOnly(2026, 4, 20),
            null, null, true, null));

        await _service.KansellerAsync(forslag.Id);

        var oppdatert = _repo.Betalingsforslag.First();
        oppdatert.Status.Should().Be(BetalingsforslagStatus.Kansellert);
    }

    [Fact]
    public async Task Kanseller_IkkeUtkast_KasterException()
    {
        var f1 = LagFaktura("F-1", new DateOnly(2026, 4, 1), 5000m);
        _repo.Fakturaer.Add(f1);

        var forslag = await _service.GenererAsync(new GenererBetalingsforslagRequest(
            new DateOnly(2026, 4, 15), new DateOnly(2026, 4, 20),
            null, null, true, null));
        await _service.GodkjennAsync(forslag.Id, "admin");

        var act = () => _service.KansellerAsync(forslag.Id);
        await act.Should().ThrowAsync<BetalingsforslagStatusException>();
    }

    [Fact]
    public async Task EkskluderLinje_OppdatererTotaler()
    {
        var f1 = LagFaktura("F-1", new DateOnly(2026, 4, 1), 5000m);
        var f2 = LagFaktura("F-2", new DateOnly(2026, 4, 1), 3000m);
        _repo.Fakturaer.AddRange(new[] { f1, f2 });

        var forslag = await _service.GenererAsync(new GenererBetalingsforslagRequest(
            new DateOnly(2026, 4, 15), new DateOnly(2026, 4, 20),
            null, null, true, null));

        var linjeId = forslag.Linjer.First().Id;
        await _service.EkskluderLinjeAsync(forslag.Id, linjeId);

        var oppdatert = _repo.Betalingsforslag.First();
        oppdatert.AntallBetalinger.Should().Be(1);
    }

    [Fact]
    public async Task FR_L05_GenererFil_KreverGodkjentStatus()
    {
        var f1 = LagFaktura("F-1", new DateOnly(2026, 4, 1), 5000m);
        _repo.Fakturaer.Add(f1);

        var forslag = await _service.GenererAsync(new GenererBetalingsforslagRequest(
            new DateOnly(2026, 4, 15), new DateOnly(2026, 4, 20),
            null, "98765432101", true, null));

        // Forsok fil fra Utkast
        var act = () => _service.GenererFilAsync(forslag.Id);
        await act.Should().ThrowAsync<BetalingsforslagStatusException>();
    }

    [Fact]
    public async Task FR_L05_GenererFil_ReturnererPain001Xml()
    {
        var f1 = LagFaktura("F-1", new DateOnly(2026, 4, 1), 5000m);
        _repo.Fakturaer.Add(f1);

        var forslag = await _service.GenererAsync(new GenererBetalingsforslagRequest(
            new DateOnly(2026, 4, 15), new DateOnly(2026, 4, 20),
            null, "98765432101", true, null));
        await _service.GodkjennAsync(forslag.Id, "admin");

        var fil = await _service.GenererFilAsync(forslag.Id);

        fil.Should().NotBeEmpty();
        var xml = System.Text.Encoding.UTF8.GetString(fil);
        xml.Should().Contain("pain.001.001.03");
        xml.Should().Contain("CstmrCdtTrfInitn");
        xml.Should().Contain("1234567"); // KID
    }
}
