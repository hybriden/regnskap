using FluentAssertions;
using Regnskap.Application.Features.Hovedbok;
using Regnskap.Application.Features.Periodeavslutning;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Periodeavslutning;

namespace Regnskap.Tests.Features.Periodeavslutning;

public class PeriodeavslutningServiceTests
{
    private readonly FakePeriodeavslutningRepository _repo;
    private readonly FakeHovedbokRepositoryForPeriodeavslutning _hovedbokRepo;
    private readonly FakePeriodeServiceForPeriodeavslutning _periodeService;
    private readonly FakeBilagRegistreringServiceForPeriodeavslutning _bilagService;
    private readonly PeriodeavslutningService _service;

    public PeriodeavslutningServiceTests()
    {
        _repo = new FakePeriodeavslutningRepository();
        _hovedbokRepo = new FakeHovedbokRepositoryForPeriodeavslutning();
        _periodeService = new FakePeriodeServiceForPeriodeavslutning();
        _bilagService = new FakeBilagRegistreringServiceForPeriodeavslutning();
        _service = new PeriodeavslutningService(_repo, _hovedbokRepo, _periodeService, _bilagService);
    }

    // --- KjorAvstemming ---

    [Fact]
    public async Task KjorAvstemming_AlleOk_ErKlarForLukking()
    {
        // Forrige periode lukket
        _hovedbokRepo.Perioder.Add(LagPeriode(2026, 1, PeriodeStatus.Lukket));
        _hovedbokRepo.Perioder.Add(LagPeriode(2026, 2, PeriodeStatus.Apen));

        var resultat = await _service.KjorAvstemmingAsync(2026, 2);

        resultat.ErKlarForLukking.Should().BeTrue();
        resultat.Kontroller.Should().Contain(k => k.Kode == "FORRIGE_LUKKET" && k.Status == "OK");
        resultat.Kontroller.Should().Contain(k => k.Kode == "DEBET_KREDIT" && k.Status == "OK");
    }

    [Fact]
    public async Task KjorAvstemming_ForrigePeriodeIkkeLukket_IkkeKlar()
    {
        _hovedbokRepo.Perioder.Add(LagPeriode(2026, 1, PeriodeStatus.Apen));

        var resultat = await _service.KjorAvstemmingAsync(2026, 2);

        resultat.ErKlarForLukking.Should().BeFalse();
        resultat.Kontroller.Should().Contain(k => k.Kode == "FORRIGE_LUKKET" && k.Status == "FEIL");
    }

    [Fact]
    public async Task KjorAvstemming_ForstePeriode_IngenForrigeSjekk()
    {
        _hovedbokRepo.Perioder.Add(LagPeriode(2026, 1, PeriodeStatus.Apen));

        var resultat = await _service.KjorAvstemmingAsync(2026, 1);

        resultat.Kontroller.Should().Contain(k => k.Kode == "FORRIGE_LUKKET" && k.Status == "OK");
    }

    // --- LukkPeriode ---

    [Fact]
    public async Task LukkPeriode_MedOkAvstemming_LukkerPerioden()
    {
        var p = LagPeriode(2026, 1, PeriodeStatus.Apen);
        _hovedbokRepo.Perioder.Add(p);

        var resultat = await _service.LukkPeriodeAsync(2026, 1, new LukkPeriodeRequest());

        resultat.NyStatus.Should().Be("Lukket");
        p.Status.Should().Be(PeriodeStatus.Lukket);
        p.LukketTidspunkt.Should().NotBeNull();
    }

    [Fact]
    public async Task LukkPeriode_AlleredeLukket_KasterException()
    {
        _hovedbokRepo.Perioder.Add(LagPeriode(2026, 1, PeriodeStatus.Lukket));

        var act = () => _service.LukkPeriodeAsync(2026, 1, new LukkPeriodeRequest());

        await act.Should().ThrowAsync<PeriodeLukkingException>();
    }

    // --- GjenapnePeriode ---

    [Fact]
    public async Task GjenapnePeriode_UtenEtterfolgende_Lykkes()
    {
        var p = LagPeriode(2026, 3, PeriodeStatus.Lukket);
        p.LukketTidspunkt = DateTime.UtcNow;
        p.LukketAv = "test";
        _hovedbokRepo.Perioder.Add(p);

        var resultat = await _service.GjenapnePeriodeAsync(2026, 3, new GjenapnePeriodeRequest("Korreksjon"));

        resultat.NyStatus.Should().Be("Apen");
        p.Status.Should().Be(PeriodeStatus.Apen);
    }

