using FluentAssertions;
using Regnskap.Application.Features.Periodeavslutning;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Periodeavslutning;

namespace Regnskap.Tests.Features.Periodeavslutning;

public class AvskrivningServiceTests
{
    private readonly FakePeriodeavslutningRepository _repo;
    private readonly FakeBilagRegistreringServiceForPeriodeavslutning _bilagService;
    private readonly AvskrivningService _service;

    public AvskrivningServiceTests()
    {
        _repo = new FakePeriodeavslutningRepository();
        _bilagService = new FakeBilagRegistreringServiceForPeriodeavslutning();
        _service = new AvskrivningService(_repo, _bilagService);
    }

    // --- OpprettAnleggsmiddel ---

    [Fact]
    public async Task OpprettAnleggsmiddel_MedGyldigeVerdier_LagerAnleggsmiddel()
    {
        var request = LagAnleggsmiddelRequest();

        var resultat = await _service.OpprettAnleggsmiddelAsync(request);

        resultat.Navn.Should().Be("Maskin A");
        resultat.Anskaffelseskostnad.Should().Be(120_000m);
        resultat.Restverdi.Should().Be(0m);
        resultat.LevetidManeder.Should().Be(60);
        resultat.Avskrivningsgrunnlag.Should().Be(120_000m);
        resultat.ManedligAvskrivning.Should().Be(2_000m);
        resultat.ErAktivt.Should().BeTrue();
        _repo.Anleggsmidler.Should().HaveCount(1);
    }

    [Fact]
    public async Task OpprettAnleggsmiddel_MedNegativKostnad_KasterException()
    {
        var request = LagAnleggsmiddelRequest() with { Anskaffelseskostnad = -100m };

        var act = () => _service.OpprettAnleggsmiddelAsync(request);

        await act.Should().ThrowAsync<AvskrivningException>()
            .WithMessage("*storre enn 0*");
    }

    [Fact]
    public async Task OpprettAnleggsmiddel_MedRestverdistorreEnnKostnad_KasterException()
    {
        var request = LagAnleggsmiddelRequest() with { Restverdi = 200_000m };

        var act = () => _service.OpprettAnleggsmiddelAsync(request);

        await act.Should().ThrowAsync<AvskrivningException>()
            .WithMessage("*Restverdi*");
    }

    [Fact]
    public async Task OpprettAnleggsmiddel_MedNullLevetid_KasterException()
    {
        var request = LagAnleggsmiddelRequest() with { LevetidManeder = 0 };

        var act = () => _service.OpprettAnleggsmiddelAsync(request);

        await act.Should().ThrowAsync<AvskrivningException>()
            .WithMessage("*LevetidManeder*");
    }

    // --- BeregnAvskrivninger ---

    [Fact]
    public async Task BeregnAvskrivninger_MedEttAnleggsmiddel_BeregnerKorrekt()
    {
        _repo.Anleggsmidler.Add(LagAnleggsmiddel(120_000m, 0m, 60));

        var resultat = await _service.BeregnAvskrivningerAsync(2026, 1);

        resultat.Linjer.Should().HaveCount(1);
        resultat.Linjer[0].Belop.Should().Be(2_000m);
        resultat.TotalAvskrivning.Should().Be(2_000m);
        resultat.AntallAnleggsmidler.Should().Be(1);
    }

    [Fact]
    public async Task BeregnAvskrivninger_MedRestverdi_BeregnerKorrektGrunnlag()
    {
        // 100.000 - 10.000 restverdi = 90.000 grunnlag / 36 mnd = 2.500/mnd
        _repo.Anleggsmidler.Add(LagAnleggsmiddel(100_000m, 10_000m, 36));

        var resultat = await _service.BeregnAvskrivningerAsync(2026, 1);

        resultat.Linjer[0].Belop.Should().Be(2_500m);
    }

    [Fact]
    public async Task BeregnAvskrivninger_FulltAvskrevet_HopperOver()
    {
        var am = LagAnleggsmiddel(12_000m, 0m, 12);
        // Legg til 12 mnd avskrivninger = fullt avskrevet
        for (int i = 1; i <= 12; i++)
        {
            am.Avskrivninger.Add(new AvskrivningHistorikk
            {
                Id = Guid.NewGuid(), AnleggsmiddelId = am.Id,
                Ar = 2025, Periode = i, Belop = 1_000m,
                AkkumulertEtter = i * 1_000m,
                BokfortVerdiEtter = 12_000m - i * 1_000m,
                BilagId = Guid.NewGuid()
            });
        }
        _repo.Anleggsmidler.Add(am);

        var resultat = await _service.BeregnAvskrivningerAsync(2026, 1);

        resultat.Linjer.Should().BeEmpty();
        resultat.TotalAvskrivning.Should().Be(0m);
    }

