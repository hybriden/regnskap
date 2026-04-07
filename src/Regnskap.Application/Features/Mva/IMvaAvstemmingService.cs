namespace Regnskap.Application.Features.Mva;

public interface IMvaAvstemmingService
{
    Task<MvaAvstemmingDto> KjorAvstemmingAsync(Guid terminId, CancellationToken ct = default);
    Task<MvaAvstemmingDto> HentAvstemmingAsync(Guid terminId, CancellationToken ct = default);
    Task<List<MvaAvstemmingDto>> HentAvstemmingshistorikkAsync(Guid terminId, CancellationToken ct = default);
    Task<MvaAvstemmingDto> GodkjennAvstemmingAsync(Guid terminId, Guid avstemmingId, string? merknad, CancellationToken ct = default);
}
