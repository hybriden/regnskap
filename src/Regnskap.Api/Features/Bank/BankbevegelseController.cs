using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Api.Features.Bank.Dtos;
using Regnskap.Application.Features.Bank;
using Regnskap.Domain.Features.Bankavstemming;
using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Api.Features.Bank;

[ApiController]
[Route("api")]
[Authorize]
public class BankbevegelseController : ControllerBase
{
    private readonly IBankRepository _repo;
    private readonly IBankMatchingService _matchingService;
    private readonly IKontoplanRepository _kontoplanRepo;

    public BankbevegelseController(
        IBankRepository repo,
        IBankMatchingService matchingService,
        IKontoplanRepository kontoplanRepo)
    {
        _repo = repo;
        _matchingService = matchingService;
        _kontoplanRepo = kontoplanRepo;
    }

    [HttpGet("bankbevegelser/{id:guid}")]
    public async Task<ActionResult<BankbevegelseResponse>> Hent(Guid id)
    {
        var bevegelse = await _repo.HentBevegelse(id);
        if (bevegelse == null) return NotFound();

        return Ok(new BankbevegelseResponse(
            bevegelse.Id, bevegelse.Bokforingsdato, bevegelse.Valuteringsdato,
            bevegelse.Retning, bevegelse.Belop.Verdi, bevegelse.KidNummer,
            bevegelse.Motpart, bevegelse.Beskrivelse, bevegelse.Status,
            bevegelse.MatcheType, bevegelse.MatcheKonfidens,
            bevegelse.Matchinger.Select(m => new BankbevegelseMatchResponse(
                m.Id, m.Belop.Verdi, m.MatcheType, m.Beskrivelse,
                m.KundeFakturaId, m.LeverandorFakturaId, m.BilagId)).ToList()));
    }

    [HttpPost("bankbevegelser/{id:guid}/match")]
    public async Task<ActionResult> ManuellMatch(Guid id, [FromBody] ManuellMatchRequest request)
    {
        await _matchingService.Match(id, request);
        return Ok();
    }

    [HttpPost("bankbevegelser/{id:guid}/splitt")]
    public async Task<ActionResult> Splitt(Guid id, [FromBody] SplittMatchRequest request)
    {
        await _matchingService.Splitt(id, request);
        return Ok();
    }

    [HttpPost("bankbevegelser/{id:guid}/ignorer")]
    public async Task<ActionResult> Ignorer(Guid id)
    {
        var bevegelse = await _repo.HentBevegelse(id);
        if (bevegelse == null) return NotFound();

        if (bevegelse.Status != BankbevegelseStatus.IkkeMatchet)
            throw new MatchAlleredeMatchetException();

        bevegelse.Status = BankbevegelseStatus.Ignorert;
        await _repo.LagreEndringerAsync();
        return Ok();
    }

    /// <summary>
    /// FR-B05: Direkte bokforing av umatched bankbevegelse.
    /// Oppretter bilag med bank-konto og motkonto.
    /// </summary>
    [HttpPost("bankbevegelser/{id:guid}/bokfor")]
    public async Task<ActionResult<BokforBankbevegelseResponse>> BokforDirekte(
        Guid id, [FromBody] BokforBankbevegelseRequest request)
    {
        // Valider at motkonto eksisterer og er bokforbar
        var konto = await _kontoplanRepo.HentKontoAsync(request.Motkontonummer);
        if (konto == null)
            return BadRequest($"Motkonto '{request.Motkontonummer}' finnes ikke.");
        if (!konto.ErAktiv)
            return BadRequest($"Motkonto '{request.Motkontonummer}' er ikke aktiv.");
        if (!konto.ErBokforbar)
            return BadRequest($"Motkonto '{request.Motkontonummer}' er ikke bokforbar.");

        var direkteRequest = new BokforDirekteRequest(
            request.MotkontoId,
            request.Motkontonummer,
            request.MvaKode,
            request.Beskrivelse,
            request.Avdelingskode,
            request.Prosjektkode);

        var bilagId = await _matchingService.BokforDirekte(id, direkteRequest);
        return Ok(new BokforBankbevegelseResponse(bilagId));
    }

    [HttpDelete("bankbevegelser/{id:guid}/match")]
    public async Task<ActionResult> FjernMatch(Guid id)
    {
        await _matchingService.FjernMatch(id);
        return Ok();
    }

    [HttpGet("kontoutskrifter/{id:guid}")]
    public async Task<ActionResult<KontoutskriftResponse>> HentKontoutskrift(Guid id)
    {
        var utskrift = await _repo.HentKontoutskrift(id);
        if (utskrift == null) return NotFound();

        return Ok(new KontoutskriftResponse(
            utskrift.Id, utskrift.MeldingsId, utskrift.PeriodeFra, utskrift.PeriodeTil,
            utskrift.InngaendeSaldo.Verdi, utskrift.UtgaendeSaldo.Verdi,
            utskrift.AntallBevegelser, utskrift.Status));
    }
}
