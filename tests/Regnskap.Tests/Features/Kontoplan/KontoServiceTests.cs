using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Regnskap.Application.Features.Kontoplan;
using Regnskap.Domain.Features.Kontoplan;
using Regnskap.Infrastructure.Features.Kontoplan;
using Regnskap.Infrastructure.Persistence;

namespace Regnskap.Tests.Features.Kontoplan;

public class KontoServiceTests : IDisposable
{
    private readonly RegnskapDbContext _db;
    private readonly KontoplanRepository _repository;
    private readonly KontoService _service;

    public KontoServiceTests()
    {
        var options = new DbContextOptionsBuilder<RegnskapDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new RegnskapDbContext(options);
        _repository = new KontoplanRepository(_db);
        _service = new KontoService(_repository);

        SeedTestData();
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private void SeedTestData()
    {
        var gruppe19 = new Kontogruppe
        {
            Id = Guid.NewGuid(),
            Gruppekode = 19,
            Navn = "Bankinnskudd, kontanter og lignende",
            Kontotype = Kontotype.Eiendel,
            Normalbalanse = Normalbalanse.Debet,
            ErSystemgruppe = true
        };
        var gruppe30 = new Kontogruppe
        {
            Id = Guid.NewGuid(),
            Gruppekode = 30,
            Navn = "Salgsinntekt, avgiftspliktig",
            Kontotype = Kontotype.Inntekt,
            Normalbalanse = Normalbalanse.Kredit,
            ErSystemgruppe = true
        };
        var gruppe24 = new Kontogruppe
        {
            Id = Guid.NewGuid(),
            Gruppekode = 24,
            Navn = "Leverandorgjeld",
            Kontotype = Kontotype.Gjeld,
            Normalbalanse = Normalbalanse.Kredit,
            ErSystemgruppe = true
        };

        _db.Kontogrupper.AddRange(gruppe19, gruppe30, gruppe24);

        _db.Kontoer.Add(new Konto
        {
            Id = Guid.NewGuid(),
            Kontonummer = "1920",
            Navn = "Bankinnskudd",
            Kontotype = Kontotype.Eiendel,
            Normalbalanse = Normalbalanse.Debet,
            KontogruppeId = gruppe19.Id,
            StandardAccountId = "1920",
            ErAktiv = true,
            ErSystemkonto = true,
            ErBokforbar = true
        });

        _db.Kontoer.Add(new Konto
        {
            Id = Guid.NewGuid(),
            Kontonummer = "3000",
            Navn = "Salgsinntekt, avgiftspliktig",
            Kontotype = Kontotype.Inntekt,
            Normalbalanse = Normalbalanse.Kredit,
            KontogruppeId = gruppe30.Id,
            StandardAccountId = "3000",
            ErAktiv = true,
            ErSystemkonto = true,
            ErBokforbar = true,
            StandardMvaKode = "3"
        });

        _db.Kontoer.Add(new Konto
        {
            Id = Guid.NewGuid(),
            Kontonummer = "2400",
            Navn = "Leverandorgjeld",
            Kontotype = Kontotype.Gjeld,
            Normalbalanse = Normalbalanse.Kredit,
            KontogruppeId = gruppe24.Id,
            StandardAccountId = "2400",
            ErAktiv = false,
            ErSystemkonto = false,
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
            ErAktiv = true,
            ErSystemkode = true
        });

        _db.SaveChanges();
    }

    // --- FR-1: Kontonummerformat ---

    [Fact]
    public void ValiderKontonummerFormat_GyldigFireSiffer_KasterIkkeException()
    {
        var act = () => KontoService.ValiderKontonummerFormat("1920");
        act.Should().NotThrow();
    }

    [Fact]
    public void ValiderKontonummerFormat_ForKortNummer_KasterException()
    {
        var act = () => KontoService.ValiderKontonummerFormat("192");
        act.Should().Throw<ArgumentException>().WithMessage("*4-6 siffer*");
    }

    [Fact]
    public void ValiderKontonummerFormat_ForLangtNummer_KasterException()
    {
        var act = () => KontoService.ValiderKontonummerFormat("1234567");
        act.Should().Throw<ArgumentException>().WithMessage("*4-6 siffer*");
    }

    [Fact]
    public void ValiderKontonummerFormat_StarterMed0_KasterException()
    {
        var act = () => KontoService.ValiderKontonummerFormat("0100");
        act.Should().Throw<ArgumentException>().WithMessage("*starte med 1-8*");
    }

    [Fact]
    public void ValiderKontonummerFormat_StarterMed9_KasterException()
    {
        var act = () => KontoService.ValiderKontonummerFormat("9200");
        act.Should().Throw<ArgumentException>().WithMessage("*starte med 1-8*");
    }

    [Fact]
    public void ValiderKontonummerFormat_InneholderBokstaver_KasterException()
    {
        var act = () => KontoService.ValiderKontonummerFormat("19AB");
        act.Should().Throw<ArgumentException>().WithMessage("*kun inneholde siffer*");
    }

    // --- FR-10: Kontotype-til-normalbalanse ---

    [Theory]
    [InlineData(Kontotype.Eiendel, Normalbalanse.Debet)]
    [InlineData(Kontotype.Gjeld, Normalbalanse.Kredit)]
    [InlineData(Kontotype.Egenkapital, Normalbalanse.Kredit)]
    [InlineData(Kontotype.Inntekt, Normalbalanse.Kredit)]
    [InlineData(Kontotype.Kostnad, Normalbalanse.Debet)]
    public void BestemNormalbalanse_GirRiktigBalanse(Kontotype kontotype, Normalbalanse forventet)
    {
        KontoService.BestemNormalbalanse(kontotype).Should().Be(forventet);
    }

    // --- FR-11: Kontoklasse-til-kontotype-konsistens ---

    [Fact]
    public void ValiderKontotypeForKontoklasse_EiendelKlasse1_GirIkkeException()
    {
        var act = () => KontoService.ValiderKontotypeForKontoklasse("1920", Kontotype.Eiendel);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValiderKontotypeForKontoklasse_InntektKlasse1_KasterException()
    {
        var act = () => KontoService.ValiderKontotypeForKontoklasse("1920", Kontotype.Inntekt);
        act.Should().Throw<ArgumentException>().WithMessage("*ikke gyldig for kontoklasse*");
    }

    [Fact]
    public void ValiderKontotypeForKontoklasse_GjeldKlasse2_GirIkkeException()
    {
        var act = () => KontoService.ValiderKontotypeForKontoklasse("2400", Kontotype.Gjeld);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValiderKontotypeForKontoklasse_EgenkapitalKlasse2_GirIkkeException()
    {
        var act = () => KontoService.ValiderKontotypeForKontoklasse("2000", Kontotype.Egenkapital);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValiderKontotypeForKontoklasse_InntektKlasse8_GirIkkeException()
    {
        var act = () => KontoService.ValiderKontotypeForKontoklasse("8000", Kontotype.Inntekt);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValiderKontotypeForKontoklasse_KostnadKlasse8_GirIkkeException()
    {
        var act = () => KontoService.ValiderKontotypeForKontoklasse("8400", Kontotype.Kostnad);
        act.Should().NotThrow();
    }

    // --- FR-5: Systemkontoer kan ikke slettes ---

    [Fact]
    public async Task SlettKonto_Systemkonto_KasterSystemkontoSlettingException()
    {
        var act = async () => await _service.SlettKontoAsync("1920");
        await act.Should().ThrowAsync<SystemkontoSlettingException>();
    }

    // --- FR-9: Deaktiverte kontoer kan ikke bokfores pa ---

    [Fact]
    public async Task HentKontoEllerKast_InaktivKonto_KasterKontoInaktivException()
    {
        var act = async () => await _service.HentKontoEllerKastAsync("2400");
        await act.Should().ThrowAsync<KontoInaktivException>();
    }

    [Fact]
    public async Task HentKontoEllerKast_IkkeEksisterende_KasterKontoIkkeFunnetException()
    {
        var act = async () => await _service.HentKontoEllerKastAsync("9999");
        await act.Should().ThrowAsync<KontoIkkeFunnetException>();
    }

    // --- FR-3: Kontonummer-gruppe-konsistens ---

    [Fact]
    public async Task OpprettKonto_GruppeMismatch_KasterException()
    {
        var request = new OpprettKontoRequest(
            "1950", "Testkonto", null, Kontotype.Eiendel, 30, "1950", null, null, true, null, null, null, false, false);

        var act = async () => await _service.OpprettKontoAsync(request);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*tilhorer ikke gruppe*");
    }

    // --- OpprettKonto happy path ---

    [Fact]
    public async Task OpprettKonto_GyldigData_OppretterKonto()
    {
        var request = new OpprettKontoRequest(
            "1950", "Testkonto", "Test account", Kontotype.Eiendel, 19, "1950", null, null, true, null, null, null, false, false);

        var konto = await _service.OpprettKontoAsync(request);

        konto.Should().NotBeNull();
        konto.Kontonummer.Should().Be("1950");
        konto.Navn.Should().Be("Testkonto");
        konto.Normalbalanse.Should().Be(Normalbalanse.Debet);
        konto.ErSystemkonto.Should().BeFalse();
    }

    // --- Duplikat kontonummer ---

    [Fact]
    public async Task OpprettKonto_DuplikatKontonummer_KasterException()
    {
        var request = new OpprettKontoRequest(
            "1920", "Duplikat", null, Kontotype.Eiendel, 19, "1920", null, null, true, null, null, null, false, false);

        var act = async () => await _service.OpprettKontoAsync(request);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*allerede i bruk*");
    }

    // --- KontoFinnesOgErAktiv ---

    [Fact]
    public async Task KontoFinnesOgErAktiv_AktivKonto_ReturnererTrue()
    {
        var resultat = await _service.KontoFinnesOgErAktivAsync("1920");
        resultat.Should().BeTrue();
    }

    [Fact]
    public async Task KontoFinnesOgErAktiv_InaktivKonto_ReturnererFalse()
    {
        var resultat = await _service.KontoFinnesOgErAktivAsync("2400");
        resultat.Should().BeFalse();
    }

    [Fact]
    public async Task KontoFinnesOgErAktiv_IkkeEksisterende_ReturnererFalse()
    {
        var resultat = await _service.KontoFinnesOgErAktivAsync("9999");
        resultat.Should().BeFalse();
    }

    // --- Deaktiver/Aktiver ---

    [Fact]
    public async Task DeaktiverKonto_AktivKonto_SetterInaktiv()
    {
        await _service.DeaktiverKontoAsync("1920");
        var konto = await _service.HentKontoAsync("1920");
        konto!.ErAktiv.Should().BeFalse();
    }

    [Fact]
    public async Task AktiverKonto_InaktivKonto_SetterAktiv()
    {
        await _service.AktiverKontoAsync("2400");
        var konto = await _service.HentKontoAsync("2400");
        konto!.ErAktiv.Should().BeTrue();
    }

    // --- HentStandardMvaKode ---

    [Fact]
    public async Task HentKonto_MedMvaKode_ReturnererMvaKode()
    {
        var konto = await _service.HentKontoAsync("3000");
        konto.Should().NotBeNull();
        konto!.StandardMvaKode.Should().Be("3");
    }

    // --- FR-14: StandardAccountID ma matche kontoklasse ---

    [Fact]
    public async Task OpprettKonto_StandardAccountIdMismatch_KasterException()
    {
        var request = new OpprettKontoRequest(
            "1950", "Test", null, Kontotype.Eiendel, 19, "3950", null, null, true, null, null, null, false, false);

        var act = async () => await _service.OpprettKontoAsync(request);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*matcher ikke kontoklassen*");
    }

    // --- FR-6: Systemkonto-beskyttelse ved oppdatering ---

    [Fact]
    public async Task OppdaterKonto_SystemkontoEndreKontotype_KasterSystemkontoFeltEndringException()
    {
        var request = new OppdaterKontoRequest(
            "Bankinnskudd oppdatert", null, true, true, null, null, null, null, false, false,
            Kontotype: Kontotype.Kostnad); // Forsok pa a endre kontotype

        var act = async () => await _service.OppdaterKontoAsync("1920", request);
        await act.Should().ThrowAsync<SystemkontoFeltEndringException>()
            .Where(e => e.Felt == "Kontotype");
    }

    [Fact]
    public async Task OppdaterKonto_SystemkontoEndreKontogruppe_KasterSystemkontoFeltEndringException()
    {
        var request = new OppdaterKontoRequest(
            "Bankinnskudd oppdatert", null, true, true, null, null, null, null, false, false,
            Gruppekode: 30); // Forsok pa a endre kontogruppe

        var act = async () => await _service.OppdaterKontoAsync("1920", request);
        await act.Should().ThrowAsync<SystemkontoFeltEndringException>()
            .Where(e => e.Felt == "Kontogruppe");
    }

    [Fact]
    public async Task OppdaterKonto_SystemkontoFjerneSystemflagg_KasterSystemkontoFeltEndringException()
    {
        var request = new OppdaterKontoRequest(
            "Bankinnskudd oppdatert", null, true, true, null, null, null, null, false, false,
            ErSystemkonto: false); // Forsok pa a fjerne systemkonto-flagg

        var act = async () => await _service.OppdaterKontoAsync("1920", request);
        await act.Should().ThrowAsync<SystemkontoFeltEndringException>()
            .Where(e => e.Felt == "ErSystemkonto");
    }

    [Fact]
    public async Task OppdaterKonto_SystemkontoEndreNavn_TillatesOk()
    {
        // Navn er ikke et beskyttet felt, sa dette skal ga gjennom
        var request = new OppdaterKontoRequest(
            "Bankinnskudd - oppdatert navn", null, true, true, null, null, null, null, false, false);

        var konto = await _service.OppdaterKontoAsync("1920", request);
        konto.Navn.Should().Be("Bankinnskudd - oppdatert navn");
    }

    [Fact]
    public async Task OppdaterKonto_IkkeSystemkonto_TillatesOk()
    {
        var request = new OppdaterKontoRequest(
            "Leverandorgjeld oppdatert", "Accounts payable updated", true, true, null, "Ny beskrivelse", null, null, false, false);

        var konto = await _service.OppdaterKontoAsync("2400", request);
        konto.Navn.Should().Be("Leverandorgjeld oppdatert");
        konto.Beskrivelse.Should().Be("Ny beskrivelse");
    }

    // --- Avledede egenskaper pa Konto ---

    [Fact]
    public void Konto_Kontoklasse_AvledesFraForsteSiffer()
    {
        var konto = new Konto { Kontonummer = "1920" };
        konto.Kontoklasse.Should().Be(Kontoklasse.Eiendeler);
    }

    [Fact]
    public void Konto_ErBalansekonto_TrueForKlasse1()
    {
        var konto = new Konto { Kontonummer = "1920" };
        konto.ErBalansekonto.Should().BeTrue();
    }

    [Fact]
    public void Konto_ErBalansekonto_FalseForKlasse3()
    {
        var konto = new Konto { Kontonummer = "3000" };
        konto.ErBalansekonto.Should().BeFalse();
    }

    [Fact]
    public void Konto_ErUnderkonto_TrueForFemSiffer()
    {
        var konto = new Konto { Kontonummer = "19201" };
        konto.ErUnderkonto.Should().BeTrue();
    }

    [Fact]
    public void Konto_ErUnderkonto_FalseForFireSiffer()
    {
        var konto = new Konto { Kontonummer = "1920" };
        konto.ErUnderkonto.Should().BeFalse();
    }
}
