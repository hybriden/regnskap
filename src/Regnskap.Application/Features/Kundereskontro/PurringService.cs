namespace Regnskap.Application.Features.Kundereskontro;

using Regnskap.Application.Features.Bilagsregistrering;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kundereskontro;

public class PurringService : IPurringService
{
    private readonly IKundeReskontroRepository _repo;
    private readonly IBilagRegistreringService _bilagService;

    /// <summary>
    /// Standard purregebyr (1/10 av inkassosatsen 2026, ca NOK 70).
    /// </summary>
    private const decimal StandardPurregebyr = 70m;

    public PurringService(IKundeReskontroRepository repo, IBilagRegistreringService bilagService)
    {
        _repo = repo;
        _bilagService = bilagService;
    }

    public async Task<List<PurreforslagDto>> GenererForslagAsync(PurreforslagRequest request, CancellationToken ct = default)
    {
        var forfalteFakturaer = await _repo.HentForfalteFakturaerAsync(request.Dato, request.MinimumDagerForfalt, ct);
        var forslag = new List<PurreforslagDto>();

        foreach (var faktura in forfalteFakturaer)
        {
            // Filtrer ut betalte, krediterte, tap
            if (faktura.Status == KundeFakturaStatus.Betalt ||
                faktura.Status == KundeFakturaStatus.Kreditert ||
                faktura.Status == KundeFakturaStatus.Tap ||
                faktura.Status == KundeFakturaStatus.Inkasso)
                continue;

            // Filtrer ut sperrede kunder
            if (faktura.Kunde.ErSperret)
                continue;

            // Filtrer pa kunde-ID
            if (request.KundeIder != null && request.KundeIder.Count > 0 && !request.KundeIder.Contains(faktura.KundeId))
                continue;

            var sistePurring = await _repo.HentSistePurringAsync(faktura.Id, ct);
            PurringType? foreslattType = null;
            decimal gebyr = 0;

            if (faktura.AntallPurringer == 0 && request.InkluderPurring1)
            {
                foreslattType = PurringType.Purring1;
                gebyr = 0; // Forste paaminnelse er gebyrfri
            }
            else if (faktura.AntallPurringer == 1 && request.InkluderPurring2)
            {
                // Minimum 14 dager mellom purringer
                if (sistePurring != null && request.Dato.DayNumber - sistePurring.Purringsdato.DayNumber < 14)
                    continue;
                foreslattType = PurringType.Purring2;
                gebyr = StandardPurregebyr;
            }
            else if (faktura.AntallPurringer == 2 && request.InkluderPurring3)
            {
                if (sistePurring != null && request.Dato.DayNumber - sistePurring.Purringsdato.DayNumber < 14)
                    continue;
                foreslattType = PurringType.Purring3Inkassovarsel;
                gebyr = StandardPurregebyr;
            }

            if (foreslattType == null)
                continue;

            forslag.Add(new PurreforslagDto(
                faktura.Id,
                faktura.Fakturanummer,
                faktura.KundeId,
                faktura.Kunde.Navn,
                faktura.GjenstaendeBelop.Verdi,
                faktura.Forfallsdato,
                faktura.DagerForfalt(request.Dato),
                foreslattType.Value,
                gebyr));
        }

        return forslag;
    }

