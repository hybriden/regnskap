namespace Regnskap.Application.Features.Mva;

using Regnskap.Domain.Features.Kontoplan;

/// <summary>
/// Genererer SAF-T TaxTable-seksjon fra systemets aktive MVA-koder.
/// Lovgrunnlag: Bokforingsforskriften §7-8 (SAF-T rapportering).
/// </summary>
public class SaftTaxTableService : ISaftTaxTableService
{
    private readonly IKontoplanRepository _kontoplanRepo;

    public SaftTaxTableService(IKontoplanRepository kontoplanRepo)
    {
        _kontoplanRepo = kontoplanRepo;
    }

    public async Task<SaftTaxTableDto> GenererSaftTaxTableAsync(CancellationToken ct = default)
    {
        var mvaKoder = await _kontoplanRepo.HentAlleMvaKoderAsync(erAktiv: true, ct: ct);

        var taxCodeDetails = mvaKoder.Select(mk => new SaftTaxCodeDetailDto(
            TaxCode: mk.Kode,
            Description: mk.Beskrivelse,
            StandardTaxCode: mk.StandardTaxCode,
            TaxPercentage: mk.Sats,
            Country: "NO",
            BaseRate: 0m
        )).ToList();

        return new SaftTaxTableDto(taxCodeDetails);
    }
}
