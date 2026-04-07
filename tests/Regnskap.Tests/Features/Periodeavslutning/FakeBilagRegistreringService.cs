using Regnskap.Domain.Features.Hovedbok;
using BilagReg = Regnskap.Application.Features.Bilagsregistrering;

namespace Regnskap.Tests.Features.Periodeavslutning;

public class FakeBilagRegistreringServiceForPeriodeavslutning : BilagReg.IBilagRegistreringService
{
    public List<BilagReg.OpprettBilagRequest> AlleRequests { get; } = new();
    public Guid SistOpprettetBilagId { get; private set; }
    public BilagReg.OpprettBilagRequest? SisteRequest { get; private set; }

    public Task<BilagReg.BilagDto> OpprettOgBokforBilagAsync(
        BilagReg.OpprettBilagRequest request, CancellationToken ct = default)
    {
        SisteRequest = request;
        AlleRequests.Add(request);
        SistOpprettetBilagId = Guid.NewGuid();

        var dto = new BilagReg.BilagDto(
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
            new List<BilagReg.PosteringDto>(),
            new List<BilagReg.VedleggDto>(),
            request.Posteringer.Where(p => p.Side == BokforingSide.Debet).Sum(p => p.Belop),
            request.Posteringer.Where(p => p.Side == BokforingSide.Kredit).Sum(p => p.Belop),
            true,
            DateTime.UtcNow,
            false,
            null,
            null);

        return Task.FromResult(dto);
    }

    public Task<BilagReg.BilagDto> HentBilagDetaljertAsync(Guid id, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<Regnskap.Application.Features.Hovedbok.BilagDto> HentBilagMedNummerAsync(int ar, int bilagsnummer, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<BilagReg.BilagDto> HentBilagMedSerieAsync(string serieKode, int ar, int serieNummer, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<BilagReg.BilagDto> BokforBilagAsync(Guid id, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<BilagReg.BilagDto> TilbakeforBilagAsync(BilagReg.TilbakeforBilagRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<BilagReg.BilagValideringResultatDto> ValiderBilagAsync(BilagReg.ValiderBilagRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<BilagReg.VedleggDto> LeggTilVedleggAsync(BilagReg.LeggTilVedleggRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<List<BilagReg.VedleggDto>> HentVedleggForBilagAsync(Guid bilagId, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task SlettVedleggAsync(Guid bilagId, Guid vedleggId, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<List<BilagReg.BilagSerieDto>> HentAlleBilagSerierAsync(CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<BilagReg.BilagSerieDto> HentBilagSerieAsync(string kode, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<BilagReg.BilagSerieDto> OpprettBilagSerieAsync(BilagReg.OpprettBilagSerieRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<BilagReg.BilagSerieDto> OppdaterBilagSerieAsync(string kode, BilagReg.OppdaterBilagSerieRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException();
}