    [Fact]
    public async Task BeregnAvskrivninger_SisteAvskrivning_BegrenserTilGjenvaerende()
    {
        // 10.000 grunnlag / 3 mnd = 3333.33/mnd. Etter 2 mnd: 6666.66, gjenstar: 3333.34
        var am = LagAnleggsmiddel(10_000m, 0m, 3);
        am.Avskrivninger.Add(new AvskrivningHistorikk
        {
            Id = Guid.NewGuid(), AnleggsmiddelId = am.Id,
            Ar = 2026, Periode = 1, Belop = 3_333.33m,
            AkkumulertEtter = 3_333.33m, BokfortVerdiEtter = 6_666.67m, BilagId = Guid.NewGuid()
        });
        am.Avskrivninger.Add(new AvskrivningHistorikk
        {
            Id = Guid.NewGuid(), AnleggsmiddelId = am.Id,
            Ar = 2026, Periode = 2, Belop = 3_333.33m,
            AkkumulertEtter = 6_666.66m, BokfortVerdiEtter = 3_333.34m, BilagId = Guid.NewGuid()
        });
        _repo.Anleggsmidler.Add(am);

        var resultat = await _service.BeregnAvskrivningerAsync(2026, 3);

        // ManedligAvskrivning = 3333.33, GjenvaerendeAvskrivning = 3333.34
        // Min(3333.33, 3333.34) = 3333.33, og gjenvaerende etter = 0.01 > 0 => ErSisteAvskrivning = false
        // Last real avskrivning will need a 4th period for the remaining 0.01
        resultat.Linjer[0].Belop.Should().Be(3_333.33m);
        resultat.Linjer[0].ErSisteAvskrivning.Should().BeFalse();
    }

    [Fact]
    public async Task BeregnAvskrivninger_IkkeAnskaffet_HopperOver()
    {
        var am = LagAnleggsmiddel(120_000m, 0m, 60);
        am.Anskaffelsesdato = new DateOnly(2026, 6, 15); // Anskaffet juni
        _repo.Anleggsmidler.Add(am);

        var resultat = await _service.BeregnAvskrivningerAsync(2026, 3); // Mars

        resultat.Linjer.Should().BeEmpty();
    }

    // --- BokforAvskrivninger ---

    [Fact]
    public async Task BokforAvskrivninger_OppretterBilagOgHistorikk()
    {
        _repo.Anleggsmidler.Add(LagAnleggsmiddel(120_000m, 0m, 60));

        var resultat = await _service.BokforAvskrivningerAsync(2026, 1);

        resultat.BilagId.Should().NotBeEmpty();
        resultat.TotalAvskrivning.Should().Be(2_000m);
        _repo.AvskrivningHistorikker.Should().HaveCount(1);
        _bilagService.SisteRequest.Should().NotBeNull();
        _bilagService.SisteRequest!.Type.Should().Be(BilagType.Avskrivning);
        _bilagService.SisteRequest.Posteringer.Should().HaveCount(2); // Debet + kredit
    }

    [Fact]
    public async Task BokforAvskrivninger_DuplikatPeriode_KasterException()
    {
        var am = LagAnleggsmiddel(120_000m, 0m, 60);
        _repo.Anleggsmidler.Add(am);
        _repo.AvskrivningHistorikker.Add(new AvskrivningHistorikk
        {
            Id = Guid.NewGuid(), AnleggsmiddelId = am.Id,
            Ar = 2026, Periode = 1, Belop = 2_000m,
            AkkumulertEtter = 2_000m, BokfortVerdiEtter = 118_000m, BilagId = Guid.NewGuid()
        });

        var act = () => _service.BokforAvskrivningerAsync(2026, 1);

        await act.Should().ThrowAsync<DuplikatAvskrivningException>();
    }

    [Fact]
    public async Task BokforAvskrivninger_IngenAktive_KasterException()
    {
        var act = () => _service.BokforAvskrivningerAsync(2026, 1);

        await act.Should().ThrowAsync<AvskrivningException>()
            .WithMessage("*Ingen avskrivninger*");
    }

    // --- UtrangerAnleggsmiddel ---

    [Fact]
    public async Task UtrangerAnleggsmiddel_SetterUtrangert()
    {
        var am = LagAnleggsmiddel(120_000m, 0m, 60);
        _repo.Anleggsmidler.Add(am);

        await _service.UtrangerAnleggsmiddelAsync(am.Id, new DateOnly(2026, 6, 30));

        am.ErAktivt.Should().BeFalse();
        am.UtrangeringsDato.Should().Be(new DateOnly(2026, 6, 30));
    }

    [Fact]
    public async Task UtrangerAnleggsmiddel_AlleredeUtrangert_KasterException()
    {
        var am = LagAnleggsmiddel(120_000m, 0m, 60);
        am.ErAktivt = false;
        am.UtrangeringsDato = new DateOnly(2025, 12, 31);
        _repo.Anleggsmidler.Add(am);

        var act = () => _service.UtrangerAnleggsmiddelAsync(am.Id, new DateOnly(2026, 6, 30));

        await act.Should().ThrowAsync<AvskrivningException>()
            .WithMessage("*allerede utrangert*");
    }

    // --- Helpers ---

    private static OpprettAnleggsmiddelRequest LagAnleggsmiddelRequest() => new(
        "Maskin A", "Produksjonsmaskin", new DateOnly(2025, 1, 1),
        120_000m, 0m, 60, "1200", "6000", "1209", null, null);

    private static Anleggsmiddel LagAnleggsmiddel(decimal kostnad, decimal restverdi, int levetidMnd) => new()
    {
        Id = Guid.NewGuid(),
        Navn = "Testmaskin",
        Anskaffelsesdato = new DateOnly(2025, 1, 1),
        Anskaffelseskostnad = kostnad,
        Restverdi = restverdi,
        LevetidManeder = levetidMnd,
        BalanseKontonummer = "1200",
        AvskrivningsKontonummer = "6000",
        AkkumulertAvskrivningKontonummer = "1209",
        ErAktivt = true,
        Avskrivninger = new()
    };
}
