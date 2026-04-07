namespace Regnskap.Application.Features.Mva;

public interface IMvaOppgjorService
{
    Task<MvaOppgjorDto> BeregnOppgjorAsync(Guid terminId, CancellationToken ct = default);
    Task<MvaOppgjorDto> HentOppgjorAsync(Guid terminId, CancellationToken ct = default);
    Task<MvaOppgjorDto> BokforOppgjorAsync(Guid terminId, CancellationToken ct = default);
}
