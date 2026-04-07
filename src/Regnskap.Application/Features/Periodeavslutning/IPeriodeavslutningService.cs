namespace Regnskap.Application.Features.Periodeavslutning;

using Regnskap.Domain.Features.Periodeavslutning;

public interface IPeriodeavslutningService
{
    // Avstemming
    Task<AvstemmingResultatDto> KjorAvstemmingAsync(
        int ar, int periode, CancellationToken ct = default);

    // Lukking
    Task<PeriodeLukkingDto> LukkPeriodeAsync(
        int ar, int periode, LukkPeriodeRequest request, CancellationToken ct = default);
    Task<PeriodeLukkingDto> GjenapnePeriodeAsync(
        int ar, int periode, GjenapnePeriodeRequest request, CancellationToken ct = default);

    // Arsavslutning
    Task<ArsavslutningDto> KjorArsavslutningAsync(
        int ar, ArsavslutningRequest request, CancellationToken ct = default);
    Task<ArsavslutningStatus> HentArsavslutningStatusAsync(
        int ar, CancellationToken ct = default);

    // Arsregnskapsklargjoring
    Task<ArsregnskapsklarDto> SjekkKlargjoringAsync(
        int ar, CancellationToken ct = default);
}

public interface IAvskrivningService
{
    // Anleggsmidler
    Task<AnleggsmiddelDto> OpprettAnleggsmiddelAsync(
        OpprettAnleggsmiddelRequest request, CancellationToken ct = default);
    Task<List<AnleggsmiddelDto>> HentAnleggsmidlerAsync(
        bool? aktive = true, string? kontonummer = null, CancellationToken ct = default);
    Task<AnleggsmiddelDto> HentAnleggsmiddelAsync(
        Guid id, CancellationToken ct = default);
    Task UtrangerAnleggsmiddelAsync(
        Guid id, DateOnly utrangeringsDato, CancellationToken ct = default);

    // Avskrivninger
    Task<AvskrivningBeregningDto> BeregnAvskrivningerAsync(
        int ar, int periode, CancellationToken ct = default);
    Task<AvskrivningBokforingDto> BokforAvskrivningerAsync(
        int ar, int periode, CancellationToken ct = default);
}

public interface IPeriodiseringsService
{
    Task<PeriodiseringDto> OpprettPeriodiseringAsync(
        OpprettPeriodiseringRequest request, CancellationToken ct = default);
    Task<List<PeriodiseringDto>> HentPeriodiseringerAsync(
        bool? aktive = true, CancellationToken ct = default);
    Task<PeriodiseringBokforingDto> BokforPeriodiseringerAsync(
        int ar, int periode, CancellationToken ct = default);
    Task DeaktiverPeriodiseringAsync(
        Guid id, CancellationToken ct = default);
}
