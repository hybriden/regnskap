namespace Regnskap.Application.Features.Mva;

public interface ISaftTaxTableService
{
    Task<SaftTaxTableDto> GenererSaftTaxTableAsync(CancellationToken ct = default);
}
