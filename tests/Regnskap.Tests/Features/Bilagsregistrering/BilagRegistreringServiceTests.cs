using FluentAssertions;
using Regnskap.Application.Features.Bilagsregistrering;
using Regnskap.Application.Features.Kontoplan;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kontoplan;
using Regnskap.Tests.Features.Hovedbok;

namespace Regnskap.Tests.Features.Bilagsregistrering;

/// <summary>
/// Minimal fake for IMvaKodeService i tester.
/// </summary>
public class FakeMvaKodeService : IMvaKodeService
{
    private readonly Dictionary<string, MvaKode> _koder = new();

    public void LeggTilMvaKode(MvaKode kode) => _koder[kode.Kode] = kode;

    public Task<MvaKode?> HentMvaKodeAsync(string kode, CancellationToken ct = default)
        => Task.FromResult(_koder.GetValueOrDefault(kode));

    public Task<MvaKode> HentMvaKodeEllerKastAsync(string kode, CancellationToken ct = default)
        => _koder.TryGetValue(kode, out var k)
            ? Task.FromResult(k)
            : throw new MvaKodeIkkeFunnetException(kode);

    public Task<IReadOnlyList<MvaKode>> HentAlleMvaKoderAsync(bool? erAktiv = null, MvaRetning? retning = null, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<MvaKode>>(_koder.Values.ToList());

    public Task<string?> HentStandardMvaKodeForKontoAsync(string kontonummer, CancellationToken ct = default)
        => Task.FromResult<string?>(null);

    public Task<MvaKode> OpprettMvaKodeAsync(OpprettMvaKodeRequest request, CancellationToken ct = default) => throw new NotImplementedException();
    public Task<MvaKode> OppdaterMvaKodeAsync(string kode, OppdaterMvaKodeRequest request, CancellationToken ct = default) => throw new NotImplementedException();
}

/// <summary>
/// Minimal fake for IKontoplanRepository i tester.
/// </summary>
public class FakeKontoplanRepository : IKontoplanRepository
{
    private readonly Dictionary<string, Konto> _kontoer = new();
    private readonly Dictionary<string, MvaKode> _mvaKoder = new();

    public void LeggTilKonto(Konto konto) => _kontoer[konto.Kontonummer] = konto;
    public void LeggTilMvaKode(MvaKode kode) => _mvaKoder[kode.Kode] = kode;

    public Task<Konto?> HentKontoAsync(string kontonummer, CancellationToken ct = default)
        => Task.FromResult(_kontoer.GetValueOrDefault(kontonummer));

    public Task<Konto?> HentKontoMedDetaljerAsync(string kontonummer, CancellationToken ct = default)
        => Task.FromResult(_kontoer.GetValueOrDefault(kontonummer));

    public Task<bool> KontoFinnesAsync(string kontonummer, CancellationToken ct = default)
        => Task.FromResult(_kontoer.ContainsKey(kontonummer));

    public Task<List<Konto>> HentKontoerAsync(int? kontoklasse = null, Kontotype? kontotype = null, int? gruppekode = null, bool? erAktiv = null, bool? erBokforbar = null, string? sok = null, int side = 1, int antall = 50, CancellationToken ct = default)
        => Task.FromResult(_kontoer.Values.ToList());

    public Task<int> TellKontoerAsync(int? kontoklasse = null, Kontotype? kontotype = null, int? gruppekode = null, bool? erAktiv = null, bool? erBokforbar = null, string? sok = null, CancellationToken ct = default)
        => Task.FromResult(_kontoer.Count);

    public Task<List<Konto>> SokKontoerAsync(string query, int antall = 10, CancellationToken ct = default)
        => Task.FromResult(new List<Konto>());

    public Task LeggTilKontoAsync(Konto konto, CancellationToken ct = default)
    {
        _kontoer[konto.Kontonummer] = konto;
        return Task.CompletedTask;
    }

