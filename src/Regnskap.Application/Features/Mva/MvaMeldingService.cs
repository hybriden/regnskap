namespace Regnskap.Application.Features.Mva;

using Regnskap.Domain.Features.Mva;

public class MvaMeldingService : IMvaMeldingService
{
    private readonly IMvaRepository _repo;

    public MvaMeldingService(IMvaRepository repo)
    {
        _repo = repo;
    }

    public async Task<MvaMeldingDto> GenererMvaMeldingAsync(Guid terminId, CancellationToken ct = default)
    {
        var termin = await _repo.HentTerminAsync(terminId, ct)
            ?? throw new MvaTerminIkkeFunnetException(terminId);

        var oppgjor = await _repo.HentOppgjorForTerminAsync(terminId, ct);

        // Bygg RF-0002 poster
        var poster = GenererRf0002Poster(oppgjor);

        decimal sumUtgaende = poster
            .Where(p => p.Postnummer >= 1 && p.Postnummer <= 6)
            .Sum(p => p.MvaBelop);

        decimal sumInngaende = poster
            .Where(p => p.Postnummer >= 7 && p.Postnummer <= 12)
            .Sum(p => p.MvaBelop);

        decimal grunnlagHoy = poster
            .Where(p => p.Postnummer == 1)
            .Sum(p => p.Grunnlag);

        decimal grunnlagMiddels = poster
            .Where(p => p.Postnummer == 2)
            .Sum(p => p.Grunnlag);

        decimal grunnlagLav = poster
            .Where(p => p.Postnummer == 3)
            .Sum(p => p.Grunnlag);

        decimal mvaTilBetaling = sumUtgaende - sumInngaende;

        return new MvaMeldingDto(
            termin.Id, termin.Terminnavn,
            termin.Ar, termin.Termin,
            termin.FraDato, termin.TilDato,
            poster,
            sumUtgaende, sumInngaende,
            grunnlagHoy, grunnlagMiddels, grunnlagLav,
            mvaTilBetaling
        );
    }

    public async Task MarkerInnsendtAsync(Guid terminId, CancellationToken ct = default)
    {
        var termin = await _repo.HentTerminAsync(terminId, ct)
            ?? throw new MvaTerminIkkeFunnetException(terminId);

        if (termin.Status != MvaTerminStatus.Avstemt)
            throw new MvaAvstemmingIkkeGodkjentException(terminId);

        var oppgjor = await _repo.HentOppgjorForTerminAsync(terminId, ct)
            ?? throw new MvaOppgjorManglerException(terminId);

        oppgjor.ErLast = true;

        termin.Status = MvaTerminStatus.Innsendt;
        termin.AvsluttetTidspunkt = DateTime.UtcNow;
        termin.AvsluttetAv = "system"; // TODO: Hent fra HTTP-kontekst

        await _repo.LagreEndringerAsync(ct);
    }

    private static List<MvaMeldingPostDto> GenererRf0002Poster(MvaOppgjor? oppgjor)
    {
        var postDefinisjoner = new (int Nr, string Beskrivelse, string[] TaxCodes)[]
        {
            (1, "Utgaende MVA, alminnelig sats (25%)", new[] { "3" }),
            (2, "Utgaende MVA, middels sats (15%)", new[] { "31" }),
            (3, "Utgaende MVA, lav sats (12%)", new[] { "33" }),
            (4, "Innforsel av varer, alminnelig sats (25%)", new[] { "51" }),
            (5, "Innforsel av varer, middels sats (15%)", new[] { "52" }),
            (6, "Tjenester kjopt fra utlandet (snudd avregning, 25%)", new[] { "82" }),
            (7, "Inngaende MVA, alminnelig sats (25%)", new[] { "1" }),
            (8, "Inngaende MVA, middels sats (15%)", new[] { "11" }),
            (9, "Inngaende MVA, lav sats (12%)", new[] { "13" }),
            (10, "Innforsel av varer, inngaende MVA (25%)", new[] { "14" }),
            (11, "Innforsel av varer, inngaende MVA (15%)", new[] { "15" }),
            (12, "Tjenester fra utlandet, inngaende MVA (snudd avregning, 25%)", new[] { "81" }),
        };

        var poster = new List<MvaMeldingPostDto>();

        foreach (var (nr, beskrivelse, taxCodes) in postDefinisjoner)
        {
            decimal grunnlag = 0;
            decimal mvaBelop = 0;

            if (oppgjor != null)
            {
                var matchendeLinjer = oppgjor.Linjer
                    .Where(l => taxCodes.Contains(l.StandardTaxCode))
                    .ToList();

                grunnlag = matchendeLinjer.Sum(l => l.SumGrunnlag);
                mvaBelop = matchendeLinjer.Sum(l => l.SumMvaBelop);
            }

            // RF-0002: belop rundes til hele kroner (FR-MVA-09)
            grunnlag = Math.Round(grunnlag, 0, MidpointRounding.ToEven);
            mvaBelop = Math.Round(mvaBelop, 0, MidpointRounding.ToEven);

            // Poster med 0 i bade grunnlag og MVA inkluderes ikke (FR-MVA-03 regel 1)
            // Men vi inkluderer alle for null-melding-stotte
            poster.Add(new MvaMeldingPostDto(
                nr, beskrivelse,
                grunnlag, mvaBelop,
                taxCodes.ToList()
            ));
        }

        return poster;
    }
}
