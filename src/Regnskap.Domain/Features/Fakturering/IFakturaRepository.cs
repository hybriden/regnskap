namespace Regnskap.Domain.Features.Fakturering;

/// <summary>
/// Repository for fakturering.
/// </summary>
public interface IFakturaRepository
{
    Task<Faktura?> HentAsync(Guid id, CancellationToken ct = default);
    Task<Faktura?> HentMedLinjerAsync(Guid id, CancellationToken ct = default);
    Task LeggTilAsync(Faktura faktura, CancellationToken ct = default);
    Task<int> NesteNummerAsync(int aar, FakturaDokumenttype type, CancellationToken ct = default);
    Task<Selskapsinfo?> HentSelskapsinfoAsync(CancellationToken ct = default);
    Task<List<Faktura>> SokAsync(FakturaSokFilter filter, CancellationToken ct = default);
    Task<int> TellAsync(FakturaSokFilter filter, CancellationToken ct = default);
    Task LagreEndringerAsync(CancellationToken ct = default);
}

/// <summary>
/// Sokfilter for fakturaer.
/// </summary>
public class FakturaSokFilter
{
    public FakturaStatus? Status { get; set; }
    public FakturaDokumenttype? Dokumenttype { get; set; }
    public Guid? KundeId { get; set; }
    public DateOnly? FraDato { get; set; }
    public DateOnly? TilDato { get; set; }
    public string? Sok { get; set; }
    public int Side { get; set; } = 1;
    public int Antall { get; set; } = 50;
}