    public async Task<List<PurringDto>> OpprettPurringerAsync(List<Guid> fakturaIder, PurringType type, CancellationToken ct = default)
    {
        var opprettet = new List<PurringDto>();
        var iDag = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var fakturaId in fakturaIder)
        {
            var faktura = await _repo.HentFakturaAsync(fakturaId, ct);
            if (faktura == null) continue;

            // Minimum 14 dager mellom purringer
            var sistePurring = await _repo.HentSistePurringAsync(fakturaId, ct);
            if (sistePurring != null && iDag.DayNumber - sistePurring.Purringsdato.DayNumber < 14)
                throw new PurringValideringException($"Minimum 14 dager mellom purringer. Siste purring: {sistePurring.Purringsdato}.");

            var gebyr = type == PurringType.Purring1 ? 0m : StandardPurregebyr;
            var nyForfallsdato = KundeFakturaService.FlyttFraHelg(iDag.AddDays(14));

            var purring = new Purring
            {
                Id = Guid.NewGuid(),
                KundeFakturaId = fakturaId,
                Type = type,
                Purringsdato = iDag,
                NyForfallsdato = nyForfallsdato,
                Gebyr = new Belop(gebyr),
            };

            // Oppdater faktura
            faktura.AntallPurringer++;
            faktura.SistePurringDato = iDag;

            if (gebyr > 0)
            {
                faktura.GjenstaendeBelop = faktura.GjenstaendeBelop + new Belop(gebyr);
                faktura.PurregebyrTotalt = faktura.PurregebyrTotalt + new Belop(gebyr);

                // FR-K07: Opprett bilag for purregebyr (Debet 1500 Kundefordringer, Kredit 3900 Andre inntekter)
                var gebyrPosteringer = new List<OpprettPosteringRequest>
                {
                    new("1500", BokforingSide.Debet, gebyr,
                        $"Purregebyr faktura #{faktura.Fakturanummer}",
                        null, null, null, KundeId: faktura.KundeId, LeverandorId: null),
                    new("3900", BokforingSide.Kredit, gebyr,
                        $"Purregebyr faktura #{faktura.Fakturanummer}",
                        null, null, null, KundeId: faktura.KundeId, LeverandorId: null)
                };

                var gebyrBilagRequest = new OpprettBilagRequest(
                    BilagType.UtgaendeFaktura,
                    iDag,
                    $"Purregebyr {faktura.Kunde.Kundenummer} faktura #{faktura.Fakturanummer}",
                    null,
                    "UF",
                    gebyrPosteringer,
                    BokforDirekte: true);

                var gebyrBilag = await _bilagService.OpprettOgBokforBilagAsync(gebyrBilagRequest, ct);
                purring.GebyrBilagId = gebyrBilag.Id;
            }

            // Oppdater status
            faktura.Status = type switch
            {
                PurringType.Purring1 => KundeFakturaStatus.Purring1,
                PurringType.Purring2 => KundeFakturaStatus.Purring2,
                PurringType.Purring3Inkassovarsel => KundeFakturaStatus.Purring3,
                _ => faktura.Status
            };

            await _repo.LeggTilPurringAsync(purring, ct);
            await _repo.OppdaterFakturaAsync(faktura, ct);

            opprettet.Add(new PurringDto(
                purring.Id,
                purring.KundeFakturaId,
                faktura.Fakturanummer,
                faktura.Kunde.Navn,
                purring.Type,
                purring.Purringsdato,
                purring.NyForfallsdato,
                purring.Gebyr.Verdi,
                purring.Forsinkelsesrente.Verdi,
                purring.ErSendt));
        }

        await _repo.LagreEndringerAsync(ct);
        return opprettet;
    }

    public async Task MarkerSendtAsync(Guid purringId, string sendemetode, CancellationToken ct = default)
    {
        // TODO: Hent purring direkte - for na gaar vi via faktura
        // Forenklet implementasjon
        throw new NotImplementedException("MarkerSendt krever direkte purring-henting fra repository.");
    }

    public async Task<List<PurringDto>> HentPurringerAsync(int side, int antall, CancellationToken ct = default)
    {
        var purringer = await _repo.HentPurringerAsync(side, antall, ct);
        return purringer.Select(p => new PurringDto(
            p.Id,
            p.KundeFakturaId,
            p.KundeFaktura?.Fakturanummer ?? 0,
            p.KundeFaktura?.Kunde?.Navn ?? "",
            p.Type,
            p.Purringsdato,
            p.NyForfallsdato,
            p.Gebyr.Verdi,
            p.Forsinkelsesrente.Verdi,
            p.ErSendt)).ToList();
    }
}
