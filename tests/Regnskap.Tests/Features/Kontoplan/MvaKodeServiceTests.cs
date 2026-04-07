using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Regnskap.Application.Features.Kontoplan;
using Regnskap.Domain.Features.Kontoplan;
using Regnskap.Infrastructure.Features.Kontoplan;
using Regnskap.Infrastructure.Persistence;

namespace Regnskap.Tests.Features.Kontoplan;

public class MvaKodeServiceTests : IDisposable
{
    private readonly RegnskapDbContext _db;
    private readonly KontoplanRepository _repository;
    private readonly MvaKodeService _service;
    private readonly Guid _utgaendeKontoId;
    private readonly Guid _inngaendeKontoId;

    public MvaKodeServiceTests()
    {
        var options = new DbContextOptionsBuilder<RegnskapDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new RegnskapDbContext(options);
        _repository = new KontoplanRepository(_db);
        _service = new MvaKodeService(_repository);

        _utgaendeKontoId = Guid.NewGuid();
        _inngaendeKontoId = Guid.NewGuid();
        SeedTestData();
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private void SeedTestData()
    {
        var gruppe27 = new Kontogruppe
        {
            Id = Guid.NewGuid(),
            Gruppekode = 27,
            Navn = "Skattetrekk og offentlige avgifter",
            Kontotype = Kontotype.Gjeld,
            Normalbalanse = Normalbalanse.Kredit,
            ErSystemgruppe = true
        };

        _db.Kontogrupper.Add(gruppe27);

        _db.Kontoer.Add(new Konto
        {
            Id = _utgaendeKontoId,
            Kontonummer = "2700",
            Navn = "Utgaende MVA",
            Kontotype = Kontotype.Gjeld,
            Normalbalanse = Normalbalanse.Kredit,
            KontogruppeId = gruppe27.Id,
            StandardAccountId = "2700",
            ErAktiv = true,
            ErSystemkonto = true,
            ErBokforbar = true
        });

        _db.Kontoer.Add(new Konto
        {
            Id = _inngaendeKontoId,
            Kontonummer = "2710",
            Navn = "Inngaende MVA",
            Kontotype = Kontotype.Gjeld,
            Normalbalanse = Normalbalanse.Kredit,
            KontogruppeId = gruppe27.Id,
            StandardAccountId = "2710",
            ErAktiv = true,
            ErSystemkonto = true,
            ErBokforbar = true
        });

        _db.MvaKoder.Add(new MvaKode
        {
            Id = Guid.NewGuid(),
            Kode = "3",
            Beskrivelse = "Utgaende MVA 25%",
            StandardTaxCode = "3",
            Sats = 25m,
            Retning = MvaRetning.Utgaende,
            UtgaendeKontoId = _utgaendeKontoId,
            ErAktiv = true,
            ErSystemkode = true
        });

        _db.SaveChanges();
    }

    // --- FR-17: MVA-kontokoblinger ---

    [Fact]
    public async Task OpprettMvaKode_UtgaendeMedSatsUtenKonto_KasterException()
    {
        var request = new OpprettMvaKodeRequest(
            "99", "Test utgaende", null, "99", 25m, MvaRetning.Utgaende, null, null);

        var act = async () => await _service.OpprettMvaKodeAsync(request);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Utgaende*UtgaendeKontoId*");
    }

    [Fact]
    public async Task OpprettMvaKode_InngaendeMedSatsUtenKonto_KasterException()
    {
        var request = new OpprettMvaKodeRequest(
            "98", "Test inngaende", null, "98", 25m, MvaRetning.Inngaende, null, null);

        var act = async () => await _service.OpprettMvaKodeAsync(request);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Inngaende*InngaendeKontoId*");
    }

    [Fact]
    public async Task OpprettMvaKode_UtgaendeNullsatsUtenKonto_TillatesOk()
    {
        var request = new OpprettMvaKodeRequest(
            "5", "Eksport 0%", null, "5", 0m, MvaRetning.Utgaende, null, null);

        var mvaKode = await _service.OpprettMvaKodeAsync(request);
        mvaKode.Should().NotBeNull();
        mvaKode.Kode.Should().Be("5");
        mvaKode.UtgaendeKontoId.Should().BeNull();
    }

    [Fact]
    public async Task OpprettMvaKode_InngaendeNullsatsUtenKonto_TillatesOk()
    {
        var request = new OpprettMvaKodeRequest(
            "6", "Fritatt 0%", null, "6", 0m, MvaRetning.Inngaende, null, null);

        var mvaKode = await _service.OpprettMvaKodeAsync(request);
        mvaKode.Should().NotBeNull();
        mvaKode.Kode.Should().Be("6");
        mvaKode.InngaendeKontoId.Should().BeNull();
    }

    [Fact]
    public async Task OpprettMvaKode_UtgaendeMedKonto_TillatesOk()
    {
        var request = new OpprettMvaKodeRequest(
            "1", "Utgaende MVA hoy sats", null, "1", 25m, MvaRetning.Utgaende, "2700", null);

        var mvaKode = await _service.OpprettMvaKodeAsync(request);
        mvaKode.Should().NotBeNull();
        mvaKode.UtgaendeKontoId.Should().Be(_utgaendeKontoId);
    }

    [Fact]
    public async Task OpprettMvaKode_SnuddAvregningUtenBeggeKontoer_KasterException()
    {
        var request = new OpprettMvaKodeRequest(
            "97", "Reverse charge", null, "97", 25m, MvaRetning.SnuddAvregning, "2700", null);

        var act = async () => await _service.OpprettMvaKodeAsync(request);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Reverse charge*bade*");
    }

    [Fact]
    public async Task OpprettMvaKode_SnuddAvregningMedBeggeKontoer_TillatesOk()
    {
        var request = new OpprettMvaKodeRequest(
            "96", "Reverse charge OK", null, "96", 25m, MvaRetning.SnuddAvregning, "2700", "2710");

        var mvaKode = await _service.OpprettMvaKodeAsync(request);
        mvaKode.Should().NotBeNull();
        mvaKode.UtgaendeKontoId.Should().Be(_utgaendeKontoId);
        mvaKode.InngaendeKontoId.Should().Be(_inngaendeKontoId);
    }

    [Fact]
    public async Task OpprettMvaKode_DuplikatKode_KasterException()
    {
        var request = new OpprettMvaKodeRequest(
            "3", "Duplikat", null, "3", 25m, MvaRetning.Utgaende, "2700", null);

        var act = async () => await _service.OpprettMvaKodeAsync(request);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*allerede i bruk*");
    }

    [Fact]
    public async Task OpprettMvaKode_UkjentKonto_KasterKontoIkkeFunnetException()
    {
        var request = new OpprettMvaKodeRequest(
            "95", "Test", null, "95", 25m, MvaRetning.Utgaende, "9999", null);

        var act = async () => await _service.OpprettMvaKodeAsync(request);
        await act.Should().ThrowAsync<KontoIkkeFunnetException>();
    }

    // --- Hent ---

    [Fact]
    public async Task HentMvaKode_Eksisterende_ReturnererKode()
    {
        var mvaKode = await _service.HentMvaKodeAsync("3");
        mvaKode.Should().NotBeNull();
        mvaKode!.Beskrivelse.Should().Be("Utgaende MVA 25%");
    }

    [Fact]
    public async Task HentMvaKode_IkkeEksisterende_ReturnererNull()
    {
        var mvaKode = await _service.HentMvaKodeAsync("999");
        mvaKode.Should().BeNull();
    }

    [Fact]
    public async Task HentMvaKodeEllerKast_IkkeEksisterende_KasterException()
    {
        var act = async () => await _service.HentMvaKodeEllerKastAsync("999");
        await act.Should().ThrowAsync<MvaKodeIkkeFunnetException>();
    }
}