    [Fact]
    public async Task GjenapnePeriode_MedEtterfolgende_KasterException()
    {
        _hovedbokRepo.Perioder.Add(LagPeriode(2026, 3, PeriodeStatus.Lukket));
        _hovedbokRepo.Perioder.Add(LagPeriode(2026, 4, PeriodeStatus.Lukket));

        var act = () => _service.GjenapnePeriodeAsync(2026, 3, new GjenapnePeriodeRequest("Korreksjon"));

        await act.Should().ThrowAsync<PeriodeLukkingException>()
            .WithMessage("*etterfølgende*");
    }

    // --- KjorArsavslutning ---

    [Fact]
    public async Task KjorArsavslutning_MedLukketePerioderOgSaldoer_Fullfort()
    {
        SetupArsavslutning(2026);

        // Saldoer: Inntekter klasse 3 = 500.000 kredit, Kostnader klasse 6 = 300.000 debet
        _hovedbokRepo.SaldoerPerPeriode[(2026, 1)] = new List<KontoSaldo>
        {
            LagKontoSaldo("3000", 2026, 1, 0m, 500_000m), // Inntekter
            LagKontoSaldo("6000", 2026, 1, 300_000m, 0m), // Kostnader
            LagKontoSaldo("1920", 2026, 1, 200_000m, 0m), // Bank (eiendel)
        };

        var request = new ArsavslutningRequest("2050");

        var resultat = await _service.KjorArsavslutningAsync(2026, request);

        resultat.Fase.Should().Be(ArsavslutningFase.Fullfort);
        resultat.Arsresultat.Should().Be(200_000m); // 500k - 300k
        resultat.DisponertTilEgenkapital.Should().Be(200_000m);
        resultat.ArsavslutningBilagId.Should().NotBeEmpty();
        _bilagService.AlleRequests.Should().NotBeEmpty();
    }

    [Fact]
    public async Task KjorArsavslutning_MedUnderskudd_BelasterEgenkapital()
    {
        SetupArsavslutning(2026);

        // Kostnader > Inntekter
        _hovedbokRepo.SaldoerPerPeriode[(2026, 1)] = new List<KontoSaldo>
        {
            LagKontoSaldo("3000", 2026, 1, 0m, 100_000m),
            LagKontoSaldo("6000", 2026, 1, 300_000m, 0m),
            LagKontoSaldo("1920", 2026, 1, 200_000m, 0m),
        };

        var request = new ArsavslutningRequest("2050");
        var resultat = await _service.KjorArsavslutningAsync(2026, request);

        resultat.Arsresultat.Should().Be(-200_000m);
    }

    [Fact]
    public async Task KjorArsavslutning_IkkeLukketePerioderFeil()
    {
        _hovedbokRepo.Perioder.Add(LagPeriode(2026, 1, PeriodeStatus.Apen));

        var act = () => _service.KjorArsavslutningAsync(2026, new ArsavslutningRequest());

        await act.Should().ThrowAsync<ArsavslutningException>()
            .WithMessage("*ikke lukket*");
    }

    [Fact]
    public async Task KjorArsavslutning_MedUtbytte_FordelesKorrekt()
    {
        SetupArsavslutning(2026);

        _hovedbokRepo.SaldoerPerPeriode[(2026, 1)] = new List<KontoSaldo>
        {
            LagKontoSaldo("3000", 2026, 1, 0m, 500_000m),
            LagKontoSaldo("6000", 2026, 1, 200_000m, 0m),
            LagKontoSaldo("1920", 2026, 1, 300_000m, 0m),
        };

        var request = new ArsavslutningRequest("2050", Utbytte: 100_000m);
        var resultat = await _service.KjorArsavslutningAsync(2026, request);

        resultat.Arsresultat.Should().Be(300_000m);
        resultat.Utbytte.Should().Be(100_000m);
        resultat.DisponertTilEgenkapital.Should().Be(200_000m);
    }

    [Fact]
    public async Task KjorArsavslutning_MedUtbytteSomOverstigerResultat_KasterException()
    {
        SetupArsavslutning(2026);

        _hovedbokRepo.SaldoerPerPeriode[(2026, 1)] = new List<KontoSaldo>
        {
            LagKontoSaldo("3000", 2026, 1, 0m, 100_000m),
            LagKontoSaldo("6000", 2026, 1, 50_000m, 0m),
        };

        var request = new ArsavslutningRequest("2050", Utbytte: 200_000m);
        var act = () => _service.KjorArsavslutningAsync(2026, request);

        await act.Should().ThrowAsync<ArsavslutningException>()
            .WithMessage("*Utbytte*overstige*");
    }

    // --- SjekkKlargjoring ---

