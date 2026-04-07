using Regnskap.Application.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Periodeavslutning;

namespace Regnskap.Application.Features.Periodeavslutning;

public class AvskrivningService : IAvskrivningService
{
    private readonly IPeriodeavslutningRepository _repo;
    private readonly IBilagRegistreringService _bilagService;

    public AvskrivningService(
        IPeriodeavslutningRepository repo,
        IBilagRegistreringService bilagService)
    {
        _repo = repo;
        _bilagService = bilagService;
    }

    public async Task<AnleggsmiddelDto> OpprettAnleggsmiddelAsync(
        OpprettAnleggsmiddelRequest request, CancellationToken ct = default)
    {
        if (request.Anskaffelseskostnad <= 0)
            throw new AvskrivningException("Anskaffelseskostnad ma vaere storre enn 0.");
        if (request.Restverdi < 0 || request.Restverdi >= request.Anskaffelseskostnad)
            throw new AvskrivningException("Restverdi ma vaere >= 0 og < anskaffelseskostnad.");
        if (request.LevetidManeder < 1)
            throw new AvskrivningException("LevetidManeder ma vaere minst 1.");

        var anleggsmiddel = new Anleggsmiddel
        {
            Id = Guid.NewGuid(),
            Navn = request.Navn,
            Beskrivelse = request.Beskrivelse,
            Anskaffelsesdato = request.Anskaffelsesdato,
            Anskaffelseskostnad = request.Anskaffelseskostnad,
            Restverdi = request.Restverdi,
            LevetidManeder = request.LevetidManeder,
            BalanseKontonummer = request.BalanseKontonummer,
            AvskrivningsKontonummer = request.AvskrivningsKontonummer,
            AkkumulertAvskrivningKontonummer = request.AkkumulertAvskrivningKontonummer,
            Avdelingskode = request.Avdelingskode,
            Prosjektkode = request.Prosjektkode,
            ErAktivt = true
        };

        await _repo.LeggTilAnleggsmiddelAsync(anleggsmiddel, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapToDto(anleggsmiddel);
    }

    public async Task<List<AnleggsmiddelDto>> HentAnleggsmidlerAsync(
        bool? aktive = true, string? kontonummer = null, CancellationToken ct = default)
    {
        var anleggsmidler = await _repo.HentAnleggsmidlerAsync(aktive, kontonummer, ct);
        return anleggsmidler.Select(MapToDto).ToList();
    }

    public async Task<AnleggsmiddelDto> HentAnleggsmiddelAsync(
        Guid id, CancellationToken ct = default)
    {
        var anleggsmiddel = await _repo.HentAnleggsmiddelAsync(id, ct)
            ?? throw new AnleggsmiddelIkkeFunnetException(id);
        return MapToDto(anleggsmiddel);
    }

    public async Task UtrangerAnleggsmiddelAsync(
        Guid id, DateOnly utrangeringsDato, CancellationToken ct = default)
    {
        var anleggsmiddel = await _repo.HentAnleggsmiddelAsync(id, ct)
            ?? throw new AnleggsmiddelIkkeFunnetException(id);

        if (!anleggsmiddel.ErAktivt)
            throw new AvskrivningException($"Anleggsmiddel '{anleggsmiddel.Navn}' er allerede utrangert.");

        anleggsmiddel.ErAktivt = false;
        anleggsmiddel.UtrangeringsDato = utrangeringsDato;

        await _repo.LagreEndringerAsync(ct);
    }

    public async Task<AvskrivningBeregningDto> BeregnAvskrivningerAsync(
        int ar, int periode, CancellationToken ct = default)
    {
        var anleggsmidler = await _repo.HentAnleggsmidlerAsync(aktive: true, ct: ct);
        var linjer = new List<AvskrivningLinjeDto>();

        foreach (var am in anleggsmidler)
        {
            if (am.ErFulltAvskrevet) continue;

            // Sjekk at anleggsmiddelet var anskaffet for eller i denne perioden
            var periodeSlutt = new DateOnly(ar, periode, DateTime.DaysInMonth(ar, periode));
            if (am.Anskaffelsesdato > periodeSlutt) continue;

            var belop = Math.Min(am.ManedligAvskrivning, am.GjenvaerendeAvskrivning);
            if (belop <= 0) continue;

            var akkumulertFor = am.AkkumulertAvskrivning;
            var akkumulertEtter = akkumulertFor + belop;
            var bokfortVerdiEtter = am.Anskaffelseskostnad - akkumulertEtter;
            var erSiste = am.GjenvaerendeAvskrivning - belop <= 0;

            linjer.Add(new AvskrivningLinjeDto(
                am.Id,
                am.Navn,
                am.BalanseKontonummer,
                am.AvskrivningsKontonummer,
                am.AkkumulertAvskrivningKontonummer,
                belop,
                akkumulertFor,
                akkumulertEtter,
                bokfortVerdiEtter,
                erSiste));
        }

        return new AvskrivningBeregningDto(
            ar,
            periode,
            linjer,
            linjer.Sum(l => l.Belop),
            linjer.Count);
    }

    public async Task<AvskrivningBokforingDto> BokforAvskrivningerAsync(
        int ar, int periode, CancellationToken ct = default)
    {
        var beregning = await BeregnAvskrivningerAsync(ar, periode, ct);

        if (beregning.Linjer.Count == 0)
            throw new AvskrivningException($"Ingen avskrivninger a bokfore for {ar}-{periode:D2}.");

        // Sjekk for duplikater
        foreach (var linje in beregning.Linjer)
        {
            if (await _repo.AvskrivningFinnesAsync(linje.AnleggsmiddelId, ar, periode, ct))
                throw new DuplikatAvskrivningException(linje.AnleggsmiddelId, ar, periode);
        }

        // Opprett bilag via IBilagRegistreringService
        var posteringer = new List<OpprettPosteringRequest>();
        foreach (var linje in beregning.Linjer)
        {
            posteringer.Add(new OpprettPosteringRequest(
                linje.AvskrivningsKontonummer,
                BokforingSide.Debet,
                linje.Belop,
                $"Avskrivning {linje.Navn}",
                null, null, null, null, null));

            posteringer.Add(new OpprettPosteringRequest(
                linje.AkkumulertAvskrivningKontonummer,
                BokforingSide.Kredit,
                linje.Belop,
                $"Akk. avskrivning {linje.Navn}",
                null, null, null, null, null));
        }

        var siste = new DateOnly(ar, periode, DateTime.DaysInMonth(ar, periode));
        var bilagRequest = new OpprettBilagRequest(
            BilagType.Avskrivning,
            siste,
            $"Avskrivninger periode {ar}-{periode:D2}",
            null,
            "AVS",
            posteringer,
            true);

        var bilagDto = await _bilagService.OpprettOgBokforBilagAsync(bilagRequest, ct);

        // Lagre historikk per anleggsmiddel
        foreach (var linje in beregning.Linjer)
        {
            var historikk = new AvskrivningHistorikk
            {
                Id = Guid.NewGuid(),
                AnleggsmiddelId = linje.AnleggsmiddelId,
                Ar = ar,
                Periode = periode,
                Belop = linje.Belop,
                AkkumulertEtter = linje.AkkumulertEtter,
                BokfortVerdiEtter = linje.BokfortVerdiEtter,
                BilagId = bilagDto.Id
            };
            await _repo.LeggTilAvskrivningHistorikkAsync(historikk, ct);
        }

        await _repo.LagreEndringerAsync(ct);

        return new AvskrivningBokforingDto(
            ar,
            periode,
            beregning.Linjer,
            beregning.TotalAvskrivning,
            bilagDto.Id);
    }

    internal static AnleggsmiddelDto MapToDto(Anleggsmiddel am) => new(
        am.Id,
        am.Navn,
        am.Beskrivelse,
        am.Anskaffelsesdato,
        am.Anskaffelseskostnad,
        am.Restverdi,
        am.LevetidManeder,
        am.BalanseKontonummer,
        am.AvskrivningsKontonummer,
        am.AkkumulertAvskrivningKontonummer,
        am.Avdelingskode,
        am.Prosjektkode,
        am.ErAktivt,
        am.UtrangeringsDato,
        am.Avskrivningsgrunnlag,
        am.ManedligAvskrivning,
        am.AkkumulertAvskrivning,
        am.BokfortVerdi,
        am.GjenvaerendeAvskrivning,
        am.ErFulltAvskrevet,
        am.Avskrivninger.Select(a => new AvskrivningHistorikkDto(
            a.Id, a.Ar, a.Periode, a.Belop, a.AkkumulertEtter, a.BokfortVerdiEtter, a.BilagId
        )).ToList());
}
