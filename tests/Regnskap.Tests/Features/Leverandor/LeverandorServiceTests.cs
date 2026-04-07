using FluentAssertions;
using Regnskap.Application.Features.Leverandorreskontro;
using Regnskap.Domain.Features.Leverandorreskontro;

namespace Regnskap.Tests.Features.Leverandor;

public class LeverandorServiceTests
{
    private readonly FakeLeverandorRepository _repo;
    private readonly LeverandorService _service;

    public LeverandorServiceTests()
    {
        _repo = new FakeLeverandorRepository();
        _service = new LeverandorService(_repo);
    }

    private static OpprettLeverandorRequest LagRequest(
        string nummer = "10001", string navn = "Test AS", string? orgNr = "123456789")
        => new(nummer, navn, orgNr, true, "Gate 1", null, "0150", "Oslo", "NO",
            "Ola", "12345678", "test@test.no", Betalingsbetingelse.Netto30, null,
            "12345678901", null, null, null, null);

    [Fact]
    public async Task Opprett_MedGyldigeData_ReturnererLeverandorDto()
    {
        var result = await _service.OpprettAsync(LagRequest());

        result.Leverandornummer.Should().Be("10001");
        result.Navn.Should().Be("Test AS");
        result.ErAktiv.Should().BeTrue();
        _repo.Leverandorer.Should().HaveCount(1);
    }

    [Fact]
    public async Task Opprett_DuplikatLeverandornummer_KasterException()
    {
        await _service.OpprettAsync(LagRequest("10001"));

        var act = () => _service.OpprettAsync(LagRequest("10001", "Annen AS", "987654321"));
        await act.Should().ThrowAsync<LeverandorDuplikatException>()
            .WithMessage("*leverandornummer*");
    }

    [Fact]
    public async Task Opprett_DuplikatOrganisasjonsnummer_KasterException()
    {
        await _service.OpprettAsync(LagRequest("10001"));

        var act = () => _service.OpprettAsync(LagRequest("10002", "Annen AS", "123456789"));
        await act.Should().ThrowAsync<LeverandorDuplikatException>()
            .WithMessage("*organisasjonsnummer*");
    }

    [Fact]
    public async Task Opprett_EgendefinertBetingelseManglerFrist_KasterArgumentException()
    {
        var request = new OpprettLeverandorRequest(
            "10001", "Test AS", null, false, null, null, null, null, "NO",
            null, null, null, Betalingsbetingelse.Egendefinert, null,
            null, null, null, null, null);

        var act = () => _service.OpprettAsync(request);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*EgendefinertBetalingsfrist*");
    }

    [Fact]
    public async Task Oppdater_EksisterendeLeverandor_OppdatererFelter()
    {
        var opprettet = await _service.OpprettAsync(LagRequest());

        var oppdaterRequest = new OppdaterLeverandorRequest(
            "Oppdatert AS", "123456789", true, "Ny gate", null, "0151", "Bergen", "NO",
            "Kari", "87654321", "ny@test.no", Betalingsbetingelse.Netto45, null,
            "12345678901", null, null, null, null, true, false, "Notat");

        var result = await _service.OppdaterAsync(opprettet.Id, oppdaterRequest);

        result.Navn.Should().Be("Oppdatert AS");
        result.Betalingsbetingelse.Should().Be(Betalingsbetingelse.Netto45);
    }

    [Fact]
    public async Task Oppdater_IkkeEksisterende_KasterException()
    {
        var oppdaterRequest = new OppdaterLeverandorRequest(
            "Test", null, false, null, null, null, null, "NO",
            null, null, null, Betalingsbetingelse.Netto30, null,
            null, null, null, null, null, true, false, null);

        var act = () => _service.OppdaterAsync(Guid.NewGuid(), oppdaterRequest);
        await act.Should().ThrowAsync<LeverandorIkkeFunnetException>();
    }

    [Fact]
    public async Task Slett_EksisterendeLeverandor_SetterIsDeleted()
    {
        var opprettet = await _service.OpprettAsync(LagRequest());

        await _service.SlettAsync(opprettet.Id);

        _repo.Leverandorer.First().IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Hent_EksisterendeLeverandor_ReturnererDto()
    {
        var opprettet = await _service.OpprettAsync(LagRequest());

        var result = await _service.HentAsync(opprettet.Id);

        result.Id.Should().Be(opprettet.Id);
        result.Leverandornummer.Should().Be("10001");
    }
}
