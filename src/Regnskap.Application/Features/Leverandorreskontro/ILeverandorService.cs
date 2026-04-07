namespace Regnskap.Application.Features.Leverandorreskontro;

using Regnskap.Application.Common;

public interface ILeverandorService
{
    Task<LeverandorDto> OpprettAsync(OpprettLeverandorRequest request, CancellationToken ct = default);
    Task<LeverandorDto> OppdaterAsync(Guid id, OppdaterLeverandorRequest request, CancellationToken ct = default);
    Task<LeverandorDto> HentAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<LeverandorDto>> SokAsync(LeverandorSokRequest request, CancellationToken ct = default);
    Task SlettAsync(Guid id, CancellationToken ct = default);
}