    [Fact]
    public async Task SjekkKlargjoring_UtenArsavslutning_IkkeKlar()
    {
        var resultat = await _service.SjekkKlargjoringAsync(2026);

        resultat.ErKlar.Should().BeFalse();
        resultat.Kontroller.Should().Contain(k => k.Kode == "ARSAVSLUTNING" && k.Status == "FEIL");
    }

    [Fact]
    public async Task SjekkKlargjoring_MedArsavslutning_Klar()
    {
        _repo.ArsavslutningStatuser.Add(new ArsavslutningStatus
        {
            Id = Guid.NewGuid(), Ar = 2026, Fase = ArsavslutningFase.Fullfort
        });

        var resultat = await _service.SjekkKlargjoringAsync(2026);

        resultat.ErKlar.Should().BeTrue();
        resultat.Frister.Godkjenningsfrist.Should().Be(new DateOnly(2027, 6, 30));
        resultat.Frister.Innsendingsfrist.Should().Be(new DateOnly(2027, 7, 31));
    }

    // --- Helpers ---

    private void SetupArsavslutning(int ar)
    {
        // Alle perioder 1-12 lukket
        for (int m = 1; m <= 12; m++)
            _hovedbokRepo.Perioder.Add(LagPeriode(ar, m, PeriodeStatus.Lukket));

        // Periode 13 apen
        _hovedbokRepo.Perioder.Add(LagPeriode(ar, 13, PeriodeStatus.Apen));

        // Default tomme saldoer for alle perioder
        for (int m = 0; m <= 13; m++)
        {
            if (!_hovedbokRepo.SaldoerPerPeriode.ContainsKey((ar, m)))
                _hovedbokRepo.SaldoerPerPeriode[(ar, m)] = new List<KontoSaldo>();
        }
    }

    private static Regnskapsperiode LagPeriode(int ar, int periode, PeriodeStatus status) => new()
    {
        Id = Guid.NewGuid(),
        Ar = ar,
        Periode = periode,
        FraDato = periode is 0 or 13 ? new DateOnly(ar, 1, 1) : new DateOnly(ar, periode, 1),
        TilDato = periode is 0 or 13 ? new DateOnly(ar, 12, 31) : new DateOnly(ar, periode, DateTime.DaysInMonth(ar, periode)),
        Status = status,
        LukketTidspunkt = status == PeriodeStatus.Lukket ? DateTime.UtcNow : null,
        LukketAv = status == PeriodeStatus.Lukket ? "test" : null
    };

    private static KontoSaldo LagKontoSaldo(string kontonummer, int ar, int periode,
        decimal debet, decimal kredit) => new()
    {
        Id = Guid.NewGuid(),
        KontoId = Guid.NewGuid(),
        Kontonummer = kontonummer,
        RegnskapsperiodeId = Guid.NewGuid(),
        Ar = ar,
        Periode = periode,
        InngaendeBalanse = Belop.Null,
        SumDebet = new Belop(debet),
        SumKredit = new Belop(kredit),
        AntallPosteringer = 1
    };
}

/// <summary>
/// Fake IHovedbokRepository for testing periodeavslutning.
/// </summary>
public class FakeHovedbokRepositoryForPeriodeavslutning : IHovedbokRepository
{
    public List<Regnskapsperiode> Perioder { get; } = new();
    public List<Bilag> Bilag { get; } = new();
    public Dictionary<(int Ar, int Periode), List<KontoSaldo>> SaldoerPerPeriode { get; } = new();
    public List<int> Bilagsnumre { get; } = new();

    public Task<Regnskapsperiode?> HentPeriodeAsync(int ar, int periode, CancellationToken ct = default)
        => Task.FromResult(Perioder.FirstOrDefault(p => p.Ar == ar && p.Periode == periode));

    public Task<Regnskapsperiode?> HentPeriodeForDatoAsync(DateOnly dato, CancellationToken ct = default)
        => Task.FromResult(Perioder.FirstOrDefault(p => dato >= p.FraDato && dato <= p.TilDato));

    public Task<List<Regnskapsperiode>> HentPerioderForArAsync(int ar, CancellationToken ct = default)
        => Task.FromResult(Perioder.Where(p => p.Ar == ar).OrderBy(p => p.Periode).ToList());

    public Task<List<Regnskapsperiode>> HentApnePerioderAsync(CancellationToken ct = default)
        => Task.FromResult(Perioder.Where(p => p.ErApen).ToList());

    public Task LeggTilPeriodeAsync(Regnskapsperiode periode, CancellationToken ct = default)
    {
        Perioder.Add(periode);
        return Task.CompletedTask;
    }

