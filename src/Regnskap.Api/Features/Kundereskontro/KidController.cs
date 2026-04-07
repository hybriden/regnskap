using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Api.Features.Kundereskontro.Dtos;
using Regnskap.Application.Features.Kundereskontro;
using Regnskap.Domain.Features.Kundereskontro;

namespace Regnskap.Api.Features.Kundereskontro;

[ApiController]
[Authorize]
[Route("api/kid")]
public class KidController : ControllerBase
{
    private readonly IKidService _kidService;
    private readonly IKundeService _kundeService;

    public KidController(IKidService kidService, IKundeService kundeService)
    {
        _kidService = kidService;
        _kundeService = kundeService;
    }

    [HttpGet("generer")]
    public async Task<ActionResult<object>> Generer(
        [FromQuery] Guid kundeId,
        [FromQuery] int fakturanummer,
        CancellationToken ct)
    {
        try
        {
            var kunde = await _kundeService.HentAsync(kundeId, ct);
            var algoritme = KidAlgoritme.MOD10; // Default
            var kid = _kidService.Generer(kunde.Kundenummer, fakturanummer, algoritme);
            return Ok(new { kidNummer = kid, algoritme = algoritme.ToString() });
        }
        catch (KundeIkkeFunnetException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { kode = "KID_GENERERING_FEIL", melding = ex.Message });
        }
    }

    [HttpPost("valider")]
    public ActionResult<object> Valider([FromBody] ValiderKidApiRequest request)
    {
        var erGyldig = _kidService.Valider(request.KidNummer, request.Algoritme);
        return Ok(new { kidNummer = request.KidNummer, algoritme = request.Algoritme.ToString(), erGyldig });
    }
}
