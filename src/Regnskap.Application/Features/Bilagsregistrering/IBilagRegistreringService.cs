namespace Regnskap.Application.Features.Bilagsregistrering;

/// <summary>
/// Interface for bilagsregistrering-tjenesten.
/// Utvider IBilagService med bilag-spesifikke operasjoner (bokforing, tilbakeforing, vedlegg, serier).
/// </summary>
public interface IBilagRegistreringService
{
    // --- Bilag CRUD og bokforing ---
    Task<BilagDto> OpprettOgBokforBilagAsync(OpprettBilagRequest request, CancellationToken ct = default);
    Task<BilagDto> HentBilagDetaljertAsync(Guid id, CancellationToken ct = default);
    Task<Application.Features.Hovedbok.BilagDto> HentBilagMedNummerAsync(int ar, int bilagsnummer, CancellationToken ct = default);
    Task<BilagDto> HentBilagMedSerieAsync(string serieKode, int ar, int serieNummer, CancellationToken ct = default);
    Task<BilagDto> BokforBilagAsync(Guid id, CancellationToken ct = default);
    Task<BilagDto> TilbakeforBilagAsync(TilbakeforBilagRequest request, CancellationToken ct = default);
    Task<BilagValideringResultatDto> ValiderBilagAsync(ValiderBilagRequest request, CancellationToken ct = default);

    // --- Vedlegg ---
    Task<VedleggDto> LeggTilVedleggAsync(LeggTilVedleggRequest request, CancellationToken ct = default);
    Task<List<VedleggDto>> HentVedleggForBilagAsync(Guid bilagId, CancellationToken ct = default);
    Task SlettVedleggAsync(Guid bilagId, Guid vedleggId, CancellationToken ct = default);

    // --- Bilagserier ---
    Task<List<BilagSerieDto>> HentAlleBilagSerierAsync(CancellationToken ct = default);
    Task<BilagSerieDto> HentBilagSerieAsync(string kode, CancellationToken ct = default);
    Task<BilagSerieDto> OpprettBilagSerieAsync(OpprettBilagSerieRequest request, CancellationToken ct = default);
    Task<BilagSerieDto> OppdaterBilagSerieAsync(string kode, OppdaterBilagSerieRequest request, CancellationToken ct = default);
}
