using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Api.Features.Kundereskontro.Dtos;
using Regnskap.Application.Features.Kundereskontro;
using Regnskap.Domain.Features.Kundereskontro;

namespace Regnskap.Api.Features.Kundereskontro;

[ApiController]
[Authorize]
[Route("api/kundeinnbetalinger")]
public class KundeInnbetalingController : ControllerBase
{
    private readonly IKundeFakturaService _fakturaService;

    public KundeInnbetalingController(IKundeFakturaService fakturaService)
    {
        _fakturaService = fakturaService;
    }

    [HttpPost]
    public async Task<ActionResult<KundeInnbetalingDto>> Registrer([FromBody] RegistrerInnbetalingApiRequest request, CancellationToken ct)
    {
        try
        {
            var innbetaling = await _fakturaService.RegistrerInnbetalingAsync(new RegistrerInnbetalingRequest(
                request.KundeFakturaId,
                request.Innbetalingsdato,
                request.Belop,
                request.Bankreferanse,
                request.KidNummer,
                request.Betalingsmetode), ct);

            return Ok(innbetaling);
        }
        catch (KundeFakturaIkkeFunnetException ex)
        {
            return NotFound(new { kode = "FAKTURA_IKKE_FUNNET", melding = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { kode = "VALIDERING_FEIL", melding = ex.Message });
        }
    }

    [HttpPost("match-kid")]
    public async Task<ActionResult<KundeInnbetalingDto>> MatchKid([FromBody] MatchKidApiRequest request, CancellationToken ct)
    {
        try
        {
            var innbetaling = await _fakturaService.MatchKidAsync(new MatchKidRequest(
                request.KidNummer,
                request.Belop,
                request.Innbetalingsdato,
                request.Bankreferanse), ct);

            return Ok(innbetaling);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { kode = "KID_MATCH_FEIL", melding = ex.Message });
        }
    }
}