    public Task<bool> KontoHarPosteringerAsync(string kontonummer, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<bool> KontoHarAktiveUnderkontoerAsync(string kontonummer, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<List<Kontogruppe>> HentAlleKontogrupperAsync(CancellationToken ct = default)
        => Task.FromResult(new List<Kontogruppe>());

    public Task<Kontogruppe?> HentKontogruppeAsync(int gruppekode, CancellationToken ct = default)
        => Task.FromResult<Kontogruppe?>(null);

    public Task<List<MvaKode>> HentAlleMvaKoderAsync(bool? erAktiv = null, MvaRetning? retning = null, CancellationToken ct = default)
        => Task.FromResult(_mvaKoder.Values.ToList());

    public Task<MvaKode?> HentMvaKodeAsync(string kode, CancellationToken ct = default)
        => Task.FromResult(_mvaKoder.GetValueOrDefault(kode));

    public Task<bool> MvaKodeFinnesAsync(string kode, CancellationToken ct = default)
        => Task.FromResult(_mvaKoder.ContainsKey(kode));

    public Task LeggTilMvaKodeAsync(MvaKode mvaKode, CancellationToken ct = default)
    {
        _mvaKoder[mvaKode.Kode] = mvaKode;
        return Task.CompletedTask;
    }

    public Task LagreEndringerAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}

public class BilagRegistreringServiceTests
{
    private readonly FakeHovedbokRepository _hovedbokRepo;
    private readonly FakeBilagRepository _bilagRepo;
    private readonly FakeKontoplanRepository _kontoplanRepo;
    private readonly FakeMvaKodeService _mvaKodeService;
    private readonly FakeTransactionManager _transactionManager;
    private readonly BilagRegistreringService _service;

    // Standard test kontoer
    private static readonly Guid KontoGruppeId = Guid.NewGuid();
    private static readonly Konto Konto6800 = LagKonto("6800", "Kontorkostnad", Kontotype.Kostnad);
    private static readonly Konto Konto1920 = LagKonto("1920", "Bankkonto", Kontotype.Eiendel);
    private static readonly Konto Konto2710 = LagKonto("2710", "Inngaende MVA", Kontotype.Eiendel);
    private static readonly Konto Konto2700 = LagKonto("2700", "Utgaende MVA", Kontotype.Gjeld);
    private static readonly Konto Konto3000 = LagKonto("3000", "Salgsinntekt", Kontotype.Inntekt);
    private static readonly Konto Konto1500 = LagKonto("1500", "Kundefordringer", Kontotype.Eiendel);
    private static readonly Konto Konto6700 = LagKonto("6700", "Fremmedtjenester", Kontotype.Kostnad);
    private static readonly Konto Konto2400 = LagKonto("2400", "Leverandorgjeld", Kontotype.Gjeld);

    // Standard test MVA-koder
    private static readonly MvaKode MvaKode1 = new()
    {
        Id = Guid.NewGuid(), Kode = "1", Beskrivelse = "Inngaende 25%", StandardTaxCode = "1",
        Sats = 25m, Retning = MvaRetning.Inngaende,
        InngaendeKontoId = Konto2710.Id, InngaendeKonto = Konto2710, ErAktiv = true
    };
    private static readonly MvaKode MvaKode3 = new()
    {
        Id = Guid.NewGuid(), Kode = "3", Beskrivelse = "Utgaende 25%", StandardTaxCode = "3",
        Sats = 25m, Retning = MvaRetning.Utgaende,
        UtgaendeKontoId = Konto2700.Id, UtgaendeKonto = Konto2700, ErAktiv = true
    };
    private static readonly MvaKode MvaKode81 = new()
    {
        Id = Guid.NewGuid(), Kode = "81", Beskrivelse = "Snudd avregning 25%", StandardTaxCode = "81",
        Sats = 25m, Retning = MvaRetning.SnuddAvregning,
        InngaendeKontoId = Konto2710.Id, InngaendeKonto = Konto2710,
        UtgaendeKontoId = Konto2700.Id, UtgaendeKonto = Konto2700, ErAktiv = true
    };

    // Standard bilagserier
    private static readonly BilagSerie ManSerie = new()
    {
        Id = Guid.NewGuid(), Kode = "MAN", Navn = "Manuelt", StandardType = BilagType.Manuelt,
        ErAktiv = true, ErSystemserie = true, SaftJournalId = "MAN"
    };
    private static readonly BilagSerie KorSerie = new()
    {
        Id = Guid.NewGuid(), Kode = "KOR", Navn = "Korreksjon", StandardType = BilagType.Korreksjon,
        ErAktiv = true, ErSystemserie = true, SaftJournalId = "KOR"
    };

    // Standard periode
    private static readonly Regnskapsperiode ApenPeriode = new()
    {
        Id = Guid.NewGuid(), Ar = 2026, Periode = 1,
        FraDato = new DateOnly(2026, 1, 1), TilDato = new DateOnly(2026, 1, 31),
        Status = PeriodeStatus.Apen
    };

    public BilagRegistreringServiceTests()
    {
        _hovedbokRepo = new FakeHovedbokRepository();
        _bilagRepo = new FakeBilagRepository();
        _kontoplanRepo = new FakeKontoplanRepository();
        _mvaKodeService = new FakeMvaKodeService();
        _transactionManager = new FakeTransactionManager();

        _service = new BilagRegistreringService(
            _hovedbokRepo, _bilagRepo, _kontoplanRepo, _mvaKodeService, _transactionManager);

        // Setup standard test data
        _hovedbokRepo.Perioder.Add(ApenPeriode);
        _bilagRepo.Serier.Add(ManSerie);
        _bilagRepo.Serier.Add(KorSerie);

        foreach (var konto in new[] { Konto6800, Konto1920, Konto2710, Konto2700, Konto3000, Konto1500, Konto6700, Konto2400 })
            _kontoplanRepo.LeggTilKonto(konto);

        _mvaKodeService.LeggTilMvaKode(MvaKode1);
        _mvaKodeService.LeggTilMvaKode(MvaKode3);
        _mvaKodeService.LeggTilMvaKode(MvaKode81);
    }

    private static Konto LagKonto(string kontonummer, string navn, Kontotype type) => new()
    {
        Id = Guid.NewGuid(),
        Kontonummer = kontonummer,
        Navn = navn,
        Kontotype = type,
        Normalbalanse = type is Kontotype.Eiendel or Kontotype.Kostnad ? Normalbalanse.Debet : Normalbalanse.Kredit,
        KontogruppeId = KontoGruppeId,
        StandardAccountId = kontonummer,
        ErAktiv = true,
        ErBokforbar = true
    };

    private OpprettBilagRequest LagEnkeltBilagRequest(
        decimal debet = 1000m, decimal kredit = 1000m,
        string debetKonto = "6800", string kreditKonto = "1920",
        bool bokforDirekte = true) => new(
        BilagType.Manuelt,
        new DateOnly(2026, 1, 15),
        "Test bilag",
        null,
        "MAN",
        new List<OpprettPosteringRequest>
        {
            new(debetKonto, BokforingSide.Debet, debet, "Debet", null, null, null, null, null),
            new(kreditKonto, BokforingSide.Kredit, kredit, "Kredit", null, null, null, null, null)
        },
        bokforDirekte);

    // --- FR-1: Dobbelt bokholderi -- balansekrav ---

    [Fact]
    public async Task OpprettBilag_BalansertBilag_Suksess()
    {
        var request = LagEnkeltBilagRequest();
        var result = await _service.OpprettOgBokforBilagAsync(request);

        result.Should().NotBeNull();
        result.SumDebet.Should().Be(1000m);
        result.SumKredit.Should().Be(1000m);
    }

    [Fact]
    public async Task OpprettBilag_IkkeIBalanse_KasterFeil()
    {
        var request = LagEnkeltBilagRequest(debet: 1000m, kredit: 500m);

        var act = () => _service.OpprettOgBokforBilagAsync(request);

        await act.Should().ThrowAsync<Exception>()
            .Where(e => e.Message.Contains("balanse") || e is AccountingBalanceException);
    }

    // --- FR-2: Minimum antall posteringer ---

    [Fact]
    public async Task OpprettBilag_EnPostering_KasterFeil()
    {
        var request = new OpprettBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 1, 15), "Test", null, "MAN",
            new List<OpprettPosteringRequest>
            {
                new("6800", BokforingSide.Debet, 1000m, "Debet", null, null, null, null, null)
            }, true);

        var act = () => _service.OpprettOgBokforBilagAsync(request);

        await act.Should().ThrowAsync<Exception>();
    }

