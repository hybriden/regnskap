namespace Regnskap.Application.Features.Mva;

public interface IMvaSammenstillingService
{
    Task<MvaSammenstillingDto> HentSammenstillingAsync(int ar, int termin, CancellationToken ct = default);
    Task<MvaSammenstillingDetaljDto> HentSammenstillingDetaljAsync(int ar, int termin, string mvaKode, CancellationToken ct = default);
}
