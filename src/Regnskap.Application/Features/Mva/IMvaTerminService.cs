namespace Regnskap.Application.Features.Mva;

using Regnskap.Domain.Features.Mva;

public interface IMvaTerminService
{
    Task<List<MvaTerminDto>> HentTerminerAsync(int ar, CancellationToken ct = default);
    Task<MvaTerminDto> HentTerminAsync(Guid id, CancellationToken ct = default);
    Task<List<MvaTerminDto>> GenererTerminerAsync(int ar, MvaTerminType type, CancellationToken ct = default);
}
