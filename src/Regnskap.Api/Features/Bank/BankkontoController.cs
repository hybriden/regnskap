using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Api.Features.Bank.Dtos;
using Regnskap.Application.Features.Bank;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Bankavstemming;

namespace Regnskap.Api.Features.Bank;

[ApiController]
[Route("api/bankkontoer")]
[Authorize]
public class BankkontoController : ControllerBase
{
    private readonly IBankRepository _repo;
    private readonly ICamt053ImportService _importService;
    private readonly IBankMatchingService _matchingService;
    private readonly IBankavstemmingService _avstemmingService;

    public BankkontoController(
        IBankRepository repo,
        ICamt053ImportService importService,
        IBankMatchingService matchingService,
        IBankavstemmingService avstemmingService)
    {
        _repo = repo;
        _importService = importService;
        _matchingService = matchingService;
        _avstemmingService = avstemmingService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BankkontoResponse>>> HentAlle([FromQuery] bool kunAktive = true)
    {
        var kontoer = await _repo.HentAlleBankkontoer(kunAktive);
        return Ok(kontoer.Select(MapBankkonto));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BankkontoResponse>> Hent(Guid id)
    {
        var konto = await _repo.HentBankkonto(id);
        if (konto == null) return NotFound();
        return Ok(MapBankkonto(konto));
    }

    [HttpPost]
    public async Task<ActionResult<BankkontoResponse>> Opprett([FromBody] OpprettBankkontoRequest request)
    {
        // Valider kontonummer (MOD11)
        if (!ValiderBankkontonummer(request.Kontonummer))
            throw new UgyldigBankkontonummerException(request.Kontonummer);

        // Sjekk duplikat
        var eksisterende = await _repo.HentBankkontoMedKontonummer(request.Kontonummer);
        if (eksisterende != null)
            throw new BankkontoFinnesException(request.Kontonummer);

        var bankkonto = new Bankkonto
        {
            Id = Guid.NewGuid(),
            Kontonummer = request.Kontonummer,
            Iban = request.Iban,
            Bic = request.Bic,
            Banknavn = request.Banknavn,
            Beskrivelse = request.Beskrivelse,
            Valutakode = request.Valutakode,
            HovedbokkkontoId = request.HovedbokkkontoId,
            Hovedbokkontonummer = "", // Set after save via lookup - TODO: resolve from Kontoplan
            ErStandardUtbetaling = request.ErStandardUtbetaling,
            ErStandardInnbetaling = request.ErStandardInnbetaling
        };

        await _repo.LeggTilBankkonto(bankkonto);
        await _repo.LagreEndringerAsync();

        // Reload to get navigation
        bankkonto = await _repo.HentBankkonto(bankkonto.Id);
        if (bankkonto?.Hovedbokkonto != null)
        {
            bankkonto.Hovedbokkontonummer = bankkonto.Hovedbokkonto.Kontonummer;
            await _repo.LagreEndringerAsync();
        }

        return CreatedAtAction(nameof(Hent), new { id = bankkonto!.Id }, MapBankkonto(bankkonto));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BankkontoResponse>> Oppdater(Guid id, [FromBody] OppdaterBankkontoRequest request)
    {
        var konto = await _repo.HentBankkonto(id);
        if (konto == null) return NotFound();

        konto.Banknavn = request.Banknavn;
        konto.Beskrivelse = request.Beskrivelse;
        konto.Iban = request.Iban;
        konto.Bic = request.Bic;
        konto.ErStandardUtbetaling = request.ErStandardUtbetaling;
        konto.ErStandardInnbetaling = request.ErStandardInnbetaling;

        await _repo.LagreEndringerAsync();
        return Ok(MapBankkonto(konto));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Deaktiver(Guid id)
    {
        var konto = await _repo.HentBankkonto(id);
        if (konto == null) return NotFound();
        konto.ErAktiv = false;
        await _repo.LagreEndringerAsync();
        return NoContent();
    }

    // --- Kontoutskrift ---

    [HttpPost("{id:guid}/import")]
    public async Task<ActionResult<ImportKontoutskriftResponse>> ImporterKontoutskrift(Guid id, IFormFile fil)
    {
        using var stream = fil.OpenReadStream();
        var resultat = await _importService.Importer(id, stream, fil.FileName);
        return Ok(new ImportKontoutskriftResponse(
            resultat.KontoutskriftId, resultat.MeldingsId,
            resultat.PeriodeFra, resultat.PeriodeTil,
            resultat.InngaendeSaldo, resultat.UtgaendeSaldo,
            resultat.AntallBevegelser, resultat.AntallAutoMatchet, resultat.AntallIkkeMatchet));
    }

    [HttpGet("{id:guid}/kontoutskrifter")]
    public async Task<ActionResult<IReadOnlyList<KontoutskriftResponse>>> HentKontoutskrifter(Guid id)
    {
        var utskrifter = await _repo.HentKontoutskrifter(id);
        return Ok(utskrifter.Select(k => new KontoutskriftResponse(
            k.Id, k.MeldingsId, k.PeriodeFra, k.PeriodeTil,
            k.InngaendeSaldo.Verdi, k.UtgaendeSaldo.Verdi,
            k.AntallBevegelser, k.Status)));
    }

    // --- Bevegelser ---

    [HttpGet("{id:guid}/bevegelser")]
    public async Task<ActionResult<IReadOnlyList<BankbevegelseResponse>>> HentBevegelser(
        Guid id,
        [FromQuery] BankbevegelseStatus? status = null,
        [FromQuery] DateOnly? fraDato = null,
        [FromQuery] DateOnly? tilDato = null)
    {
        var bevegelser = await _repo.HentBevegelser(id, status, fraDato, tilDato);
        return Ok(bevegelser.Select(MapBevegelse));
    }

    // --- Auto-match ---

    [HttpPost("{id:guid}/auto-match")]
    public async Task<ActionResult<object>> AutoMatch(Guid id)
    {
        var antall = await _matchingService.AutoMatch(id);
        return Ok(new { AntallMatchet = antall });
    }

    [HttpGet("{id:guid}/match-forslag")]
    public async Task<ActionResult<IReadOnlyList<MatcheForslagResponse>>> HentMatchForslag(Guid id)
    {
        // This endpoint lists suggestions for all unmatched for the account
        var umatchede = await _repo.HentUmatchedeBevegelser(id);
        var forslag = new List<object>();
        foreach (var bev in umatchede)
        {
            var f = await _matchingService.HentForslag(bev.Id);
            forslag.Add(new { BankbevegelseId = bev.Id, Forslag = f });
        }
        return Ok(forslag);
    }

    // --- Avstemming ---

    [HttpGet("{id:guid}/avstemming")]
    public async Task<ActionResult<AvstemmingResponse>> HentAvstemming(Guid id, [FromQuery] int aar, [FromQuery] int periode)
    {
        var avstemming = await _avstemmingService.HentEllerOpprett(id, aar, periode);
        return Ok(MapAvstemming(avstemming));
    }

    [HttpPost("{id:guid}/avstemming")]
    public async Task<ActionResult<AvstemmingResponse>> OpprettEllerOppdaterAvstemming(
        Guid id,
        [FromQuery] int aar,
        [FromQuery] int periode,
        [FromBody] OppdaterAvstemmingRequest request)
    {
        var avstemming = await _avstemmingService.HentEllerOpprett(id, aar, periode);
        avstemming = await _avstemmingService.Oppdater(avstemming.Id, request);
        return Ok(MapAvstemming(avstemming));
    }

    [HttpPost("{id:guid}/avstemming/godkjenn")]
    public async Task<ActionResult<AvstemmingResponse>> GodkjennAvstemming(Guid id, [FromQuery] int aar, [FromQuery] int periode)
    {
        var avstemming = await _avstemmingService.HentEllerOpprett(id, aar, periode);
        var bruker = User.Identity?.Name ?? "system";
        avstemming = await _avstemmingService.Godkjenn(avstemming.Id, bruker);
        return Ok(MapAvstemming(avstemming));
    }

    [HttpGet("{id:guid}/avstemming/rapport")]
    public async Task<ActionResult<AvstemmingsrapportResponse>> HentRapport(Guid id, [FromQuery] DateOnly dato)
    {
        var rapport = await _avstemmingService.GenererRapport(id, dato);
        return Ok(rapport);
    }

    // --- Hjelpemetoder ---

    private static BankkontoResponse MapBankkonto(Bankkonto b) => new(
        b.Id, b.Kontonummer, b.Iban, b.Bic, b.Banknavn, b.Beskrivelse,
        b.Valutakode, b.Hovedbokkontonummer, b.ErAktiv,
        b.ErStandardUtbetaling, b.ErStandardInnbetaling);

    private static BankbevegelseResponse MapBevegelse(Bankbevegelse b) => new(
        b.Id, b.Bokforingsdato, b.Valuteringsdato, b.Retning, b.Belop.Verdi,
        b.KidNummer, b.Motpart, b.Beskrivelse, b.Status, b.MatcheType, b.MatcheKonfidens,
        b.Matchinger.Select(m => new BankbevegelseMatchResponse(
            m.Id, m.Belop.Verdi, m.MatcheType, m.Beskrivelse,
            m.KundeFakturaId, m.LeverandorFakturaId, m.BilagId)).ToList());

    private static AvstemmingResponse MapAvstemming(Bankavstemming a) => new(
        a.Id, a.BankkontoId, a.Avstemmingsdato,
        a.SaldoHovedbok.Verdi, a.SaldoBank.Verdi, a.Differanse.Verdi,
        a.UtestaaendeBetalinger.Verdi, a.InnbetalingerITransitt.Verdi, a.AndreDifferanser.Verdi,
        a.UforklartDifferanse.Verdi, a.Status, a.GodkjentAv, a.GodkjentTidspunkt);

    /// <summary>
    /// FR-B07: Validering av norsk bankkontonummer med MOD11.
    /// </summary>
    public static bool ValiderBankkontonummer(string kontonummer)
    {
        var digits = kontonummer.Replace(".", "").Replace(" ", "");
        if (digits.Length != 11 || !digits.All(char.IsDigit))
            return false;

        int[] vekter = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
        var sum = 0;
        for (int i = 0; i < 10; i++)
            sum += (digits[i] - '0') * vekter[i];

        var rest = sum % 11;
        if (rest == 1) return false; // Ugyldig
        var kontrollsiffer = rest == 0 ? 0 : 11 - rest;

        return (digits[10] - '0') == kontrollsiffer;
    }
}
