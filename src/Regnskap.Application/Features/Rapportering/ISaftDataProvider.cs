using Regnskap.Domain.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kontoplan;
using Regnskap.Domain.Features.Kundereskontro;
using Regnskap.Domain.Features.Leverandorreskontro;

namespace Regnskap.Application.Features.Rapportering;

/// <summary>
/// Aggregerer data fra flere moduler for SAF-T eksport.
/// Implementeres i Infrastructure-laget med direkte DbContext-tilgang.
/// </summary>
public interface ISaftDataProvider
{
    Task<List<Konto>> HentKontoerAsync(CancellationToken ct = default);
    Task<List<Kunde>> HentKunderAsync(CancellationToken ct = default);
    Task<List<Leverandor>> HentLeverandorerAsync(CancellationToken ct = default);
    Task<List<MvaKode>> HentMvaKoderAsync(CancellationToken ct = default);
    Task<List<BilagSerie>> HentBilagSerierAsync(CancellationToken ct = default);
    Task<List<Bilag>> HentBilagMedPosteringerAsync(int ar, int fraPeriode, int tilPeriode, CancellationToken ct = default);
}
