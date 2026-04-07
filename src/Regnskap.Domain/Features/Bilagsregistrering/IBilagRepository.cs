namespace Regnskap.Domain.Features.Bilagsregistrering;

using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Repository for bilagsregistrering-spesifikke operasjoner.
/// Utvider IHovedbokRepository med serie- og vedleggshåndtering.
/// </summary>
public interface IBilagRepository
{
    // --- BilagSerie ---
    Task<BilagSerie?> HentBilagSerieAsync(string kode, CancellationToken ct = default);
    Task<BilagSerie?> HentBilagSerieMedIdAsync(Guid id, CancellationToken ct = default);
    Task<List<BilagSerie>> HentAlleBilagSerierAsync(CancellationToken ct = default);
    Task LeggTilBilagSerieAsync(BilagSerie serie, CancellationToken ct = default);

    // --- BilagSerieNummer ---
    Task<BilagSerieNummer?> HentSerieNummerAsync(string serieKode, int ar, CancellationToken ct = default);
    Task LeggTilSerieNummerAsync(BilagSerieNummer serieNummer, CancellationToken ct = default);

    // --- Vedlegg ---
    Task<Vedlegg?> HentVedleggAsync(Guid id, CancellationToken ct = default);
    Task<List<Vedlegg>> HentVedleggForBilagAsync(Guid bilagId, CancellationToken ct = default);
    Task LeggTilVedleggAsync(Vedlegg vedlegg, CancellationToken ct = default);

    // --- Utvidet Bilag-oppslag ---
    Task<Bilag?> HentBilagMedSerieAsync(string serieKode, int ar, int serieNummer, CancellationToken ct = default);
    Task<(List<Bilag> Data, int TotaltAntall)> SokBilagAsync(BilagSokParametre parametere, CancellationToken ct = default);

    // --- Generelt ---
    Task LagreEndringerAsync(CancellationToken ct = default);
}

/// <summary>
/// Interne sokeparametere for bilagsok-spørring.
/// </summary>
public class BilagSokParametre
{
    public int? Ar { get; set; }
    public int? Periode { get; set; }
    public BilagType? Type { get; set; }
    public string? SerieKode { get; set; }
    public DateOnly? FraDato { get; set; }
    public DateOnly? TilDato { get; set; }
    public string? Kontonummer { get; set; }
    public decimal? MinBelop { get; set; }
    public decimal? MaxBelop { get; set; }
    public string? Beskrivelse { get; set; }
    public string? EksternReferanse { get; set; }
    public int? Bilagsnummer { get; set; }
    public bool? ErBokfort { get; set; }
    public bool? ErTilbakfort { get; set; }
    public int Side { get; set; } = 1;
    public int Antall { get; set; } = 50;
}
