namespace Regnskap.Application.Features.Leverandorreskontro;

public interface IBetalingsforslagService
{
    Task<BetalingsforslagDto> GenererAsync(GenererBetalingsforslagRequest request, CancellationToken ct = default);
    Task<BetalingsforslagDto> HentAsync(Guid id, CancellationToken ct = default);
    Task<BetalingsforslagDto> GodkjennAsync(Guid id, string godkjentAv, CancellationToken ct = default);
    Task<byte[]> GenererFilAsync(Guid id, CancellationToken ct = default);
    Task MarkerSendtAsync(Guid id, CancellationToken ct = default);
    Task KansellerAsync(Guid id, CancellationToken ct = default);
    Task EkskluderLinjeAsync(Guid forslagId, Guid linjeId, CancellationToken ct = default);
    Task InkluderLinjeAsync(Guid forslagId, Guid linjeId, CancellationToken ct = default);
}
