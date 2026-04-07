namespace Regnskap.Application.Features.Leverandorreskontro;

using Regnskap.Application.Common;

public interface ILeverandorFakturaService
{
    Task<LeverandorFakturaDto> RegistrerFakturaAsync(RegistrerFakturaRequest request, CancellationToken ct = default);
    Task<LeverandorFakturaDto> GodkjennAsync(Guid id, CancellationToken ct = default);
    Task<LeverandorFakturaDto> SperrAsync(Guid id, string arsak, CancellationToken ct = default);
    Task<LeverandorFakturaDto> OpphevSperringAsync(Guid id, CancellationToken ct = default);
    Task<LeverandorFakturaDto> HentAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<LeverandorFakturaDto>> SokAsync(FakturaSokRequest request, CancellationToken ct = default);

    // Rapporter
    Task<List<LeverandorFakturaDto>> HentApnePosterAsync(DateOnly? dato = null, CancellationToken ct = default);
    Task<AldersfordelingDto> HentAldersfordelingAsync(DateOnly dato, CancellationToken ct = default);
    Task<LeverandorutskriftDto> HentUtskriftAsync(Guid leverandorId, DateOnly fraDato, DateOnly tilDato, CancellationToken ct = default);
}
