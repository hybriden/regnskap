namespace Regnskap.Application.Features.Kundereskontro;

using Regnskap.Domain.Features.Kundereskontro;

public interface IKundeFakturaService
{
    // Faktura
    Task<KundeFakturaDto> RegistrerFakturaAsync(RegistrerKundeFakturaRequest request, CancellationToken ct = default);
    Task<KundeFakturaDto> HentAsync(Guid id, CancellationToken ct = default);
    Task<(List<KundeFakturaDto> Data, int TotaltAntall)> SokAsync(KundeFakturaSokRequest request, CancellationToken ct = default);

    // Innbetaling
    Task<KundeInnbetalingDto> RegistrerInnbetalingAsync(RegistrerInnbetalingRequest request, CancellationToken ct = default);
    Task<KundeInnbetalingDto> MatchKidAsync(MatchKidRequest request, CancellationToken ct = default);

    // Tap
    Task<KundeFakturaDto> AvskrivTapAsync(Guid fakturaId, string begrunnelse, CancellationToken ct = default);

    // Rapporter
    Task<List<KundeFakturaDto>> HentApnePosterAsync(DateOnly? dato = null, CancellationToken ct = default);
    Task<KundeAldersfordelingDto> HentAldersfordelingAsync(DateOnly dato, CancellationToken ct = default);
    Task<KundeutskriftDto> HentUtskriftAsync(Guid kundeId, DateOnly fraDato, DateOnly tilDato, CancellationToken ct = default);
}
