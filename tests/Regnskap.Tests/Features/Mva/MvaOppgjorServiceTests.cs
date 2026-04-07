using FluentAssertions;
using Regnskap.Application.Features.Bilagsregistrering;
using Regnskap.Application.Features.Mva;
using Regnskap.Domain.Features.Kontoplan;
using Regnskap.Domain.Features.Mva;

namespace Regnskap.Tests.Features.Mva;

public class MvaOppgjorServiceTests
{
    private readonly FakeMvaRepository _repo;
    private readonly FakeKontoplanRepository _kontoplanRepo;
    private readonly FakeBilagRegistreringService _bilagService;
    private readonly MvaOppgjorService _service;

    public MvaOppgjorServiceTests()
    {
        _repo = new FakeMvaRepository();
        _kontoplanRepo = new FakeKontoplanRepository();
        _bilagService = new FakeBilagRegistreringService();
        _service = new MvaOppgjorService(_repo, _kontoplanRepo, _bilagService);
    }

    [Fact]
    public async Task BeregnOppgjor_MedUtgaendeOgInngaendeMva_BeregnerKorrektTilBetaling()
    {
        // Arrange: Termin 1, jan-feb 2026
        var termin = LagTermin(2026, 1, MvaTerminStatus.Apen);
        _repo.Terminer.Add(termin);

        // Salg 100.000 @ 25% = 25.000 utg MVA
        // Kjop 40.000 @ 25% = 10.000 inng MVA
        _repo.Aggregeringer.AddRange(new[]
        {
            new MvaAggregeringDto("3", "3", 25m, MvaRetning.Utgaende, 100_000m, 25_000m, 5),
            new MvaAggregeringDto("1", "1", 25m, MvaRetning.Inngaende, 40_000m, 10_000m, 3),
        });

        _kontoplanRepo.MvaKoder.AddRange(LagStandardMvaKoder());

        // Act
        var result = await _service.BeregnOppgjorAsync(termin.Id);

        // Assert
        result.SumUtgaendeMva.Should().Be(25_000m);
        result.SumInngaendeMva.Should().Be(10_000m);
        result.MvaTilBetaling.Should().Be(15_000m);
        result.Linjer.Should().HaveCount(2);
    }

    [Fact]
    public async Task BeregnOppgjor_MedFlereUtgaendeSatser_SummererKorrekt()
    {
        // Arrange
        var termin = LagTermin(2026, 1, MvaTerminStatus.Apen);
        _repo.Terminer.Add(termin);

        // 25% salg + 15% salg + 12% salg
        _repo.Aggregeringer.AddRange(new[]
        {
            new MvaAggregeringDto("3", "3", 25m, MvaRetning.Utgaende, 100_000m, 25_000m, 3),
            new MvaAggregeringDto("31", "31", 15m, MvaRetning.Utgaende, 50_000m, 7_500m, 2),
            new MvaAggregeringDto("33", "33", 12m, MvaRetning.Utgaende, 30_000m, 3_600m, 1),
        });

        _kontoplanRepo.MvaKoder.AddRange(LagStandardMvaKoder());

        // Act
        var result = await _service.BeregnOppgjorAsync(termin.Id);

        // Assert
        result.SumUtgaendeMva.Should().Be(25_000m + 7_500m + 3_600m);
        result.MvaTilBetaling.Should().Be(36_100m);
    }

    [Fact]
    public async Task BeregnOppgjor_MedSnuddAvregning_ErResultatnoytralt()
    {
        // Arrange
        var termin = LagTermin(2026, 1, MvaTerminStatus.Apen);
        _repo.Terminer.Add(termin);

        // Snudd avregning: utg. del (kode 82) og inng. del (kode 81)
        _repo.Aggregeringer.AddRange(new[]
        {
            new MvaAggregeringDto("82", "82", 25m, MvaRetning.SnuddAvregning, 20_000m, 5_000m, 1),
            new MvaAggregeringDto("81", "81", 25m, MvaRetning.SnuddAvregning, 20_000m, 5_000m, 1),
        });

        _kontoplanRepo.MvaKoder.AddRange(LagStandardMvaKoder());

        // Act
        var result = await _service.BeregnOppgjorAsync(termin.Id);

        // Assert
        result.SumSnuddAvregningUtgaende.Should().Be(5_000m);
        result.SumSnuddAvregningInngaende.Should().Be(5_000m);
        result.MvaTilBetaling.Should().Be(0m); // Noytralt
    }

