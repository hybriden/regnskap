namespace Regnskap.Application.Features.Kundereskontro;

public interface IKundeService
{
    Task<KundeDto> OpprettAsync(OpprettKundeRequest request, CancellationToken ct = default);
    Task<KundeDto> OppdaterAsync(Guid id, OppdaterKundeRequest request, CancellationToken ct = default);
    Task<KundeDto> HentAsync(Guid id, CancellationToken ct = default);
    Task<(List<KundeDto> Data, int TotaltAntall)> SokAsync(KundeSokRequest request, CancellationToken ct = default);
    Task SlettAsync(Guid id, CancellationToken ct = default);
    Task<decimal> HentSaldoAsync(Guid id, CancellationToken ct = default);
}
