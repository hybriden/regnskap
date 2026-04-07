namespace Regnskap.Application.Features.Mva;

public interface IMvaMeldingService
{
    Task<MvaMeldingDto> GenererMvaMeldingAsync(Guid terminId, CancellationToken ct = default);
    Task MarkerInnsendtAsync(Guid terminId, CancellationToken ct = default);
}
