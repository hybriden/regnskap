using FluentAssertions;
using Regnskap.Application.Features.Mva;
using Regnskap.Domain.Features.Mva;

namespace Regnskap.Tests.Features.Mva;

public class MvaTerminServiceTests
{
    private readonly FakeMvaRepository _repo;
    private readonly MvaTerminService _service;

    public MvaTerminServiceTests()
    {
        _repo = new FakeMvaRepository();
        _service = new MvaTerminService(_repo);
    }

    [Fact]
    public async Task GenererTerminer_Tomaaneders_Lager6Terminer()
    {
        var result = await _service.GenererTerminerAsync(2026, MvaTerminType.Tomaaneders);

        result.Should().HaveCount(6);
        result[0].Termin.Should().Be(1);
        result[5].Termin.Should().Be(6);
    }

    [Fact]
    public async Task GenererTerminer_Arlig_Lager1Termin()
    {
        var result = await _service.GenererTerminerAsync(2026, MvaTerminType.Arlig);

        result.Should().HaveCount(1);
        result[0].Termin.Should().Be(1);
        result[0].Type.Should().Be("Arlig");
    }

    [Fact]
    public async Task GenererTerminer_Termin1_HarKorrekteDatoer()
    {
        var result = await _service.GenererTerminerAsync(2026, MvaTerminType.Tomaaneders);

        var t1 = result.First(t => t.Termin == 1);
        t1.FraDato.Should().Be(new DateOnly(2026, 1, 1));
        t1.TilDato.Should().Be(new DateOnly(2026, 2, 28));
        t1.Frist.Should().Be(new DateOnly(2026, 4, 10));
    }

    [Fact]
    public async Task GenererTerminer_Termin3_HarForlengetFrist()
    {
        var result = await _service.GenererTerminerAsync(2026, MvaTerminType.Tomaaneders);

        var t3 = result.First(t => t.Termin == 3);
        t3.Frist.Should().Be(new DateOnly(2026, 8, 31)); // Forlenget sommerfrist
    }

    [Fact]
    public async Task GenererTerminer_Termin6_HarFristNesteAr()
    {
        var result = await _service.GenererTerminerAsync(2026, MvaTerminType.Tomaaneders);

        var t6 = result.First(t => t.Termin == 6);
        t6.Frist.Should().Be(new DateOnly(2027, 2, 10));
    }

    [Fact]
    public async Task GenererTerminer_Skuddar_HarKorrektFebruardato()
    {
        var result = await _service.GenererTerminerAsync(2024, MvaTerminType.Tomaaneders);

        var t1 = result.First(t => t.Termin == 1);
        t1.TilDato.Should().Be(new DateOnly(2024, 2, 29));
    }

    [Fact]
    public async Task GenererTerminer_FinnesAllerede_KasterException()
    {
        _repo.Terminer.Add(new MvaTermin
        {
            Id = Guid.NewGuid(), Ar = 2026, Termin = 1,
            Type = MvaTerminType.Tomaaneders,
            FraDato = new DateOnly(2026, 1, 1),
            TilDato = new DateOnly(2026, 2, 28),
            Frist = new DateOnly(2026, 4, 10)
        });

        var act = () => _service.GenererTerminerAsync(2026, MvaTerminType.Tomaaneders);

        await act.Should().ThrowAsync<MvaTerminerFinnesException>();
    }

    [Fact]
    public async Task GenererTerminer_UgyldigAr_KasterArgumentException()
    {
        var act = () => _service.GenererTerminerAsync(1999, MvaTerminType.Tomaaneders);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task HentTerminer_ReturnererAlleForAr()
    {
        await _service.GenererTerminerAsync(2026, MvaTerminType.Tomaaneders);

        var result = await _service.HentTerminerAsync(2026);

        result.Should().HaveCount(6);
        result.Should().AllSatisfy(t => t.Status.Should().Be("Apen"));
    }

    [Fact]
    public async Task HentTermin_MedId_ReturnererKorrekt()
    {
        var terminer = await _service.GenererTerminerAsync(2026, MvaTerminType.Tomaaneders);
        var forstId = terminer[0].Id;

        var result = await _service.HentTerminAsync(forstId);

        result.Ar.Should().Be(2026);
        result.Termin.Should().Be(1);
    }

    [Fact]
    public async Task HentTermin_IkkeFunnet_KasterException()
    {
        var act = () => _service.HentTerminAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<MvaTerminIkkeFunnetException>();
    }

    [Fact]
    public async Task GenererTerminer_Arlig_HarFristNesteAr()
    {
        var result = await _service.GenererTerminerAsync(2026, MvaTerminType.Arlig);

        var t = result[0];
        t.FraDato.Should().Be(new DateOnly(2026, 1, 1));
        t.TilDato.Should().Be(new DateOnly(2026, 12, 31));
        t.Frist.Should().Be(new DateOnly(2027, 3, 10));
    }
}
