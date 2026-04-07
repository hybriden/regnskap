using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Application.Features.Hovedbok;

/// <summary>
/// Service for periodehanding: opprett, list, sperr, lukk.
/// </summary>
public interface IPeriodeService
{
    Task<List<RegnskapsperiodeDto>> OpprettPerioderForArAsync(int ar, CancellationToken ct = default);
    Task<List<RegnskapsperiodeDto>> HentPerioderAsync(int ar, CancellationToken ct = default);
    Task<RegnskapsperiodeDto> EndrePeriodeStatusAsync(
        int ar, int periode, PeriodeStatus nyStatus, string? merknad = null, CancellationToken ct = default);
    Task<PeriodeavstemmingDto> KjorPeriodeavstemmingAsync(
        int ar, int periode, CancellationToken ct = default);
}
