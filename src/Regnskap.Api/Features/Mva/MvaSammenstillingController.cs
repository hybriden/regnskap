using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Mva;

namespace Regnskap.Api.Features.Mva;

[ApiController]
[Route("api/mva/sammenstilling")]
[Authorize]
public class MvaSammenstillingController : ControllerBase
{
    private readonly IMvaSammenstillingService _sammenstillingService;

    public MvaSammenstillingController(IMvaSammenstillingService sammenstillingService)
    {
        _sammenstillingService = sammenstillingService;
    }

    [HttpGet]
    public async Task<ActionResult<MvaSammenstillingDto>> HentSammenstilling(
        [FromQuery] int ar, [FromQuery] int termin, CancellationToken ct)
    {
        try
        {
            var sammenstilling = await _sammenstillingService.HentSammenstillingAsync(ar, termin, ct);
            return Ok(sammenstilling);
        }
        catch (MvaTerminIkkeFunnetException)
        {
            return NotFound(new { Kode = "MVA_TERMIN_IKKE_FUNNET", Melding = $"MVA-termin {ar}/{termin} finnes ikke." });
        }
    }

    [HttpGet("detalj")]
    public async Task<ActionResult<MvaSammenstillingDetaljDto>> HentSammenstillingDetalj(
        [FromQuery] int ar, [FromQuery] int termin, [FromQuery] string mvaKode, CancellationToken ct)
    {
        try
        {
            var detalj = await _sammenstillingService.HentSammenstillingDetaljAsync(ar, termin, mvaKode, ct);
            return Ok(detalj);
        }
        catch (MvaTerminIkkeFunnetException)
        {
            return NotFound(new { Kode = "MVA_TERMIN_IKKE_FUNNET", Melding = $"MVA-termin {ar}/{termin} finnes ikke." });
        }
    }
}
