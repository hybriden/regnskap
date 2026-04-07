using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Regnskap.Api.Features.Kontoplan.Dtos;
using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Api.Features.Kontoplan;

[ApiController]
[Authorize]
[Route("api/v1/kontogrupper")]
public class KontogruppeController : ControllerBase
{
    private readonly IKontoplanRepository _repository;

    public KontogruppeController(IKontoplanRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<KontogruppeListeDto>>> HentAlle(CancellationToken ct)
    {
        var grupper = await _repository.HentAlleKontogrupperAsync(ct);
        var dtos = grupper.Select(g => new KontogruppeListeDto(
            g.Id,
            g.Gruppekode,
            g.Navn,
            g.NavnEn,
            (int)g.Kontoklasse,
            g.Kontoklasse.ToString(),
            g.Kontotype.ToString(),
            g.Normalbalanse.ToString(),
            g.ErSystemgruppe,
            g.Kontoer.Count
        )).ToList();

        return Ok(dtos);
    }

    [HttpGet("{gruppekode:int}")]
    public async Task<ActionResult<KontogruppeDetaljerDto>> HentEnkel(int gruppekode, CancellationToken ct)
    {
        var gruppe = await _repository.HentKontogruppeAsync(gruppekode, ct);
        if (gruppe is null)
            return NotFound();

        var dto = new KontogruppeDetaljerDto(
            gruppe.Id,
            gruppe.Gruppekode,
            gruppe.Navn,
            gruppe.NavnEn,
            (int)gruppe.Kontoklasse,
            gruppe.Kontoklasse.ToString(),
            gruppe.Kontotype.ToString(),
            gruppe.Normalbalanse.ToString(),
            gruppe.ErSystemgruppe,
            gruppe.Kontoer.Select(k => new KontoKortDto(
                k.Id, k.Kontonummer, k.Navn, k.Kontotype.ToString(), k.ErAktiv, k.StandardMvaKode
            )).ToList()
        );

        return Ok(dto);
    }
}
