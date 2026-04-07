using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Api.Features.Fakturering.Dtos;
using Regnskap.Application.Features.Fakturering;
using Regnskap.Domain.Features.Fakturering;

namespace Regnskap.Api.Features.Fakturering;

[ApiController]
[Route("api/fakturaer")]
[Authorize]
public class FakturaController : ControllerBase
{
    private readonly IFaktureringService _faktureringService;
    private readonly IEhfService _ehfService;
    private readonly IFakturaPdfService _pdfService;

    public FakturaController(
        IFaktureringService faktureringService,
        IEhfService ehfService,
        IFakturaPdfService pdfService)
    {
        _faktureringService = faktureringService;
        _ehfService = ehfService;
        _pdfService = pdfService;
    }

    /// <summary>
    /// List fakturaer med filtrering/paginering.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<FakturaListeResponse>> HentFakturaer(
        [FromQuery] FakturaStatus? status,
        [FromQuery] FakturaDokumenttype? dokumenttype,
        [FromQuery] Guid? kundeId,
        [FromQuery] DateOnly? fraDato,
        [FromQuery] DateOnly? tilDato,
        [FromQuery] string? sok,
        [FromQuery] int side = 1,
        [FromQuery] int antall = 50,
        CancellationToken ct = default)
    {
        var filter = new FakturaSokFilter
        {
            Status = status,
            Dokumenttype = dokumenttype,
            KundeId = kundeId,
            FraDato = fraDato,
            TilDato = tilDato,
            Sok = sok,
            Side = side,
            Antall = antall
        };

        var resultat = await _faktureringService.SokFakturaerAsync(filter, ct);
        return Ok(new FakturaListeResponse(
            resultat.Fakturaer.Select(FakturaMapper.TilResponse).ToList(),
            resultat.TotaltAntall,
            resultat.Side,
            resultat.Antall));
    }

    /// <summary>
    /// Hent enkelt faktura med linjer.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FakturaResponse>> HentFaktura(Guid id, CancellationToken ct = default)
    {
        var faktura = await _faktureringService.HentFakturaAsync(id, ct);
        if (faktura == null) return NotFound();
        return Ok(FakturaMapper.TilResponse(faktura));
    }

    /// <summary>
    /// Opprett ny faktura (utkast).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<FakturaResponse>> OpprettFaktura(
        OpprettFakturaRequest request, CancellationToken ct = default)
    {
        try
        {
            var faktura = await _faktureringService.OpprettFakturaAsync(request, ct);
            return CreatedAtAction(nameof(HentFaktura), new { id = faktura.Id }, FakturaMapper.TilResponse(faktura));
        }
        catch (FakturaException ex)
        {
            return BadRequest(new { kode = ex.Kode, melding = ex.Message });
        }
    }

    /// <summary>
    /// Oppdater utkast.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FakturaResponse>> OppdaterFaktura(
        Guid id, OpprettFakturaRequest request, CancellationToken ct = default)
    {
        try
        {
            var faktura = await _faktureringService.OppdaterFakturaAsync(id, request, ct);
            return Ok(FakturaMapper.TilResponse(faktura));
        }
        catch (FakturaException ex)
        {
            return BadRequest(new { kode = ex.Kode, melding = ex.Message });
        }
    }

    /// <summary>
    /// Utstede faktura (tildel nummer, bokfor, generer KID).
    /// </summary>
    [HttpPost("{id:guid}/utstede")]
    public async Task<ActionResult<FakturaResponse>> UtstedeFaktura(Guid id, CancellationToken ct = default)
    {
        try
        {
            var faktura = await _faktureringService.UtstedeFakturaAsync(id, ct);
            return Ok(FakturaMapper.TilResponse(faktura));
        }
        catch (FakturaException ex)
        {
            return BadRequest(new { kode = ex.Kode, melding = ex.Message });
        }
    }

    /// <summary>
    /// Opprett kreditnota for faktura.
    /// </summary>
    [HttpPost("{id:guid}/kreditnota")]
    public async Task<ActionResult<FakturaResponse>> OpprettKreditnota(
        Guid id, OpprettKreditnotaRequest request, CancellationToken ct = default)
    {
        try
        {
            var kreditnota = await _faktureringService.OpprettKreditnotaAsync(id, request, ct);
            return CreatedAtAction(nameof(HentFaktura), new { id = kreditnota.Id }, FakturaMapper.TilResponse(kreditnota));
        }
        catch (FakturaException ex)
        {
            return BadRequest(new { kode = ex.Kode, melding = ex.Message });
        }
    }

    /// <summary>
    /// Generer EHF-XML.
    /// </summary>
    [HttpPost("{id:guid}/ehf")]
    public async Task<ActionResult> GenererEhf(Guid id, CancellationToken ct = default)
    {
        try
        {
            var xml = await _ehfService.GenererEhfXmlAsync(id, ct);
            return File(xml, "application/xml", $"faktura-{id}.xml");
        }
        catch (FakturaException ex)
        {
            return BadRequest(new { kode = ex.Kode, melding = ex.Message });
        }
    }

    /// <summary>
    /// Last ned EHF-XML.
    /// </summary>
    [HttpGet("{id:guid}/ehf")]
    public async Task<ActionResult> LastNedEhf(Guid id, CancellationToken ct = default)
    {
        try
        {
            var xml = await _ehfService.GenererEhfXmlAsync(id, ct);
            return File(xml, "application/xml", $"faktura-{id}.xml");
        }
        catch (FakturaException ex)
        {
            return BadRequest(new { kode = ex.Kode, melding = ex.Message });
        }
    }

    /// <summary>
    /// Generer PDF.
    /// </summary>
    [HttpPost("{id:guid}/pdf")]
    public async Task<ActionResult> GenererPdf(Guid id, CancellationToken ct = default)
    {
        try
        {
            var pdf = await _pdfService.GenererPdfAsync(id, ct);
            return File(pdf, "text/html", $"faktura-{id}.html");
        }
        catch (FakturaException ex)
        {
            return BadRequest(new { kode = ex.Kode, melding = ex.Message });
        }
    }

    /// <summary>
    /// Last ned PDF.
    /// </summary>
    [HttpGet("{id:guid}/pdf")]
    public async Task<ActionResult> LastNedPdf(Guid id, CancellationToken ct = default)
    {
        try
        {
            var pdf = await _pdfService.GenererPdfAsync(id, ct);
            return File(pdf, "text/html", $"faktura-{id}.html");
        }
        catch (FakturaException ex)
        {
            return BadRequest(new { kode = ex.Kode, melding = ex.Message });
        }
    }

    /// <summary>
    /// Kanseller utkast (soft delete).
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> KansellerFaktura(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _faktureringService.KansellerFakturaAsync(id, ct);
            return NoContent();
        }
        catch (FakturaException ex)
        {
            return BadRequest(new { kode = ex.Kode, melding = ex.Message });
        }
    }
}
