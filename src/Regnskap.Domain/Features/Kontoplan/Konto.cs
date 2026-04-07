using Regnskap.Domain.Common;

namespace Regnskap.Domain.Features.Kontoplan;

/// <summary>
/// En konto i kontoplanen. Firesifret kontonummer ihht NS 4102.
/// Kan ha brukerdefinerte underkontoer (5+ siffer).
/// </summary>
public class Konto : AuditableEntity
{
    /// <summary>
    /// Firesifret kontonummer (1000-8999). For underkontoer: 5-6 siffer.
    /// </summary>
    public string Kontonummer { get; set; } = default!;

    /// <summary>
    /// Norsk kontonavn.
    /// </summary>
    public string Navn { get; set; } = default!;

    /// <summary>
    /// Engelsk kontonavn for SAF-T eksport.
    /// </summary>
    public string? NavnEn { get; set; }

    /// <summary>
    /// Kontotype (Eiendel, Gjeld, Egenkapital, Inntekt, Kostnad).
    /// </summary>
    public Kontotype Kontotype { get; set; }

    /// <summary>
    /// Normalbalanse (Debet eller Kredit).
    /// </summary>
    public Normalbalanse Normalbalanse { get; set; }

    /// <summary>
    /// FK til kontogruppen.
    /// </summary>
    public Guid KontogruppeId { get; set; }
    public Kontogruppe Kontogruppe { get; set; } = default!;

    /// <summary>
    /// SAF-T StandardAccountID. Obligatorisk mapping til Skatteetatens standardkonto.
    /// </summary>
    public string StandardAccountId { get; set; } = default!;

    /// <summary>
    /// SAF-T GroupingCategory (RF-1167, RF-1175, RF-1323).
    /// </summary>
    public GrupperingsKategori? GrupperingsKategori { get; set; }

    /// <summary>
    /// SAF-T GroupingCode innenfor valgt kategori.
    /// </summary>
    public string? GrupperingsKode { get; set; }

    /// <summary>
    /// Om kontoen er aktiv og kan brukes til bokforing.
    /// </summary>
    public bool ErAktiv { get; set; } = true;

    /// <summary>
    /// Systemkonto kan ikke slettes.
    /// </summary>
    public bool ErSystemkonto { get; set; }

    /// <summary>
    /// Om kontoen kan bokfores direkte (false = kun summekonto/overskrift).
    /// </summary>
    public bool ErBokforbar { get; set; } = true;

    /// <summary>
    /// Standard MVA-kode for denne kontoen. Brukes som default ved bokforing.
    /// </summary>
    public string? StandardMvaKode { get; set; }

    /// <summary>
    /// Fritekst beskrivelse / notat for kontoen.
    /// </summary>
    public string? Beskrivelse { get; set; }

    /// <summary>
    /// FK til overordnet konto (for underkontoer).
    /// </summary>
    public Guid? OverordnetKontoId { get; set; }
    public Konto? OverordnetKonto { get; set; }

    /// <summary>
    /// Eventuelle underkontoer.
    /// </summary>
    public ICollection<Konto> Underkontoer { get; set; } = new List<Konto>();

    /// <summary>
    /// Om kontoen krever at en avdeling/kostnadssted angis ved bokforing.
    /// </summary>
    public bool KreverAvdeling { get; set; }

    /// <summary>
    /// Om kontoen krever at et prosjekt angis ved bokforing.
    /// </summary>
    public bool KreverProsjekt { get; set; }

    // --- Avledede egenskaper ---

    /// <summary>
    /// Kontoklasse avledet fra forste siffer i kontonummer.
    /// </summary>
    public Kontoklasse Kontoklasse => (Kontoklasse)int.Parse(Kontonummer[..1]);

    /// <summary>
    /// Om dette er en balansepost (klasse 1-2) eller resultatpost (klasse 3-8).
    /// </summary>
    public bool ErBalansekonto => Kontoklasse is Kontoklasse.Eiendeler
                                    or Kontoklasse.EgenkapitalOgGjeld;

    /// <summary>
    /// Om dette er en underkonto (5+ siffer).
    /// </summary>
    public bool ErUnderkonto => Kontonummer.Length > 4;
}
