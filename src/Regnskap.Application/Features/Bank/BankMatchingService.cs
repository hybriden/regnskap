namespace Regnskap.Application.Features.Bank;

using Regnskap.Application.Features.Bilagsregistrering;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Bankavstemming;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kundereskontro;
using Regnskap.Domain.Features.Leverandorreskontro;

/// <summary>
/// Service for automatisk og manuell matching av bankbevegelser.
/// Implementerer FR-B02, FR-B03, FR-B04, FR-B05 fra spesifikasjonen.
/// </summary>
public class BankMatchingService : IBankMatchingService
{
    private readonly IBankRepository _bankRepo;
    private readonly IKundeReskontroRepository _kundeRepo;
    private readonly ILeverandorReskontroRepository _leverandorRepo;
    private readonly IBilagRegistreringService _bilagService;

    public BankMatchingService(
        IBankRepository bankRepo,
        IKundeReskontroRepository kundeRepo,
        ILeverandorReskontroRepository leverandorRepo,
        IBilagRegistreringService bilagService)
    {
        _bankRepo = bankRepo;
        _kundeRepo = kundeRepo;
        _leverandorRepo = leverandorRepo;
        _bilagService = bilagService;
    }

    /// <summary>
    /// FR-B02: Automatisk matching paa alle umatchede bevegelser.
    /// </summary>
    public async Task<int> AutoMatch(Guid bankkontoId)
    {
        var umatchede = await _bankRepo.HentUmatchedeBevegelser(bankkontoId);
        var antallMatchet = 0;

        foreach (var bevegelse in umatchede)
        {
            var matchet = await ForsokAutoMatch(bevegelse);
            if (matchet) antallMatchet++;
        }

        await _bankRepo.LagreEndringerAsync();
        return antallMatchet;
    }

    private async Task<bool> ForsokAutoMatch(Bankbevegelse bevegelse)
    {
        // Prioritet 1: KID-matching (innbetalinger)
        if (bevegelse.Retning == BankbevegelseRetning.Inn && !string.IsNullOrWhiteSpace(bevegelse.KidNummer))
        {
            var faktura = await _kundeRepo.HentFakturaMedKidAsync(bevegelse.KidNummer);
            if (faktura != null && faktura.GjenstaendeBelop.Verdi > 0)
            {
                decimal konfidens;
                if (bevegelse.Belop.Verdi == faktura.GjenstaendeBelop.Verdi)
                    konfidens = 1.0m;
                else if (bevegelse.Belop.Verdi < faktura.GjenstaendeBelop.Verdi)
                    konfidens = 0.95m;
                else
                    return false; // Betalt mer enn gjenstaende - krever manuell haandtering

                var match = new BankbevegelseMatch
                {
                    Id = Guid.NewGuid(),
                    BankbevegelseId = bevegelse.Id,
                    Belop = bevegelse.Belop,
                    KundeFakturaId = faktura.Id,
                    MatcheType = MatcheType.Kid,
                    Beskrivelse = $"Auto-matchet mot kundefaktura {faktura.Fakturanummer} via KID {bevegelse.KidNummer}"
                };

                await _bankRepo.LeggTilMatch(match);
                bevegelse.Status = BankbevegelseStatus.AutoMatchet;
                bevegelse.MatcheType = MatcheType.Kid;
                bevegelse.MatcheKonfidens = konfidens;
                return true;
            }
        }

        // Prioritet 2: EndToEndId-matching (utbetalinger)
        if (!string.IsNullOrWhiteSpace(bevegelse.EndToEndId))
        {
            var betaling = await _leverandorRepo.HentBetalingMedBankreferanseAsync(bevegelse.EndToEndId);
            if (betaling != null)
            {
                var match = new BankbevegelseMatch
                {
                    Id = Guid.NewGuid(),
                    BankbevegelseId = bevegelse.Id,
                    Belop = bevegelse.Belop,
                    LeverandorFakturaId = betaling.LeverandorFakturaId,
                    MatcheType = MatcheType.Referanse,
                    Beskrivelse = $"Auto-matchet mot leverandorbetaling via EndToEndId {bevegelse.EndToEndId}"
                };

                await _bankRepo.LeggTilMatch(match);
                bevegelse.Status = BankbevegelseStatus.AutoMatchet;
                bevegelse.MatcheType = MatcheType.Referanse;
                bevegelse.MatcheKonfidens = 0.9m;
                return true;
            }
        }

        // Prioritet 3: Belop + dato-matching
        var belopMatch = await ForsokBelopDatoMatch(bevegelse);
        if (belopMatch) return true;

        return false;
    }

