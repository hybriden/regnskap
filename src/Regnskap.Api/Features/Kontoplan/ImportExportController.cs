using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Application.Features.Kontoplan;

namespace Regnskap.Api.Features.Kontoplan;

[ApiController]
[Authorize]
[Route("api/kontoplan")]
public class ImportExportController : ControllerBase
{
    private readonly IKontoplanImportExportService _importExportService;

    public ImportExportController(IKontoplanImportExportService importExportService)
    {
        _importExportService = importExportService;
    }

    /// <summary>
    /// Eksporterer kontoplanen i oensket format.
    /// </summary>
    [HttpGet("eksport")]
    public async Task<IActionResult> Eksport([FromQuery] string format = "json", CancellationToken ct = default)
    {
        return format.ToLowerInvariant() switch
        {
            "csv" => File(await _importExportService.EksporterCsvAsync(ct), "text/csv; charset=utf-8", "kontoplan.csv"),
            "json" => File(await _importExportService.EksporterJsonAsync(ct), "application/json; charset=utf-8", "kontoplan.json"),
            "saft" => File(await _importExportService.EksporterSaftAsync(ct), "application/xml; charset=utf-8", "kontoplan-saft.xml"),
            _ => BadRequest(new { kode = "UGYLDIG_FORMAT", melding = "Stottede formater: csv, json, saft" })
        };
    }

    /// <summary>
    /// Importerer kontoer fra CSV eller JSON.
    /// </summary>
    [HttpPost("import")]
    public async Task<ActionResult<ImportResultat>> Import(IFormFile fil, CancellationToken ct)
    {
        if (fil is null || fil.Length == 0)
            return BadRequest(new { kode = "FIL_MANGLER", melding = "Ingen fil ble lastet opp." });

        var extension = Path.GetExtension(fil.FileName).ToLowerInvariant();
        var contentType = fil.ContentType.ToLowerInvariant();

        ImportResultat resultat;
        using var stream = fil.OpenReadStream();

        if (extension == ".json" || contentType.Contains("json"))
        {
            resultat = await _importExportService.ImporterJsonAsync(stream, ct);
        }
        else if (extension == ".csv" || contentType.Contains("csv"))
        {
            resultat = await _importExportService.ImporterCsvAsync(stream, ct);
        }
        else
        {
            return BadRequest(new { kode = "UGYLDIG_FORMAT", melding = "Kun CSV og JSON er stottet for import." });
        }

        return Ok(resultat);
    }
}
