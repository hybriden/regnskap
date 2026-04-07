using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Application.Features.Hovedbok;

/// <summary>
/// Service for bilagregistrering og -oppslag.
/// Full implementasjon kommer i Modul 3 (Bilag).
/// Definerer kontrakten som Bilag-modulen skal implementere.
/// </summary>
public interface IBilagService
{
    /// <summary>
    /// Opprett et nytt bilag med posteringer. Atomisk bokforing.
    /// </summary>
    Task<BilagDto> OpprettBilagAsync(OpprettBilagRequest request, CancellationToken ct = default);

    /// <summary>
    /// Hent et bilag med alle posteringer.
    /// </summary>
    Task<BilagDto> HentBilagAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Hent et bilag basert pa bilagsnummer og ar.
    /// </summary>
    Task<BilagDto> HentBilagMedNummerAsync(int ar, int bilagsnummer, CancellationToken ct = default);

    /// <summary>
    /// Hent bilag med paginering og filtrering.
    /// </summary>
    Task<(List<BilagDto> Data, int TotaltAntall)> HentBilagListeAsync(
        int ar, int? periode = null, BilagType? type = null,
        int side = 1, int antall = 50, CancellationToken ct = default);
}
