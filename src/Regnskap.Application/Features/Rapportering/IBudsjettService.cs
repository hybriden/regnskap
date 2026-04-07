namespace Regnskap.Application.Features.Rapportering;

public interface IBudsjettService
{
    Task<BudsjettDto> OpprettBudsjettLinjeAsync(OpprettBudsjettRequest request, CancellationToken ct = default);
    Task<List<BudsjettDto>> BulkImportAsync(BudsjettBulkRequest request, CancellationToken ct = default);
    Task<List<BudsjettDto>> HentBudsjettAsync(int ar, string versjon = "Opprinnelig", CancellationToken ct = default);
    Task SlettBudsjettAsync(int ar, string versjon, CancellationToken ct = default);
}
