using Regnskap.Application.Features.Bilagsregistrering;

namespace Regnskap.Tests.Features.Kundereskontro;

/// <summary>
/// Minimal fake for IBilagRegistreringService, for kundereskontro-tester.
/// </summary>
public class FakeBilagRegistreringService : IBilagRegistreringService
{
    public List<OpprettBilagRequest> AlleRequests { get; } = new();
    public Guid SistOpprettetBilagId { get; private set; }
    public OpprettBilagRequest? SisteRequest { get; private set; }

    public Task<BilagDto> OpprettOgBokforBilagAsync(
        OpprettBilagRequest request, CancellationToken ct = default)
    {
        SisteRequest = request;
        AlleRequests.Add(request);
        SistOpprettetBilagId = Guid.NewGuid();

        var dto = new BilagDto(
            SistOpprettetBilagId,
            $"B-{SistOpprettetBilagId:N}",
            null,
            1,
            null,
            request.SerieKode,
            request.Bilagsdato.Year,
            request.Type.ToString(),
            request.Bilagsdato,
            DateTime.UtcNow,
            request.Beskrivelse,
            request.EksternReferanse,
            new Regnskap.Application.Features.Hovedbok.RegnskapsperiodeDto(
                Guid.NewGuid(), request.Bilagsdato.Year, request.Bilagsdato.Month,
                $"Periode {request.Bilagsdato.Month}",
                new DateOnly(request.Bilagsdato.Year, request.Bilagsdato.Month, 1),
                request.Bilagsdato,
                "Apen", null, null, null),
            new List<PosteringDto>(),
            new List<VedleggDto>(),
            0m, 0m,
            true, DateTime.UtcNow,
            false, null, null);

        return Task.FromResult(dto);
    }

    // --- Not implemented ---
    public Task<BilagDto> HentBilagDetaljertAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<Regnskap.Application.Features.Hovedbok.BilagDto> HentBilagMedNummerAsync(int ar, int bilagsnummer, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<BilagDto> HentBilagMedSerieAsync(string serieKode, int ar, int serieNummer, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<BilagDto> BokforBilagAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<BilagDto> TilbakeforBilagAsync(TilbakeforBilagRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<BilagValideringResultatDto> ValiderBilagAsync(ValiderBilagRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<VedleggDto> LeggTilVedleggAsync(LeggTilVedleggRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<List<VedleggDto>> HentVedleggForBilagAsync(Guid bilagId, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task SlettVedleggAsync(Guid bilagId, Guid vedleggId, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<List<BilagSerieDto>> HentAlleBilagSerierAsync(CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<BilagSerieDto> HentBilagSerieAsync(string kode, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<BilagSerieDto> OpprettBilagSerieAsync(OpprettBilagSerieRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<BilagSerieDto> OppdaterBilagSerieAsync(string kode, OppdaterBilagSerieRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();
}