    [Fact]
    public async Task BeregnOppgjor_FulltEksempelFraSpec_GirKorrektResultat()
    {
        // Arrange: FR-MVA-02 eksempel fra spesifikasjonen
        var termin = LagTermin(2026, 1, MvaTerminStatus.Apen);
        _repo.Terminer.Add(termin);

        _repo.Aggregeringer.AddRange(new[]
        {
            new MvaAggregeringDto("3", "3", 25m, MvaRetning.Utgaende, 100_000m, 25_000m, 5),
            new MvaAggregeringDto("31", "31", 15m, MvaRetning.Utgaende, 50_000m, 7_500m, 3),
            new MvaAggregeringDto("1", "1", 25m, MvaRetning.Inngaende, 40_000m, 10_000m, 4),
            new MvaAggregeringDto("82", "82", 25m, MvaRetning.SnuddAvregning, 20_000m, 5_000m, 1),
            new MvaAggregeringDto("81", "81", 25m, MvaRetning.SnuddAvregning, 20_000m, 5_000m, 1),
        });

        _kontoplanRepo.MvaKoder.AddRange(LagStandardMvaKoder());

        // Act
        var result = await _service.BeregnOppgjorAsync(termin.Id);

        // Assert: fra spec
        result.SumUtgaendeMva.Should().Be(32_500m);
        result.SumInngaendeMva.Should().Be(10_000m);
        result.SumSnuddAvregningUtgaende.Should().Be(5_000m);
        result.SumSnuddAvregningInngaende.Should().Be(5_000m);
        result.MvaTilBetaling.Should().Be(22_500m);
    }

    [Fact]
    public async Task BeregnOppgjor_UtenPosteringer_GirNullmelding()
    {
        // Arrange
        var termin = LagTermin(2026, 1, MvaTerminStatus.Apen);
        _repo.Terminer.Add(termin);
        _kontoplanRepo.MvaKoder.AddRange(LagStandardMvaKoder());

        // Act
        var result = await _service.BeregnOppgjorAsync(termin.Id);

        // Assert
        result.SumUtgaendeMva.Should().Be(0m);
        result.SumInngaendeMva.Should().Be(0m);
        result.MvaTilBetaling.Should().Be(0m);
        result.Linjer.Should().BeEmpty();
    }

    [Fact]
    public async Task BeregnOppgjor_TerminIkkeApen_KasterException()
    {
        var termin = LagTermin(2026, 1, MvaTerminStatus.Innsendt);
        _repo.Terminer.Add(termin);

        var act = () => _service.BeregnOppgjorAsync(termin.Id);

        await act.Should().ThrowAsync<MvaTerminIkkeApenException>();
    }

    [Fact]
    public async Task BeregnOppgjor_TerminIkkeFunnet_KasterException()
    {
        var act = () => _service.BeregnOppgjorAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<MvaTerminIkkeFunnetException>();
    }

    [Fact]
    public async Task BeregnOppgjor_LastOppgjor_KasterException()
    {
        var termin = LagTermin(2026, 1, MvaTerminStatus.Beregnet);
        _repo.Terminer.Add(termin);
        _repo.Oppgjor.Add(new MvaOppgjor
        {
            Id = Guid.NewGuid(),
            MvaTerminId = termin.Id,
            ErLast = true,
            BeregnetAv = "test",
            Linjer = new()
        });

        var act = () => _service.BeregnOppgjorAsync(termin.Id);

        await act.Should().ThrowAsync<MvaOppgjorAlleredeLastException>();
    }