    public Task<bool> PeriodeFinnesAsync(int ar, int periode, CancellationToken ct = default)
        => Task.FromResult(Perioder.Any(p => p.Ar == ar && p.Periode == periode));

    public Task<Bilag?> HentBilagAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Bilag.FirstOrDefault(b => b.Id == id));

    public Task<Bilag?> HentBilagMedPosteringerAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(Bilag.FirstOrDefault(b => b.Id == id));

    public Task<Bilag?> HentBilagMedNummerAsync(int ar, int bilagsnummer, CancellationToken ct = default)
        => Task.FromResult(Bilag.FirstOrDefault(b => b.Ar == ar && b.Bilagsnummer == bilagsnummer));

    public Task<int> NestebilagsnummerAsync(int ar, CancellationToken ct = default)
        => Task.FromResult(Bilag.Where(b => b.Ar == ar).Select(b => b.Bilagsnummer).DefaultIfEmpty(0).Max() + 1);

    public Task LeggTilBilagAsync(Bilag bilag, CancellationToken ct = default)
    {
        Bilag.Add(bilag);
        return Task.CompletedTask;
    }

    public Task<List<Bilag>> HentBilagForPeriodeAsync(
        int ar, int? periode = null, BilagType? type = null,
        int side = 1, int antall = 50, CancellationToken ct = default)
        => Task.FromResult(new List<Bilag>());

    public Task<int> TellBilagForPeriodeAsync(int ar, int? periode = null, BilagType? type = null, CancellationToken ct = default)
        => Task.FromResult(0);

    public Task<List<int>> HentBilagsnumreForArAsync(int ar, CancellationToken ct = default)
        => Task.FromResult(Bilagsnumre.ToList());

    public Task<List<Postering>> HentPosteringerForKontoAsync(
        string kontonummer, DateOnly? fraDato = null, DateOnly? tilDato = null,
        int side = 1, int antall = 100, CancellationToken ct = default)
        => Task.FromResult(new List<Postering>());

    public Task<int> TellPosteringerForKontoAsync(
        string kontonummer, DateOnly? fraDato = null, DateOnly? tilDato = null, CancellationToken ct = default)
        => Task.FromResult(0);

    public Task<bool> PeriodeHarPosteringerAsync(int ar, int periode, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<KontoSaldo?> HentKontoSaldoAsync(
        string kontonummer, int ar, int periode, CancellationToken ct = default)
    {
        if (SaldoerPerPeriode.TryGetValue((ar, periode), out var saldoer))
            return Task.FromResult(saldoer.FirstOrDefault(s => s.Kontonummer == kontonummer));
        return Task.FromResult<KontoSaldo?>(null);
    }

    public Task<List<KontoSaldo>> HentAlleSaldoerForPeriodeAsync(
        int ar, int periode, CancellationToken ct = default)
    {
        if (SaldoerPerPeriode.TryGetValue((ar, periode), out var saldoer))
            return Task.FromResult(saldoer);
        return Task.FromResult(new List<KontoSaldo>());
    }

    public Task<List<KontoSaldo>> HentSaldoHistorikkForKontoAsync(
        string kontonummer, int ar, CancellationToken ct = default)
        => Task.FromResult(new List<KontoSaldo>());

    public Task LeggTilKontoSaldoAsync(KontoSaldo saldo, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task LagreEndringerAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}

/// <summary>
/// Fake IPeriodeService for testing.
/// </summary>
public class FakePeriodeServiceForPeriodeavslutning : IPeriodeService
{
    public Task<List<RegnskapsperiodeDto>> OpprettPerioderForArAsync(int ar, CancellationToken ct = default)
    {
        var perioder = new List<RegnskapsperiodeDto>();
        for (int m = 0; m <= 13; m++)
        {
            perioder.Add(new RegnskapsperiodeDto(
                Guid.NewGuid(), ar, m, $"{ar}-{m:D2}",
                new DateOnly(ar, 1, 1), new DateOnly(ar, 12, 31),
                "Apen", null, null, null));
        }
        return Task.FromResult(perioder);
    }

    public Task<List<RegnskapsperiodeDto>> HentPerioderAsync(int ar, CancellationToken ct = default)
        => Task.FromResult(new List<RegnskapsperiodeDto>());

    public Task<RegnskapsperiodeDto> EndrePeriodeStatusAsync(
        int ar, int periode, PeriodeStatus nyStatus, string? merknad = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<PeriodeavstemmingDto> KjorPeriodeavstemmingAsync(
        int ar, int periode, CancellationToken ct = default)
        => Task.FromResult(new PeriodeavstemmingDto(ar, periode, true, new()));
}
