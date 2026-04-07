namespace Regnskap.Application.Features.Kundereskontro;

using Regnskap.Domain.Features.Kundereskontro;

public interface IPurringService
{
    Task<List<PurreforslagDto>> GenererForslagAsync(PurreforslagRequest request, CancellationToken ct = default);
    Task<List<PurringDto>> OpprettPurringerAsync(List<Guid> fakturaIder, PurringType type, CancellationToken ct = default);
    Task MarkerSendtAsync(Guid purringId, string sendemetode, CancellationToken ct = default);
    Task<List<PurringDto>> HentPurringerAsync(int side, int antall, CancellationToken ct = default);
}
