using Regnskap.Application.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Periodeavslutning;

namespace Regnskap.Application.Features.Periodeavslutning;

public class PeriodiseringsService : IPeriodiseringsService
{
    private readonly IPeriodeavslutningRepository _repo;
    private readonly IBilagRegistreringService _bilagService;

    public PeriodiseringsService(
        IPeriodeavslutningRepository repo,
        IBilagRegistreringService bilagService)
    {
        _repo = repo;
        _bilagService = bilagService;
    }

    public async Task<PeriodiseringDto> OpprettPeriodiseringAsync(
        OpprettPeriodiseringRequest request, CancellationToken ct = default)
    {
        if (request.TotalBelop <= 0)
            throw new PeriodiseringsException("TotalBelop ma vaere storre enn 0.");
        if (request.FraPeriode < 1 || request.FraPeriode > 12)
            throw new PeriodiseringsException("FraPeriode ma vaere mellom 1 og 12.");
        if (request.TilPeriode < 1 || request.TilPeriode > 12)
            throw new PeriodiseringsException("TilPeriode ma vaere mellom 1 og 12.");
        if (request.FraAr * 12 + request.FraPeriode > request.TilAr * 12 + request.TilPeriode)
            throw new PeriodiseringsException("Fra-periode ma vaere for eller lik til-periode.");

        var periodisering = new Periodisering
        {
            Id = Guid.NewGuid(),
            Beskrivelse = request.Beskrivelse,
            Type = request.Type,
            TotalBelop = request.TotalBelop,
            FraAr = request.FraAr,
            FraPeriode = request.FraPeriode,
            TilAr = request.TilAr,
            TilPeriode = request.TilPeriode,
            BalanseKontonummer = request.BalanseKontonummer,
            ResultatKontonummer = request.ResultatKontonummer,
            Avdelingskode = request.Avdelingskode,
            Prosjektkode = request.Prosjektkode,
            OpprinneligBilagId = request.OpprinneligBilagId,
            ErAktiv = true
        };

        await _repo.LeggTilPeriodiseringAsync(periodisering, ct);
        await _repo.LagreEndringerAsync(ct);

        return MapToDto(periodisering);
    }

    public async Task<List<PeriodiseringDto>> HentPeriodiseringerAsync(
        bool? aktive = true, CancellationToken ct = default)
    {
        var periodiseringer = await _repo.HentPeriodiseringerAsync(aktive, ct);
        return periodiseringer.Select(MapToDto).ToList();
    }