    private async Task<bool> ForsokBelopDatoMatch(Bankbevegelse bevegelse)
    {
        if (bevegelse.Retning == BankbevegelseRetning.Inn)
        {
            // Sok kundefakturaer
            var fakturaer = await _kundeRepo.HentApnePosterAsync();
            var treff = fakturaer.Where(f =>
                f.GjenstaendeBelop.Verdi == bevegelse.Belop.Verdi &&
                Math.Abs(f.Forfallsdato.DayNumber - bevegelse.Bokforingsdato.DayNumber) <= 5
            ).ToList();

            if (treff.Count == 1)
            {
                var faktura = treff[0];
                var match = new BankbevegelseMatch
                {
                    Id = Guid.NewGuid(),
                    BankbevegelseId = bevegelse.Id,
                    Belop = bevegelse.Belop,
                    KundeFakturaId = faktura.Id,
                    MatcheType = MatcheType.Belop,
                    Beskrivelse = $"Auto-matchet mot kundefaktura {faktura.Fakturanummer} via belop/dato"
                };

                await _bankRepo.LeggTilMatch(match);
                bevegelse.Status = BankbevegelseStatus.AutoMatchet;
                bevegelse.MatcheType = MatcheType.Belop;
                bevegelse.MatcheKonfidens = 0.7m;
                return true;
            }
        }
        else
        {
            // Sok leverandorfakturaer
            var fakturaer = await _leverandorRepo.HentApnePosterAsync();
            var treff = fakturaer.Where(f =>
                f.GjenstaendeBelop.Verdi == bevegelse.Belop.Verdi &&
                Math.Abs(f.Forfallsdato.DayNumber - bevegelse.Bokforingsdato.DayNumber) <= 5
            ).ToList();

            if (treff.Count == 1)
            {
                var faktura = treff[0];
                var match = new BankbevegelseMatch
                {
                    Id = Guid.NewGuid(),
                    BankbevegelseId = bevegelse.Id,
                    Belop = bevegelse.Belop,
                    LeverandorFakturaId = faktura.Id,
                    MatcheType = MatcheType.Belop,
                    Beskrivelse = $"Auto-matchet mot leverandorfaktura {faktura.InternNummer} via belop/dato"
                };

                await _bankRepo.LeggTilMatch(match);
                bevegelse.Status = BankbevegelseStatus.AutoMatchet;
                bevegelse.MatcheType = MatcheType.Belop;
                bevegelse.MatcheKonfidens = 0.7m;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Hent matchforslag for en enkelt bevegelse.
    /// </summary>
    public async Task<IReadOnlyList<MatcheForslagResponse>> HentForslag(Guid bankbevegelseId)
    {
        var bevegelse = await _bankRepo.HentBevegelse(bankbevegelseId)
            ?? throw new KeyNotFoundException($"Bankbevegelse {bankbevegelseId} ikke funnet.");

        var forslag = new List<MatcheForslagResponse>();

        if (bevegelse.Retning == BankbevegelseRetning.Inn)
        {
            // KID-match
            if (!string.IsNullOrWhiteSpace(bevegelse.KidNummer))
            {
                var kidFaktura = await _kundeRepo.HentFakturaMedKidAsync(bevegelse.KidNummer);
                if (kidFaktura != null && kidFaktura.GjenstaendeBelop.Verdi > 0)
                {
                    forslag.Add(new MatcheForslagResponse(
                        MatcheType.Kid, 1.0m,
                        $"KID-match: Faktura {kidFaktura.Fakturanummer}",
                        kidFaktura.Id, kidFaktura.Fakturanummer.ToString(), kidFaktura.GjenstaendeBelop.Verdi,
                        null, null, null, null, null));
                }
            }

            // Belop-match
            var apnePoster = await _kundeRepo.HentApnePosterAsync();
            foreach (var f in apnePoster.Where(f => f.GjenstaendeBelop.Verdi == bevegelse.Belop.Verdi))
            {
                forslag.Add(new MatcheForslagResponse(
                    MatcheType.Belop, 0.7m,
                    $"Belop-match: Faktura {f.Fakturanummer} ({f.GjenstaendeBelop.Verdi:N2})",
                    f.Id, f.Fakturanummer.ToString(), f.GjenstaendeBelop.Verdi,
                    null, null, null, null, null));
            }
        }
        else
        {
            var apnePoster = await _leverandorRepo.HentApnePosterAsync();
            foreach (var f in apnePoster.Where(f => f.GjenstaendeBelop.Verdi == bevegelse.Belop.Verdi))
            {
                forslag.Add(new MatcheForslagResponse(
                    MatcheType.Belop, 0.7m,
                    $"Belop-match: Leverandorfaktura {f.InternNummer} ({f.GjenstaendeBelop.Verdi:N2})",
                    null, null, null,
                    f.Id, f.InternNummer.ToString(), f.GjenstaendeBelop.Verdi,
                    null, null));
            }
        }

        return forslag.OrderByDescending(f => f.Konfidens).ToList();
    }

    /// <summary>
    /// FR-B03: Manuell matching av en bevegelse.
    /// </summary>
    public async Task Match(Guid bankbevegelseId, ManuellMatchRequest request)
    {
        var bevegelse = await _bankRepo.HentBevegelse(bankbevegelseId)
            ?? throw new KeyNotFoundException($"Bankbevegelse {bankbevegelseId} ikke funnet.");

        if (bevegelse.Status != BankbevegelseStatus.IkkeMatchet)
            throw new MatchAlleredeMatchetException();

        // Valider at noyaktig en av FK-ene er satt
        var antallSatt = (request.KundeFakturaId.HasValue ? 1 : 0)
            + (request.LeverandorFakturaId.HasValue ? 1 : 0)
            + (request.BilagId.HasValue ? 1 : 0);
        if (antallSatt != 1)
            throw new ArgumentException("Noyaktig en av KundeFakturaId, LeverandorFakturaId, BilagId maa vaere satt.");

        var match = new BankbevegelseMatch
        {
            Id = Guid.NewGuid(),
            BankbevegelseId = bankbevegelseId,
            Belop = bevegelse.Belop,
            KundeFakturaId = request.KundeFakturaId,
            LeverandorFakturaId = request.LeverandorFakturaId,
            BilagId = request.BilagId,
            MatcheType = MatcheType.Manuell,
            Beskrivelse = request.Beskrivelse
        };

        await _bankRepo.LeggTilMatch(match);
        bevegelse.Status = BankbevegelseStatus.ManueltMatchet;
        bevegelse.MatcheType = MatcheType.Manuell;
        bevegelse.MatcheKonfidens = 1.0m;

        await _bankRepo.LagreEndringerAsync();
    }

    /// <summary>
    /// FR-B04: Splitt-matching.
    /// </summary>
    public async Task Splitt(Guid bankbevegelseId, SplittMatchRequest request)
    {
        var bevegelse = await _bankRepo.HentBevegelse(bankbevegelseId)
            ?? throw new KeyNotFoundException($"Bankbevegelse {bankbevegelseId} ikke funnet.");

        if (bevegelse.Status != BankbevegelseStatus.IkkeMatchet)
            throw new MatchAlleredeMatchetException();

        // Valider sum
        var sum = request.Linjer.Sum(l => l.Belop);
        if (sum != bevegelse.Belop.Verdi)
            throw new SplittSumFeilException(sum, bevegelse.Belop.Verdi);

        foreach (var linje in request.Linjer)
        {
            var match = new BankbevegelseMatch
            {
                Id = Guid.NewGuid(),
                BankbevegelseId = bankbevegelseId,
                Belop = new Belop(linje.Belop),
                KundeFakturaId = linje.KundeFakturaId,
                LeverandorFakturaId = linje.LeverandorFakturaId,
                BilagId = linje.BilagId,
                MatcheType = MatcheType.Splitt,
                Beskrivelse = linje.Beskrivelse
            };
            await _bankRepo.LeggTilMatch(match);
        }

        bevegelse.Status = BankbevegelseStatus.Splittet;
        bevegelse.MatcheType = MatcheType.Splitt;
        bevegelse.MatcheKonfidens = 1.0m;

        await _bankRepo.LagreEndringerAsync();
    }

    /// <summary>
    /// Fjern matching og tilbakestill bevegelse til IkkeMatchet.
    /// </summary>
    public async Task FjernMatch(Guid bankbevegelseId)
    {
        var bevegelse = await _bankRepo.HentBevegelse(bankbevegelseId)
            ?? throw new KeyNotFoundException($"Bankbevegelse {bankbevegelseId} ikke funnet.");

        await _bankRepo.FjernMatchinger(bankbevegelseId);
        bevegelse.Status = BankbevegelseStatus.IkkeMatchet;
        bevegelse.MatcheType = null;
        bevegelse.MatcheKonfidens = null;

        await _bankRepo.LagreEndringerAsync();
    }

    /// <summary>
    /// FR-B05: Direkte bokforing av umatched bankbevegelse.
    /// Oppretter bilag med bank-konto og motkonto.
    /// </summary>
    public async Task<Guid> BokforDirekte(Guid bankbevegelseId, BokforDirekteRequest request)
    {
        var bevegelse = await _bankRepo.HentBevegelse(bankbevegelseId)
            ?? throw new KeyNotFoundException($"Bankbevegelse {bankbevegelseId} ikke funnet.");

        if (bevegelse.Status != BankbevegelseStatus.IkkeMatchet)
            throw new MatchAlleredeMatchetException();

        // Hent bankkontoens hovedbok-kontonummer (typisk 1920)
        var bankkonto = await _bankRepo.HentBankkonto(bevegelse.BankkontoId)
            ?? throw new KeyNotFoundException($"Bankkonto {bevegelse.BankkontoId} ikke funnet.");

        var bankKontonummer = bankkonto.Hovedbokkontonummer;

        // Bygg posteringer: Bank (1920) og motkonto
        var posteringer = new List<OpprettPosteringRequest>();

        if (bevegelse.Retning == BankbevegelseRetning.Inn)
        {
            // Innbetaling: Debet 1920 Bank, Kredit motkonto
            posteringer.Add(new OpprettPosteringRequest(
                bankKontonummer, BokforingSide.Debet, bevegelse.Belop.Verdi,
                request.Beskrivelse, null, request.Avdelingskode, request.Prosjektkode,
                KundeId: null, LeverandorId: null));

            posteringer.Add(new OpprettPosteringRequest(
                request.Motkontonummer, BokforingSide.Kredit, bevegelse.Belop.Verdi,
                request.Beskrivelse, request.MvaKode, request.Avdelingskode, request.Prosjektkode,
                KundeId: null, LeverandorId: null));
        }
        else
        {
            // Utbetaling: Kredit 1920 Bank, Debet motkonto
            posteringer.Add(new OpprettPosteringRequest(
                bankKontonummer, BokforingSide.Kredit, bevegelse.Belop.Verdi,
                request.Beskrivelse, null, request.Avdelingskode, request.Prosjektkode,
                KundeId: null, LeverandorId: null));

            posteringer.Add(new OpprettPosteringRequest(
                request.Motkontonummer, BokforingSide.Debet, bevegelse.Belop.Verdi,
                request.Beskrivelse, request.MvaKode, request.Avdelingskode, request.Prosjektkode,
                KundeId: null, LeverandorId: null));
        }

        var bilagRequest = new OpprettBilagRequest(
            BilagType.Bank,
            bevegelse.Bokforingsdato,
            request.Beskrivelse,
            bevegelse.BankReferanse,
            "BK", // Bank serie
            posteringer,
            BokforDirekte: true);

        var bilag = await _bilagService.OpprettOgBokforBilagAsync(bilagRequest);

        bevegelse.BilagId = bilag.Id;
        bevegelse.Status = BankbevegelseStatus.Bokfort;

        await _bankRepo.LagreEndringerAsync();

        return bilag.Id;
    }
}
