using FluentAssertions;
using Regnskap.Application.Features.Hovedbok;
using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Tests.Features.Hovedbok;

public class PeriodeServiceTests
{
    private readonly FakeHovedbokRepository _repo;
    private readonly PeriodeService _sut;

    public PeriodeServiceTests()
    {
        _repo = new FakeHovedbokRepository();
        _sut = new PeriodeService(_repo);
    }

    // --- OpprettPerioderForAr ---

    [Fact]
    public async Task OpprettPerioderForAr_Oppretter14Perioder()
    {
        var result = await _sut.OpprettPerioderForArAsync(2026);

        result.Should().HaveCount(14); // 0-13
        result.First().Periode.Should().Be(0);
        result.Last().Periode.Should().Be(13);
    }

    [Fact]
    public async Task OpprettPerioderForAr_Periode0ErApningsbalanse()
    {
        var result = await _sut.OpprettPerioderForArAsync(2026);

        var p0 = result.First(p => p.Periode == 0);
        p0.Periodenavn.Should().Contain("Apningsbalanse");
        p0.FraDato.Should().Be(new DateOnly(2026, 1, 1));
        p0.TilDato.Should().Be(new DateOnly(2026, 1, 1));
    }

    [Fact]
    public async Task OpprettPerioderForAr_Periode1Til12HarKorrektDatoSpenn()
    {
        var result = await _sut.OpprettPerioderForArAsync(2026);

        var p1 = result.First(p => p.Periode == 1);
        p1.FraDato.Should().Be(new DateOnly(2026, 1, 1));
        p1.TilDato.Should().Be(new DateOnly(2026, 1, 31));

        var p2 = result.First(p => p.Periode == 2);
        p2.FraDato.Should().Be(new DateOnly(2026, 2, 1));
        p2.TilDato.Should().Be(new DateOnly(2026, 2, 28));

        var p12 = result.First(p => p.Periode == 12);
        p12.FraDato.Should().Be(new DateOnly(2026, 12, 1));
        p12.TilDato.Should().Be(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public async Task OpprettPerioderForAr_Periode13ErArsavslutning()
    {
        var result = await _sut.OpprettPerioderForArAsync(2026);

        var p13 = result.First(p => p.Periode == 13);
        p13.Periodenavn.Should().Contain("Arsavslutning");
        p13.FraDato.Should().Be(new DateOnly(2026, 12, 31));
        p13.TilDato.Should().Be(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public async Task OpprettPerioderForAr_AllePerioderErApne()
    {
        var result = await _sut.OpprettPerioderForArAsync(2026);

        result.Should().AllSatisfy(p => p.Status.Should().Be("Apen"));
    }

    [Fact]
    public async Task OpprettPerioderForAr_KasterHvisPerioderAlleredeFinnes()
    {
        await _sut.OpprettPerioderForArAsync(2026);

        var act = () => _sut.OpprettPerioderForArAsync(2026);

        await act.Should().ThrowAsync<PerioderFinnesAlleredeException>();
    }

    [Fact]
    public async Task OpprettPerioderForAr_KasterHvisArUtenforGyldigRekkevidde()
    {
        var act = () => _sut.OpprettPerioderForArAsync(1999);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // --- EndrePeriodeStatus ---

    [Fact]
    public async Task EndrePeriodeStatus_ApenTilSperret_Fungerer()
    {
        await _sut.OpprettPerioderForArAsync(2026);

        var result = await _sut.EndrePeriodeStatusAsync(2026, 1, PeriodeStatus.Sperret, "Under avstemming");

        result.Status.Should().Be("Sperret");
        result.Merknad.Should().Be("Under avstemming");
    }

    [Fact]
    public async Task EndrePeriodeStatus_SperretTilApen_Fungerer()
    {
        await _sut.OpprettPerioderForArAsync(2026);
        await _sut.EndrePeriodeStatusAsync(2026, 1, PeriodeStatus.Sperret);

        var result = await _sut.EndrePeriodeStatusAsync(2026, 1, PeriodeStatus.Apen);

        result.Status.Should().Be("Apen");
    }

    [Fact]
    public async Task EndrePeriodeStatus_ApenTilLukket_Kaster()
    {
        await _sut.OpprettPerioderForArAsync(2026);

        var act = () => _sut.EndrePeriodeStatusAsync(2026, 1, PeriodeStatus.Lukket);

        await act.Should().ThrowAsync<UgyldigStatusOvergangException>();
    }

    [Fact]
    public async Task EndrePeriodeStatus_LukketTilApen_Kaster()
    {
        await _sut.OpprettPerioderForArAsync(2026);
        await _sut.EndrePeriodeStatusAsync(2026, 1, PeriodeStatus.Sperret);
        // Lukking krever at avstemming bestaar - for periode 1 er forrige-periode-sjekk OK
        await _sut.EndrePeriodeStatusAsync(2026, 1, PeriodeStatus.Lukket);

        var act = () => _sut.EndrePeriodeStatusAsync(2026, 1, PeriodeStatus.Apen);

        await act.Should().ThrowAsync<UgyldigStatusOvergangException>();
    }

    [Fact]
    public async Task EndrePeriodeStatus_LukketTilSperret_Kaster()
    {
        await _sut.OpprettPerioderForArAsync(2026);
        await _sut.EndrePeriodeStatusAsync(2026, 1, PeriodeStatus.Sperret);
        await _sut.EndrePeriodeStatusAsync(2026, 1, PeriodeStatus.Lukket);

        var act = () => _sut.EndrePeriodeStatusAsync(2026, 1, PeriodeStatus.Sperret);

        await act.Should().ThrowAsync<UgyldigStatusOvergangException>();
    }

    [Fact]
    public async Task EndrePeriodeStatus_IkkeEksisterendePeriode_Kaster()
    {
        var act = () => _sut.EndrePeriodeStatusAsync(2026, 1, PeriodeStatus.Sperret);

        await act.Should().ThrowAsync<PeriodeIkkeFunnetException>();
    }

    // --- Lukking med forrige-periode-sjekk ---

    [Fact]
    public async Task EndrePeriodeStatus_SperretTilLukket_KreverForrigePeriodeLukket()
    {
        await _sut.OpprettPerioderForArAsync(2026);
        // Sperr periode 2 uten at periode 1 er lukket
        await _sut.EndrePeriodeStatusAsync(2026, 2, PeriodeStatus.Sperret);

        var act = () => _sut.EndrePeriodeStatusAsync(2026, 2, PeriodeStatus.Lukket);

        await act.Should().ThrowAsync<PeriodeLukkingException>();
    }
}