    public async Task<PeriodiseringBokforingDto> BokforPeriodiseringerAsync(
        int ar, int periode, CancellationToken ct = default)
    {
        var periodiseringer = await _repo.HentAktivePeriodiseringerForPeriodeAsync(ar, periode, ct);

        if (periodiseringer.Count == 0)
            throw new PeriodiseringsException($"Ingen aktive periodiseringer for {ar}-{periode:D2}.");

        var posteringer = new List<OpprettPosteringRequest>();
        var linjer = new List<PeriodiseringBokforingLinjeDto>();

        foreach (var p in periodiseringer)
        {
            // Sjekk duplikat
            if (await _repo.PeriodiseringsHistorikkFinnesAsync(p.Id, ar, periode, ct))
                throw new DuplikatPeriodiseringException(p.Id, ar, periode);

            var belop = BeregnBelopForPeriode(p, ar, periode);
            if (belop <= 0) continue;

            var gjenstaarEtter = p.GjenstaendeBelop - belop;

            // Opprett posteringer basert pa type
            var (debetKonto, kreditKonto) = p.Type switch
            {
                PeriodiseringsType.ForskuddsbetaltKostnad => (p.ResultatKontonummer, p.BalanseKontonummer),
                PeriodiseringsType.PaloptKostnad => (p.ResultatKontonummer, p.BalanseKontonummer),
                PeriodiseringsType.ForskuddsbetaltInntekt => (p.BalanseKontonummer, p.ResultatKontonummer),
                PeriodiseringsType.OpptjentInntekt => (p.BalanseKontonummer, p.ResultatKontonummer),
                _ => throw new PeriodiseringsException($"Ukjent periodiseringstype: {p.Type}")
            };

            posteringer.Add(new OpprettPosteringRequest(
                debetKonto,
                BokforingSide.Debet,
                belop,
                $"Periodisering: {p.Beskrivelse}",
                null, p.Avdelingskode, p.Prosjektkode, null, null));

            posteringer.Add(new OpprettPosteringRequest(
                kreditKonto,
                BokforingSide.Kredit,
                belop,
                $"Periodisering: {p.Beskrivelse}",
                null, p.Avdelingskode, p.Prosjektkode, null, null));

            linjer.Add(new PeriodiseringBokforingLinjeDto(
                p.Id,
                p.Beskrivelse,
                p.Type,
                belop,
                gjenstaarEtter));
        }

        if (posteringer.Count == 0)
            throw new PeriodiseringsException($"Ingen periodiseringer a bokfore for {ar}-{periode:D2}.");

        var siste = new DateOnly(ar, periode, DateTime.DaysInMonth(ar, periode));
        var bilagRequest = new OpprettBilagRequest(
            BilagType.Periodisering,
            siste,
            $"Periodiseringer periode {ar}-{periode:D2}",
            null,
            "PER",
            posteringer,
            true);

        var bilagDto = await _bilagService.OpprettOgBokforBilagAsync(bilagRequest, ct);

        // Lagre historikk
        foreach (var linje in linjer)
        {
            var p = periodiseringer.First(x => x.Id == linje.PeriodiseringId);
            var historikk = new PeriodiseringsHistorikk
            {
                Id = Guid.NewGuid(),
                PeriodiseringId = p.Id,
                Ar = ar,
                Periode = periode,
                Belop = linje.Belop,
                AkkumulertEtter = p.SumPeriodisert + linje.Belop,
                GjenstaarEtter = linje.GjenstaarEtter,
                BilagId = bilagDto.Id
            };
            await _repo.LeggTilPeriodiseringsHistorikkAsync(historikk, ct);

            // Deaktiver hvis ferdig periodisert
            if (linje.GjenstaarEtter <= 0)
                p.ErAktiv = false;
        }

        await _repo.LagreEndringerAsync(ct);

        return new PeriodiseringBokforingDto(
            ar,
            periode,
            linjer,
            linjer.Sum(l => l.Belop),
            bilagDto.Id);
    }

    public async Task DeaktiverPeriodiseringAsync(
        Guid id, CancellationToken ct = default)
    {
        var periodisering = await _repo.HentPeriodiseringAsync(id, ct)
            ?? throw new PeriodiseringIkkeFunnetException(id);

        periodisering.ErAktiv = false;
        await _repo.LagreEndringerAsync(ct);
    }

    /// <summary>
    /// Beregner belop for en gitt periode. For siste periode brukes gjenstaende for a unnga avrundingsdifferanser.
    /// </summary>
    internal static decimal BeregnBelopForPeriode(Periodisering p, int ar, int periode)
    {
        var erSistePeriode = ar == p.TilAr && periode == p.TilPeriode;
        if (erSistePeriode)
            return p.GjenstaendeBelop;

        return Math.Min(p.BelopPerPeriode, p.GjenstaendeBelop);
    }

    internal static PeriodiseringDto MapToDto(Periodisering p) => new(
        p.Id,
        p.Beskrivelse,
        p.Type,
        p.TotalBelop,
        p.FraAr,
        p.FraPeriode,
        p.TilAr,
        p.TilPeriode,
        p.BalanseKontonummer,
        p.ResultatKontonummer,
        p.ErAktiv,
        p.AntallPerioder,
        p.BelopPerPeriode,
        p.SumPeriodisert,
        p.GjenstaendeBelop,
        p.Posteringer.Select(h => new PeriodiseringsHistorikkDto(
            h.Id, h.Ar, h.Periode, h.Belop, h.AkkumulertEtter, h.GjenstaarEtter, h.BilagId
        )).ToList());
}
