using Regnskap.Application.Features.Bilagsregistrering;

namespace Regnskap.Tests.Features.Mva;

/// <summary>
/// Minimal fake for IBilagRegistreringService, only implements methods needed by MVA tests.
/// </summary>
public class FakeBilagRegistreringService : IBilagRegistreringService
{
    public Guid SistOpprettetBilagId { get; private set; }
    public Regnskap.Application.Features.Bilagsregistrering.OpprettBilagRequest? SisteRequest { get; private set; }

    public Task<Regnskap.Application.Features.Bilagsregistrering.BilagDto> OpprettOgBokforBilagAsync(
        Regnskap.Application.Features.Bilagsregistrering.OpprettBilagRequest request, CancellationToken ct = default)
    {
        SisteRequest = request;
        SistOpprettetBilagId = Guid.NewGuid();

        var dto = new Regnskap.Application.Features.Bilagsregistrering.BilagDto(
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
            new List<Regnskap.Application.Features.Bilagsregistrering.PosteringDto>(),
            new List<Regnskap.Application.Features.Bilagsregistrering.VedleggDto>(),
            0m, 0m,
            true, DateTime.UtcNow,
            false, null, null);

        return Task.FromResult(dto);
    }

    // --- Not implemented for MVA tests ---
    public Task<Regnskap.Application.Features.Bilagsregistrering.BilagDto> HentBilagDetaljertAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<Regnskap.Application.Features.Hovedbok.BilagDto> HentBilagMedNummerAsync(int ar, int bilagsnummer, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<Regnskap.Application.Features.Bilagsregistrering.BilagDto> HentBilagMedSerieAsync(string serieKode, int ar, int serieNummer, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<Regnskap.Application.Features.Bilagsregistrering.BilagDto> BokforBilagAsync(Guid id, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<Regnskap.Application.Features.Bilagsregistrering.BilagDto> TilbakeforBilagAsync(TilbakeforBilagRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<BilagValideringResultatDto> ValiderBilagAsync(ValiderBilagRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<Regnskap.Application.Features.Bilagsregistrering.VedleggDto> LeggTilVedleggAsync(LeggTilVedleggRequest request, CancellationToken ct = default)
        => throw new NotImplementedException();
    public Task<List<Regnskap.Application.Features.Bilagsregistrering.VedleggDto>> HentVedleggForBilagAsync(Guid bilagId, CancellationToken ct = default)
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
