namespace Regnskap.Application.Features.Bilagsregistrering;

using Regnskap.Application.Features.Hovedbok;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Service for bilagsok (FR-12).
/// </summary>
public class BilagSokService
{
    private readonly IBilagRepository _bilagRepo;

    public BilagSokService(IBilagRepository bilagRepo)
    {
        _bilagRepo = bilagRepo;
    }

    public async Task<BilagSokResultatDto> SokAsync(BilagSokRequest request, CancellationToken ct = default)
    {
        var antall = Math.Min(request.Antall, 200);

        var parametere = new BilagSokParametre
        {
            Ar = request.Ar,
            Periode = request.Periode,
            Type = request.Type,
            SerieKode = request.SerieKode,
            FraDato = request.FraDato,
            TilDato = request.TilDato,
            Kontonummer = request.Kontonummer,
            MinBelop = request.MinBelop,
            MaxBelop = request.MaxBelop,
            Beskrivelse = request.Beskrivelse,
            EksternReferanse = request.EksternReferanse,
            Bilagsnummer = request.Bilagsnummer,
            ErBokfort = request.ErBokfort,
            ErTilbakfort = request.ErTilbakfort,
            Side = request.Side,
            Antall = antall
        };

        var (data, totaltAntall) = await _bilagRepo.SokBilagAsync(parametere, ct);

        var dtos = data.Select(MapTilBilagDto).ToList();

        return new BilagSokResultatDto(dtos, totaltAntall, request.Side, antall);
    }

    private static BilagDto MapTilBilagDto(Bilag bilag)
    {
        var periodeDto = bilag.Regnskapsperiode != null
            ? new RegnskapsperiodeDto(
                bilag.Regnskapsperiode.Id,
                bilag.Regnskapsperiode.Ar,
                bilag.Regnskapsperiode.Periode,
                bilag.Regnskapsperiode.Periodenavn,
                bilag.Regnskapsperiode.FraDato,
                bilag.Regnskapsperiode.TilDato,
                bilag.Regnskapsperiode.Status.ToString(),
                bilag.Regnskapsperiode.LukketTidspunkt,
                bilag.Regnskapsperiode.LukketAv,
                bilag.Regnskapsperiode.Merknad)
            : new RegnskapsperiodeDto(
                Guid.Empty, bilag.Ar, bilag.Bilagsdato.Month,
                $"{bilag.Ar}-{bilag.Bilagsdato.Month:D2}",
                default, default, "Ukjent", null, null, null);

        return new BilagDto(
            bilag.Id,
            bilag.BilagsId,
            bilag.SerieBilagsId,
            bilag.Bilagsnummer,
            bilag.SerieNummer,
            bilag.SerieKode,
            bilag.Ar,
            bilag.Type.ToString(),
            bilag.Bilagsdato,
            bilag.Registreringsdato,
            bilag.Beskrivelse,
            bilag.EksternReferanse,
            periodeDto,
            bilag.Posteringer.OrderBy(p => p.Linjenummer).Select(p => new PosteringDto(
                p.Id, p.Linjenummer, p.Kontonummer, p.Konto?.Navn ?? "",
                p.Side.ToString(), p.Belop.Verdi, p.Beskrivelse,
                p.MvaKode, p.MvaBelop?.Verdi, p.MvaGrunnlag?.Verdi, p.MvaSats,
                p.Avdelingskode, p.Prosjektkode, p.KundeId, p.LeverandorId,
                p.ErAutoGenerertMva)).ToList(),
            bilag.Vedlegg.Where(v => !v.IsDeleted).Select(v => new VedleggDto(
                v.Id, v.Filnavn, v.MimeType, v.Storrelse, v.LagringSti,
                v.Beskrivelse, v.Rekkefolge, v.CreatedAt)).ToList(),
            bilag.SumDebet().Verdi,
            bilag.SumKredit().Verdi,
            bilag.ErBokfort,
            bilag.BokfortTidspunkt,
            bilag.ErTilbakfort,
            bilag.TilbakefortFraBilagId,
            bilag.TilbakefortAvBilagId);
    }
}