    // --- FR-3: Positive belop ---

    [Fact]
    public async Task ValiderBilag_NegativtBelop_GirFeil()
    {
        var request = new ValiderBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 1, 15), "Test", "MAN",
            new List<OpprettPosteringRequest>
            {
                new("6800", BokforingSide.Debet, -100m, "Negativ", null, null, null, null, null),
                new("1920", BokforingSide.Kredit, 100m, "Kredit", null, null, null, null, null)
            });

        var result = await _service.ValiderBilagAsync(request);

        result.ErGyldig.Should().BeFalse();
        result.Feil.Should().Contain(f => f.Kode == "BELOP_MA_VAERE_POSITIVT");
    }

    // --- FR-4: Fortlopende bilagsnummerering ---

    [Fact]
    public async Task OpprettBilag_FarBilagsnummer()
    {
        var request = LagEnkeltBilagRequest();
        var result = await _service.OpprettOgBokforBilagAsync(request);

        result.Bilagsnummer.Should().Be(1);
        result.Ar.Should().Be(2026);
        result.BilagsId.Should().Be("2026-00001");
    }

    [Fact]
    public async Task OpprettBilag_FarFortlopendeNummer()
    {
        var r1 = await _service.OpprettOgBokforBilagAsync(LagEnkeltBilagRequest());
        var r2 = await _service.OpprettOgBokforBilagAsync(LagEnkeltBilagRequest());

        r1.Bilagsnummer.Should().Be(1);
        r2.Bilagsnummer.Should().Be(2);
    }

    // --- FR-5: Bilagserier ---

    [Fact]
    public async Task OpprettBilag_MedSerie_FarSerieNummer()
    {
        var request = LagEnkeltBilagRequest();
        var result = await _service.OpprettOgBokforBilagAsync(request);

        result.SerieKode.Should().Be("MAN");
        result.SerieNummer.Should().Be(1);
        result.SerieBilagsId.Should().Be("MAN-2026-00001");
    }

    [Fact]
    public async Task OpprettBilag_UkjentSerie_KasterFeil()
    {
        var request = new OpprettBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 1, 15), "Test", null, "FINNES_IKKE",
            new List<OpprettPosteringRequest>
            {
                new("6800", BokforingSide.Debet, 1000m, "D", null, null, null, null, null),
                new("1920", BokforingSide.Kredit, 1000m, "K", null, null, null, null, null)
            }, true);

        var act = () => _service.OpprettOgBokforBilagAsync(request);
        await act.Should().ThrowAsync<BilagValideringException>()
            .Where(e => e.Message.Contains("SERIE_IKKE_FUNNET"));
    }

    [Fact]
    public async Task OpprettBilag_InaktivSerie_KasterFeil()
    {
        var inaktivSerie = new BilagSerie
        {
            Id = Guid.NewGuid(), Kode = "INAKTIV", Navn = "Inaktiv",
            StandardType = BilagType.Manuelt, ErAktiv = false, SaftJournalId = "INA"
        };
        _bilagRepo.Serier.Add(inaktivSerie);

        var request = new OpprettBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 1, 15), "Test", null, "INAKTIV",
            new List<OpprettPosteringRequest>
            {
                new("6800", BokforingSide.Debet, 1000m, "D", null, null, null, null, null),
                new("1920", BokforingSide.Kredit, 1000m, "K", null, null, null, null, null)
            }, true);

        var act = () => _service.OpprettOgBokforBilagAsync(request);
        await act.Should().ThrowAsync<BilagValideringException>()
            .Where(e => e.Message.Contains("SERIE_INAKTIV"));
    }

    // --- FR-6: Periodevalidering ---

    [Fact]
    public async Task OpprettBilag_LukketPeriode_KasterFeil()
    {
        var lukketPeriode = new Regnskapsperiode
        {
            Id = Guid.NewGuid(), Ar = 2025, Periode = 12,
            FraDato = new DateOnly(2025, 12, 1), TilDato = new DateOnly(2025, 12, 31),
            Status = PeriodeStatus.Lukket
        };
        _hovedbokRepo.Perioder.Add(lukketPeriode);

        var request = new OpprettBilagRequest(
            BilagType.Manuelt, new DateOnly(2025, 12, 15), "Test", null, "MAN",
            new List<OpprettPosteringRequest>
            {
                new("6800", BokforingSide.Debet, 1000m, "D", null, null, null, null, null),
                new("1920", BokforingSide.Kredit, 1000m, "K", null, null, null, null, null)
            }, true);

        var act = () => _service.OpprettOgBokforBilagAsync(request);
        await act.Should().ThrowAsync<BilagValideringException>()
            .Where(e => e.Message.Contains("PERIODE_LUKKET"));
    }

    [Fact]
    public async Task OpprettBilag_SperretPeriode_KasterFeil()
    {
        var sperretPeriode = new Regnskapsperiode
        {
            Id = Guid.NewGuid(), Ar = 2026, Periode = 2,
            FraDato = new DateOnly(2026, 2, 1), TilDato = new DateOnly(2026, 2, 28),
            Status = PeriodeStatus.Sperret
        };
        _hovedbokRepo.Perioder.Add(sperretPeriode);

        var request = new OpprettBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 2, 15), "Test", null, "MAN",
            new List<OpprettPosteringRequest>
            {
                new("6800", BokforingSide.Debet, 1000m, "D", null, null, null, null, null),
                new("1920", BokforingSide.Kredit, 1000m, "K", null, null, null, null, null)
            }, true);

        var act = () => _service.OpprettOgBokforBilagAsync(request);
        await act.Should().ThrowAsync<BilagValideringException>()
            .Where(e => e.Message.Contains("PERIODE_SPERRET"));
    }

    [Fact]
    public async Task OpprettBilag_PeriodeFinnesIkke_KasterFeil()
    {
        var request = new OpprettBilagRequest(
            BilagType.Manuelt, new DateOnly(2030, 6, 15), "Test", null, "MAN",
            new List<OpprettPosteringRequest>
            {
                new("6800", BokforingSide.Debet, 1000m, "D", null, null, null, null, null),
                new("1920", BokforingSide.Kredit, 1000m, "K", null, null, null, null, null)
            }, true);

        var act = () => _service.OpprettOgBokforBilagAsync(request);
        await act.Should().ThrowAsync<Exception>();
    }

    // --- FR-7: Kontovalidering ---

    [Fact]
    public async Task ValiderBilag_KontoFinnesIkke_GirFeil()
    {
        var request = new ValiderBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 1, 15), "Test", "MAN",
            new List<OpprettPosteringRequest>
            {
                new("9999", BokforingSide.Debet, 1000m, "D", null, null, null, null, null),
                new("1920", BokforingSide.Kredit, 1000m, "K", null, null, null, null, null)
            });

        var result = await _service.ValiderBilagAsync(request);

        result.ErGyldig.Should().BeFalse();
        result.Feil.Should().Contain(f => f.Kode == "KONTO_IKKE_FUNNET");
    }

    [Fact]
    public async Task ValiderBilag_InaktivKonto_GirFeil()
    {
        var inaktivKonto = LagKonto("6900", "Inaktiv konto", Kontotype.Kostnad);
        inaktivKonto.ErAktiv = false;
        _kontoplanRepo.LeggTilKonto(inaktivKonto);

        var request = new ValiderBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 1, 15), "Test", "MAN",
            new List<OpprettPosteringRequest>
            {
                new("6900", BokforingSide.Debet, 1000m, "D", null, null, null, null, null),
                new("1920", BokforingSide.Kredit, 1000m, "K", null, null, null, null, null)
            });

        var result = await _service.ValiderBilagAsync(request);

        result.ErGyldig.Should().BeFalse();
        result.Feil.Should().Contain(f => f.Kode == "KONTO_INAKTIV");
    }

    [Fact]
    public async Task ValiderBilag_IkkeBokforbarKonto_GirFeil()
    {
        var summekonto = LagKonto("6000", "Summekonto", Kontotype.Kostnad);
        summekonto.ErBokforbar = false;
        _kontoplanRepo.LeggTilKonto(summekonto);

        var request = new ValiderBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 1, 15), "Test", "MAN",
            new List<OpprettPosteringRequest>
            {
                new("6000", BokforingSide.Debet, 1000m, "D", null, null, null, null, null),
                new("1920", BokforingSide.Kredit, 1000m, "K", null, null, null, null, null)
            });

        var result = await _service.ValiderBilagAsync(request);

        result.ErGyldig.Should().BeFalse();
        result.Feil.Should().Contain(f => f.Kode == "KONTO_IKKE_BOKFORBAR");
    }

    [Fact]
    public async Task ValiderBilag_KreverAvdelingUtenAvdeling_GirFeil()
    {
        var avdKonto = LagKonto("6100", "Avdelingskonto", Kontotype.Kostnad);
        avdKonto.KreverAvdeling = true;
        _kontoplanRepo.LeggTilKonto(avdKonto);

        var request = new ValiderBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 1, 15), "Test", "MAN",
            new List<OpprettPosteringRequest>
            {
                new("6100", BokforingSide.Debet, 1000m, "D", null, null, null, null, null),
                new("1920", BokforingSide.Kredit, 1000m, "K", null, null, null, null, null)
            });

        var result = await _service.ValiderBilagAsync(request);

        result.ErGyldig.Should().BeFalse();
        result.Feil.Should().Contain(f => f.Kode == "AVDELING_PAKREVD");
    }

    [Fact]
    public async Task ValiderBilag_KreverProsjektUtenProsjekt_GirFeil()
    {
        var projKonto = LagKonto("6200", "Prosjektkonto", Kontotype.Kostnad);
        projKonto.KreverProsjekt = true;
        _kontoplanRepo.LeggTilKonto(projKonto);

        var request = new ValiderBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 1, 15), "Test", "MAN",
            new List<OpprettPosteringRequest>
            {
                new("6200", BokforingSide.Debet, 1000m, "D", null, null, null, null, null),
                new("1920", BokforingSide.Kredit, 1000m, "K", null, null, null, null, null)
            });

        var result = await _service.ValiderBilagAsync(request);

        result.ErGyldig.Should().BeFalse();
        result.Feil.Should().Contain(f => f.Kode == "PROSJEKT_PAKREVD");
    }

    // --- FR-8 & FR-9: MVA-validering og auto-postering ---

    [Fact]
    public async Task OpprettBilag_MedInngaendeMva_GenererAutoPostering()
    {
        // Kjop av kontorrekvisita kr 1000 + MVA 25%
        var request = new OpprettBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 1, 15), "Kjop kontorrekvisita", null, "MAN",
            new List<OpprettPosteringRequest>
            {
                new("6800", BokforingSide.Debet, 1000m, "Kontorkostnad", "1", null, null, null, null),
                new("1920", BokforingSide.Kredit, 1250m, "Betaling", null, null, null, null, null)
            }, true);

        var result = await _service.OpprettOgBokforBilagAsync(request);

        result.Posteringer.Should().HaveCount(3);
        result.Posteringer.Should().Contain(p => p.ErAutoGenerertMva && p.Belop == 250m);
        result.SumDebet.Should().Be(1250m);
        result.SumKredit.Should().Be(1250m);
    }

    [Fact]
    public async Task OpprettBilag_MedUtgaendeMva_GenererAutoPostering()
    {
        // Salg kr 10000 + MVA 25%
        var request = new OpprettBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 1, 15), "Salg", null, "MAN",
            new List<OpprettPosteringRequest>
            {
                new("3000", BokforingSide.Kredit, 10000m, "Salgsinntekt", "3", null, null, null, null),
                new("1500", BokforingSide.Debet, 12500m, "Kundefordring", null, null, null, null, null)
            }, true);

        var result = await _service.OpprettOgBokforBilagAsync(request);

        result.Posteringer.Should().HaveCount(3);
        result.Posteringer.Should().Contain(p => p.ErAutoGenerertMva && p.Side == "Kredit" && p.Belop == 2500m);
        result.SumDebet.Should().Be(12500m);
        result.SumKredit.Should().Be(12500m);
    }

    [Fact]
    public async Task OpprettBilag_MedSnuddAvregning_GenererToAutoLinjer()
    {
        // Kjop av tjenester fra utlandet 5000 + snudd avregning 25%
        var request = new OpprettBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 1, 15), "Snudd avregning", null, "MAN",
            new List<OpprettPosteringRequest>
            {
                new("6700", BokforingSide.Debet, 5000m, "Fremmedtjenester", "81", null, null, null, null),
                new("2400", BokforingSide.Kredit, 5000m, "Leverandorgjeld", null, null, null, null, null)
            }, true);

        var result = await _service.OpprettOgBokforBilagAsync(request);

        // Skal ha 4 linjer: 2 bruker + 2 MVA-auto
        result.Posteringer.Should().HaveCount(4);
        var mvaLinjer = result.Posteringer.Where(p => p.ErAutoGenerertMva).ToList();
        mvaLinjer.Should().HaveCount(2);
        mvaLinjer.Should().Contain(p => p.Side == "Debet" && p.Belop == 1250m);
        mvaLinjer.Should().Contain(p => p.Side == "Kredit" && p.Belop == 1250m);
        result.SumDebet.Should().Be(6250m);
        result.SumKredit.Should().Be(6250m);
    }

    [Fact]
    public async Task ValiderBilag_MvaKodeFinnesIkke_GirFeil()
    {
        var request = new ValiderBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 1, 15), "Test", "MAN",
            new List<OpprettPosteringRequest>
            {
                new("6800", BokforingSide.Debet, 1000m, "D", "99", null, null, null, null),
                new("1920", BokforingSide.Kredit, 1000m, "K", null, null, null, null, null)
            });

        var result = await _service.ValiderBilagAsync(request);

        result.ErGyldig.Should().BeFalse();
        result.Feil.Should().Contain(f => f.Kode == "MVA_KODE_IKKE_FUNNET");
    }

    // --- FR-10: Tilbakeforing ---

    [Fact]
    public async Task TilbakeforBilag_SpeilvendtePosteringer()
    {
        // Opprett og bokfor
        var bilag = await _service.OpprettOgBokforBilagAsync(LagEnkeltBilagRequest());

        // Tilbakefor
        var tilbakeforRequest = new TilbakeforBilagRequest(
            bilag.Id, new DateOnly(2026, 1, 20), "Feil bilag");
        var tilbakefort = await _service.TilbakeforBilagAsync(tilbakeforRequest);

        tilbakefort.Should().NotBeNull();
        tilbakefort.SerieKode.Should().Be("KOR");
        tilbakefort.Posteringer.Should().HaveCount(2);

        // Sjekk at sider er speilvendt
        var origDebet = bilag.Posteringer.First(p => p.Side == "Debet");
        var tilbDebet = tilbakefort.Posteringer.First(p => p.Kontonummer == origDebet.Kontonummer);
        tilbDebet.Side.Should().Be("Kredit");
    }

    [Fact]
    public async Task TilbakeforBilag_IkkeBokfort_KasterFeil()
    {
        // Opprett kladd (ikke bokfort)
        var bilag = await _service.OpprettOgBokforBilagAsync(LagEnkeltBilagRequest(bokforDirekte: false));

        var act = () => _service.TilbakeforBilagAsync(
            new TilbakeforBilagRequest(bilag.Id, new DateOnly(2026, 1, 20), "Feil"));

        await act.Should().ThrowAsync<BilagIkkeBokfortForTilbakeforingException>();
    }

    [Fact]
    public async Task TilbakeforBilag_AlleredeTilbakfort_KasterFeil()
    {
        var bilag = await _service.OpprettOgBokforBilagAsync(LagEnkeltBilagRequest());
        await _service.TilbakeforBilagAsync(
            new TilbakeforBilagRequest(bilag.Id, new DateOnly(2026, 1, 20), "Feil"));

        var act = () => _service.TilbakeforBilagAsync(
            new TilbakeforBilagRequest(bilag.Id, new DateOnly(2026, 1, 21), "Igjen"));

        await act.Should().ThrowAsync<BilagAlleredeTilbakfortException>();
    }

    // --- FR-11: Bokforing mot hovedbok ---

    [Fact]
    public async Task OpprettBilag_BokforDirekte_OppdatererKontoSaldo()
    {
        var request = LagEnkeltBilagRequest();
        var result = await _service.OpprettOgBokforBilagAsync(request);

        result.ErBokfort.Should().BeTrue();
        result.BokfortTidspunkt.Should().NotBeNull();

        var saldo6800 = _hovedbokRepo.SaldoListe.FirstOrDefault(s => s.Kontonummer == "6800");
        saldo6800.Should().NotBeNull();
        saldo6800!.SumDebet.Verdi.Should().Be(1000m);
    }

    [Fact]
    public async Task OpprettBilag_SomKladd_OppdatererIkkeKontoSaldo()
    {
        var request = LagEnkeltBilagRequest(bokforDirekte: false);
        var result = await _service.OpprettOgBokforBilagAsync(request);

        result.ErBokfort.Should().BeFalse();
        result.BokfortTidspunkt.Should().BeNull();

        _hovedbokRepo.SaldoListe.Should().BeEmpty();
    }

    [Fact]
    public async Task BokforBilag_KladdBilag_Suksess()
    {
        var bilag = await _service.OpprettOgBokforBilagAsync(LagEnkeltBilagRequest(bokforDirekte: false));

        bilag.ErBokfort.Should().BeFalse();

        var bokfort = await _service.BokforBilagAsync(bilag.Id);

        bokfort.ErBokfort.Should().BeTrue();
        _hovedbokRepo.SaldoListe.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BokforBilag_AlleredeBokfort_KasterFeil()
    {
        var bilag = await _service.OpprettOgBokforBilagAsync(LagEnkeltBilagRequest());

        var act = () => _service.BokforBilagAsync(bilag.Id);
        await act.Should().ThrowAsync<BilagAlleredeBokfortException>();
    }

    // --- FR-13: Vedlegg ---

    [Fact]
    public async Task LeggTilVedlegg_GyldigMimeType_Suksess()
    {
        var bilag = await _service.OpprettOgBokforBilagAsync(LagEnkeltBilagRequest(bokforDirekte: false));

        var vedleggRequest = new LeggTilVedleggRequest(
            bilag.Id, "faktura.pdf", "application/pdf", 1024,
            "/vedlegg/test.pdf", "abc123def456", "Fakturakopi");

        var vedlegg = await _service.LeggTilVedleggAsync(vedleggRequest);

        vedlegg.Filnavn.Should().Be("faktura.pdf");
        vedlegg.MimeType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task LeggTilVedlegg_UgyldigMimeType_KasterFeil()
    {
        var bilag = await _service.OpprettOgBokforBilagAsync(LagEnkeltBilagRequest(bokforDirekte: false));

        var vedleggRequest = new LeggTilVedleggRequest(
            bilag.Id, "script.exe", "application/octet-stream", 1024,
            "/vedlegg/test.exe", "abc123", null);

        var act = () => _service.LeggTilVedleggAsync(vedleggRequest);
        await act.Should().ThrowAsync<UgyldigMimeTypeException>();
    }

    [Fact]
    public async Task SlettVedlegg_BokfortBilag_KasterFeil()
    {
        var bilag = await _service.OpprettOgBokforBilagAsync(LagEnkeltBilagRequest());

        var vedleggRequest = new LeggTilVedleggRequest(
            bilag.Id, "faktura.pdf", "application/pdf", 1024,
            "/vedlegg/test.pdf", "abc123", null);
        var vedlegg = await _service.LeggTilVedleggAsync(vedleggRequest);

        var act = () => _service.SlettVedleggAsync(bilag.Id, vedlegg.Id);
        await act.Should().ThrowAsync<VedleggPaBokfortBilagException>();
    }

    // --- FR-14: Kladd vs. direkte bokforing ---

    [Fact]
    public async Task OpprettBilag_BokforDirekteTrue_BokforesUmiddelbart()
    {
        var result = await _service.OpprettOgBokforBilagAsync(LagEnkeltBilagRequest(bokforDirekte: true));
        result.ErBokfort.Should().BeTrue();
    }

    [Fact]
    public async Task OpprettBilag_BokforDirekteFalse_BliKladd()
    {
        var result = await _service.OpprettOgBokforBilagAsync(LagEnkeltBilagRequest(bokforDirekte: false));
        result.ErBokfort.Should().BeFalse();
    }

    // --- Bilagserier ---

    [Fact]
    public async Task HentAlleBilagSerier_ReturnererSerier()
    {
        var serier = await _service.HentAlleBilagSerierAsync();
        serier.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task OpprettBilagSerie_Suksess()
    {
        var request = new OpprettBilagSerieRequest("TEST", "Testserie", "Test Series", BilagType.Manuelt, "TEST");
        var result = await _service.OpprettBilagSerieAsync(request);

        result.Kode.Should().Be("TEST");
        result.Navn.Should().Be("Testserie");
    }

    // --- Validering (komplett) ---

    [Fact]
    public async Task ValiderBilag_GyldigBilag_ErGyldig()
    {
        var request = new ValiderBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 1, 15), "Test", "MAN",
            new List<OpprettPosteringRequest>
            {
                new("6800", BokforingSide.Debet, 1000m, "D", null, null, null, null, null),
                new("1920", BokforingSide.Kredit, 1000m, "K", null, null, null, null, null)
            });

        var result = await _service.ValiderBilagAsync(request);

        result.ErGyldig.Should().BeTrue();
        result.Feil.Should().BeEmpty();
    }

    [Fact]
    public async Task ValiderBilag_MedMva_ViserMvaPosteringer()
    {
        var request = new ValiderBilagRequest(
            BilagType.Manuelt, new DateOnly(2026, 1, 15), "Test", "MAN",
            new List<OpprettPosteringRequest>
            {
                new("6800", BokforingSide.Debet, 1000m, "D", "1", null, null, null, null),
                new("1920", BokforingSide.Kredit, 1250m, "K", null, null, null, null, null)
            });

        var result = await _service.ValiderBilagAsync(request);

        result.ErGyldig.Should().BeTrue();
        result.GenererteMvaPosteringer.Should().NotBeNull();
        result.GenererteMvaPosteringer.Should().HaveCount(1);
    }
}
