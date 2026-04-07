using FluentAssertions;
using Regnskap.Application.Features.Mva;
using Regnskap.Domain.Features.Mva;

namespace Regnskap.Tests.Features.Mva;

public class MvaAvstemmingServiceTests
{
    private readonly FakeMvaRepository _repo;
    private readonly MvaAvstemmingService _service;

    public MvaAvstemmingServiceTests()
    {
        _repo = new FakeMvaRepository();
        _service = new MvaAvstemmingService(_repo);
    }

    [Fact]
    public async Task KjorAvstemming_MedKontoSaldoer_ReturnererLinjer()
    {
        var termin = LagTermin(2026, 1);
        _repo.Terminer.Add(termin);

        _repo.KontoSaldoer.AddRange(new[]
        {
            new MvaKontoSaldoDto("2700", "Utgaende MVA 25%", 0m, 0m, 25_000m, -25_000m),
            new MvaKontoSaldoDto("2710", "Inngaende MVA 25%", 0m, 10_000m, 0m, 10_000m),
        });

        var result = await _service.KjorAvstemmingAsync(termin.Id);

        result.Linjer.Should().HaveCount(2);
        result.MvaTerminId.Should().Be(termin.Id);
    }

    [Fact]
    public async Task KjorAvstemming_TerminIkkeFunnet_KasterException()
    {
        var act = () => _service.KjorAvstemmingAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<MvaTerminIkkeFunnetException>();
    }

    [Fact]
    public async Task HentAvstemming_MedEksisterende_ReturnererSiste()
    {
        var termin = LagTermin(2026, 1);
        _repo.Terminer.Add(termin);

        var avstemming = new MvaAvstemming
        {
            Id = Guid.NewGuid(),
            MvaTerminId = termin.Id,
            AvstemmingTidspunkt = DateTime.UtcNow,
            AvstemmingAv = "test",
            ErGodkjent = false,
            Linjer = new List<MvaAvstemmingLinje>
            {
                new() { Id = Guid.NewGuid(), Kontonummer = "2700", Kontonavn = "Utg MVA",
                    SaldoIflgHovedbok = -25_000m, BeregnetFraPosteringer = -25_000m, Avvik = 0m }
            }
        };
        _repo.Avstemminger.Add(avstemming);

        var result = await _service.HentAvstemmingAsync(termin.Id);

        result.Id.Should().Be(avstemming.Id);
        result.ErGodkjent.Should().BeFalse();
        result.Linjer.Should().HaveCount(1);
    }

    [Fact]
    public async Task HentAvstemming_UtenEksisterende_KasterException()
    {
        var termin = LagTermin(2026, 1);
        _repo.Terminer.Add(termin);

        var act = () => _service.HentAvstemmingAsync(termin.Id);

        await act.Should().ThrowAsync<MvaAvstemmingIkkeFunnetException>();
    }

    [Fact]
    public async Task GodkjennAvstemming_SetterGodkjentOgMerknad()
    {
        var termin = LagTermin(2026, 1);
        termin.Status = MvaTerminStatus.Beregnet;
        _repo.Terminer.Add(termin);

        var avstemming = new MvaAvstemming
        {
            Id = Guid.NewGuid(),
            MvaTerminId = termin.Id,
            AvstemmingTidspunkt = DateTime.UtcNow,
            AvstemmingAv = "test",
            ErGodkjent = false,
            Linjer = new()
        };
        _repo.Avstemminger.Add(avstemming);

        var result = await _service.GodkjennAvstemmingAsync(
            termin.Id, avstemming.Id, "Godkjent av regnskapsforer");

        result.ErGodkjent.Should().BeTrue();
        result.Merknad.Should().Be("Godkjent av regnskapsforer");
        termin.Status.Should().Be(MvaTerminStatus.Avstemt);
    }

    [Fact]
    public async Task GodkjennAvstemming_FeilAvstemmingId_KasterException()
    {
        var termin = LagTermin(2026, 1);
        _repo.Terminer.Add(termin);

        var avstemming = new MvaAvstemming
        {
            Id = Guid.NewGuid(),
            MvaTerminId = termin.Id,
            AvstemmingTidspunkt = DateTime.UtcNow,
            AvstemmingAv = "test",
            ErGodkjent = false,
            Linjer = new()
        };
        _repo.Avstemminger.Add(avstemming);

        var act = () => _service.GodkjennAvstemmingAsync(termin.Id, Guid.NewGuid(), null);

        await act.Should().ThrowAsync<MvaAvstemmingIkkeFunnetException>();
    }

    [Fact]
    public async Task HentHistorikk_ReturnererAlleAvstemmingerForTermin()
    {
        var termin = LagTermin(2026, 1);
        _repo.Terminer.Add(termin);

        _repo.Avstemminger.AddRange(new[]
        {
            new MvaAvstemming { Id = Guid.NewGuid(), MvaTerminId = termin.Id, AvstemmingTidspunkt = DateTime.UtcNow.AddDays(-1), AvstemmingAv = "a", Linjer = new() },
            new MvaAvstemming { Id = Guid.NewGuid(), MvaTerminId = termin.Id, AvstemmingTidspunkt = DateTime.UtcNow, AvstemmingAv = "b", Linjer = new() },
        });

        var result = await _service.HentAvstemmingshistorikkAsync(termin.Id);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task KjorAvstemming_UtenKontoSaldoer_ReturnererTomListe()
    {
        var termin = LagTermin(2026, 1);
        _repo.Terminer.Add(termin);

        var result = await _service.KjorAvstemmingAsync(termin.Id);

        result.Linjer.Should().BeEmpty();
        result.HarAvvik.Should().BeFalse();
        result.TotaltAvvik.Should().Be(0m);
    }

    private static MvaTermin LagTermin(int ar, int termin)
    {
        return new MvaTermin
        {
            Id = Guid.NewGuid(),
            Ar = ar,
            Termin = termin,
            Type = MvaTerminType.Tomaaneders,
            FraDato = new DateOnly(ar, (termin - 1) * 2 + 1, 1),
            TilDato = new DateOnly(ar, termin * 2, DateTime.DaysInMonth(ar, termin * 2)),
            Frist = new DateOnly(ar, 4, 10),
            Status = MvaTerminStatus.Apen
        };
    }
}
