namespace Regnskap.Domain.Features.Fakturering;

/// <summary>
/// Status for en faktura i faktureringsprosessen.
/// </summary>
public enum FakturaStatus
{
    /// <summary>Utkast -- kan fortsatt redigeres.</summary>
    Utkast,

    /// <summary>Godkjent -- klar for utsendelse.</summary>
    Godkjent,

    /// <summary>Utstedt -- faktura er sendt, bilag opprettet.</summary>
    Utstedt,

    /// <summary>Kreditert -- hel eller delvis kreditnota utstedt.</summary>
    Kreditert,

    /// <summary>Kansellert -- utkast som ble forkastet (aldri utstedt).</summary>
    Kansellert
}

/// <summary>
/// Type fakturadokument.
/// </summary>
public enum FakturaDokumenttype
{
    /// <summary>Ordinaer faktura. EHF InvoiceTypeCode = 380.</summary>
    Faktura,

    /// <summary>Kreditnota. EHF InvoiceTypeCode = 381.</summary>
    Kreditnota
}

/// <summary>
/// Leveringsformat for faktura.
/// </summary>
public enum FakturaLeveringsformat
{
    /// <summary>PDF sendt per e-post.</summary>
    Epost,

    /// <summary>EHF/PEPPOL elektronisk faktura.</summary>
    Ehf,

    /// <summary>Papir (utskrift).</summary>
    Papir,

    /// <summary>Kun lagret i systemet (f.eks. kontantsalg).</summary>
    Intern
}

/// <summary>
/// Rabattype per linje.
/// </summary>
public enum RabattType
{
    /// <summary>Prosent rabatt.</summary>
    Prosent,

    /// <summary>Fast belop rabatt.</summary>
    Belop
}

/// <summary>
/// Enhet for fakturalinje.
/// Mapper til UBL unitCode (UN/ECE Recommendation 20).
/// </summary>
public enum Enhet
{
    /// <summary>Stykk (EA).</summary>
    Stykk,

    /// <summary>Timer (HUR).</summary>
    Timer,

    /// <summary>Kilogram (KGM).</summary>
    Kilogram,

    /// <summary>Liter (LTR).</summary>
    Liter,

    /// <summary>Meter (MTR).</summary>
    Meter,

    /// <summary>Kvadratmeter (MTK).</summary>
    Kvadratmeter,

    /// <summary>Pakke (PK).</summary>
    Pakke,

    /// <summary>Maaned (MON).</summary>
    Maaned,

    /// <summary>Dag (DAY).</summary>
    Dag
}