    [Fact]
    public async Task BeregnOppgjor_KanReberegne_NarIkkeLastOgStatusErBeregnet()
    {
        var termin = LagTermin(2026, 1, MvaTerminStatus.Beregnet);
        _repo.Terminer.Add(termin);

        var gammeltOppgjor = new MvaOppgjor
        {
            Id = Guid.NewGuid(),
            MvaTerminId = termin.Id,
            ErLast = false,
            BeregnetAv = "test",
            Linjer = new()
        };
        _repo.Oppgjor.Add(gammeltOppgjor);

        _repo.Aggregeringer.Add(
            new MvaAggregeringDto("3", "3", 25m, MvaRetning.Utgaende, 50_000m, 12_500m, 2));
        _kontoplanRepo.MvaKoder.AddRange(LagStandardMvaKoder());

        // Act
        var result = await _service.BeregnOppgjorAsync(termin.Id);

        // Assert
        result.SumUtgaendeMva.Should().Be(12_500m);
        gammeltOppgjor.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task BeregnOppgjor_MedNegativtResultat_GirTilgode()
    {
        // Mer fradrag enn utgaende MVA
        var termin = LagTermin(2026, 1, MvaTerminStatus.Apen);
        _repo.Terminer.Add(termin);

        _repo.Aggregeringer.AddRange(new[]
        {
            new MvaAggregeringDto("3", "3", 25m, MvaRetning.Utgaende, 10_000m, 2_500m, 1),
            new MvaAggregeringDto("1", "1", 25m, MvaRetning.Inngaende, 80_000m, 20_000m, 5),
        });
        _kontoplanRepo.MvaKoder.AddRange(LagStandardMvaKoder());

        var result = await _service.BeregnOppgjorAsync(termin.Id);

        result.MvaTilBetaling.Should().Be(-17_500m); // Tilgode
    }

    [Fact]
    public async Task HentOppgjor_NarOppgjorFinnes_ReturnererKorrektDto()
    {
        var termin = LagTermin(2026, 1, MvaTerminStatus.Beregnet);
        _repo.Terminer.Add(termin);

        var oppgjor = new MvaOppgjor
        {
            Id = Guid.NewGuid(),
            MvaTerminId = termin.Id,
            BeregnetTidspunkt = DateTime.UtcNow,
            BeregnetAv = "bruker",
            SumUtgaendeMva = 25_000m,
            SumInngaendeMva = 10_000m,
            MvaTilBetaling = 15_000m,
            Linjer = new List<MvaOppgjorLinje>
            {
                new() { Id = Guid.NewGuid(), MvaKode = "3", StandardTaxCode = "3", Sats = 25m,
                    Retning = MvaRetning.Utgaende, RfPostnummer = 1, SumGrunnlag = 100_000m, SumMvaBelop = 25_000m, AntallPosteringer = 5 }
            }
        };
        _repo.Oppgjor.Add(oppgjor);

        var result = await _service.HentOppgjorAsync(termin.Id);

        result.MvaTilBetaling.Should().Be(15_000m);
        result.Linjer.Should().HaveCount(1);
    }

    [Fact]
    public async Task HentOppgjor_UtenOppgjor_KasterException()
    {
        var termin = LagTermin(2026, 1, MvaTerminStatus.Apen);
        _repo.Terminer.Add(termin);

        var act = () => _service.HentOppgjorAsync(termin.Id);

        await act.Should().ThrowAsync<MvaOppgjorManglerException>();
    }

    // --- RF-0002 postnummer-tester ---

    [Theory]
    [InlineData("3", 1)]
    [InlineData("31", 2)]
    [InlineData("33", 3)]
    [InlineData("51", 4)]
    [InlineData("52", 5)]
    [InlineData("82", 6)]
    [InlineData("1", 7)]
    [InlineData("11", 8)]
    [InlineData("13", 9)]
    [InlineData("14", 10)]
    [InlineData("15", 11)]
    [InlineData("81", 12)]
    public void TilordneRfPostnummer_MapperKorrekt(string taxCode, int expected)
    {
        MvaOppgjorService.TilordneRfPostnummer(taxCode).Should().Be(expected);
    }

    [Theory]
    [InlineData("82", true)]
    [InlineData("87", true)]
    [InlineData("92", true)]
    [InlineData("81", false)]
    [InlineData("3", false)]
    public void IsSnuddAvregningUtgaende_IdentifisererKorrekt(string taxCode, bool expected)
    {
        MvaOppgjorService.IsSnuddAvregningUtgaende(taxCode).Should().Be(expected);
    }

    [Theory]
    [InlineData("81", true)]
    [InlineData("86", true)]
    [InlineData("91", true)]
    [InlineData("82", false)]
    [InlineData("1", false)]
    public void IsSnuddAvregningInngaende_IdentifisererKorrekt(string taxCode, bool expected)
    {
        MvaOppgjorService.IsSnuddAvregningInngaende(taxCode).Should().Be(expected);
    }

    // --- Helpers ---

    private static MvaTermin LagTermin(int ar, int termin, MvaTerminStatus status)
    {
        return new MvaTermin
        {
            Id = Guid.NewGuid(),
            Ar = ar,
            Termin = termin,
            Type = MvaTerminType.Tomaaneders,
            FraDato = new DateOnly(ar, (termin - 1) * 2 + 1, 1),
            TilDato = new DateOnly(ar, termin * 2, DateTime.DaysInMonth(ar, termin * 2)),
            Frist = new DateOnly(ar, termin * 2 + 2 > 12 ? 2 : termin * 2 + 2, 10),
            Status = status
        };
    }

    private static List<MvaKode> LagStandardMvaKoder()
    {
        return new List<MvaKode>
        {
            new() { Id = Guid.NewGuid(), Kode = "1", StandardTaxCode = "1", Sats = 25m, Retning = MvaRetning.Inngaende, ErAktiv = true },
            new() { Id = Guid.NewGuid(), Kode = "3", StandardTaxCode = "3", Sats = 25m, Retning = MvaRetning.Utgaende, ErAktiv = true },
            new() { Id = Guid.NewGuid(), Kode = "11", StandardTaxCode = "11", Sats = 15m, Retning = MvaRetning.Inngaende, ErAktiv = true },
            new() { Id = Guid.NewGuid(), Kode = "13", StandardTaxCode = "13", Sats = 12m, Retning = MvaRetning.Inngaende, ErAktiv = true },
            new() { Id = Guid.NewGuid(), Kode = "31", StandardTaxCode = "31", Sats = 15m, Retning = MvaRetning.Utgaende, ErAktiv = true },
            new() { Id = Guid.NewGuid(), Kode = "33", StandardTaxCode = "33", Sats = 12m, Retning = MvaRetning.Utgaende, ErAktiv = true },
            new() { Id = Guid.NewGuid(), Kode = "81", StandardTaxCode = "81", Sats = 25m, Retning = MvaRetning.SnuddAvregning, ErAktiv = true },
            new() { Id = Guid.NewGuid(), Kode = "82", StandardTaxCode = "82", Sats = 25m, Retning = MvaRetning.SnuddAvregning, ErAktiv = true },
        };
    }
}
