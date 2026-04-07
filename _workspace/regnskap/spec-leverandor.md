# Spesifikasjon: Leverandorreskontro (Accounts Payable)

**Modul:** Leverandorreskontro
**Status:** Komplett spesifikasjon
**Avhengigheter:** Kontoplan, Hovedbok, Bilagsregistrering, MVA
**SAF-T-seksjon:** MasterFiles > Suppliers, GeneralLedgerEntries (SupplierID per linje)
**Bokforingsloven:** Krav om leverandorspesifikasjon (5, 3-1)

---

## Datamodell

### Enums

```csharp
namespace Regnskap.Domain.Features.Leverandorreskontro;

/// <summary>
/// Betalingsstatus for en leverandorfaktura.
/// </summary>
public enum FakturaStatus
{
    /// <summary>Registrert men ikke godkjent for betaling.</summary>
    Registrert,

    /// <summary>Godkjent for betaling (attestert).</summary>
    Godkjent,

    /// <summary>Inkludert i et betalingsforslag.</summary>
    IBetalingsforslag,

    /// <summary>Betalingsfil er generert og sendt til bank.</summary>
    SendtTilBank,

    /// <summary>Betalt i sin helhet.</summary>
    Betalt,

    /// <summary>Delvis betalt.</summary>
    DelvisBetalt,

    /// <summary>Kreditert (kreditnota mottatt).</summary>
    Kreditert,

    /// <summary>Omstridt/sperret for betaling.</summary>
    Sperret
}

/// <summary>
/// Status for et betalingsforslag.
/// </summary>
public enum BetalingsforslagStatus
{
    /// <summary>Forslaget er opprettet, kan redigeres.</summary>
    Utkast,

    /// <summary>Godkjent, klar for filoppretting.</summary>
    Godkjent,

    /// <summary>pain.001 fil generert.</summary>
    FilGenerert,

    /// <summary>Sendt til bank.</summary>
    SendtTilBank,

    /// <summary>Bekreftet utfort av bank.</summary>
    Utfort,

    /// <summary>Avvist av bank (helt eller delvis).</summary>
    Avvist,

    /// <summary>Kansellert for sending.</summary>
    Kansellert
}

/// <summary>
/// Type leverandortransaksjon.
/// </summary>
public enum LeverandorTransaksjonstype
{
    Faktura,
    Kreditnota,
    Betaling,
    Forskudd
}

/// <summary>
/// Betalingsbetingelser.
/// </summary>
public enum Betalingsbetingelse
{
    Netto10,
    Netto14,
    Netto20,
    Netto30,
    Netto45,
    Netto60,
    Netto90,
    Kontant,
    Egendefinert
}
```

### Entity: Leverandor

Representerer en leverandor i leverandorregisteret. Mapper til SAF-T MasterFiles > Suppliers > Supplier.

```csharp
namespace Regnskap.Domain.Features.Leverandorreskontro;

using Regnskap.Domain.Common;

/// <summary>
/// Leverandor (supplier). Grunndata for leverandorreskontro.
/// Mapper til SAF-T: MasterFiles > Suppliers > Supplier.
/// Bokforingsforskriften 3-1: leverandorspesifikasjon krever leverandorkode, navn og org.nr.
/// </summary>
public class Leverandor : AuditableEntity
{
    /// <summary>
    /// Unikt leverandornummer. Brukes som intern referanse.
    /// Mapper til SAF-T SupplierID.
    /// </summary>
    public string Leverandornummer { get; set; } = default!;

    /// <summary>
    /// Leverandorens fulle navn.
    /// Mapper til SAF-T Supplier/Name.
    /// </summary>
    public string Navn { get; set; } = default!;

    /// <summary>
    /// Organisasjonsnummer (9 siffer). Obligatorisk for norske foretak.
    /// Bokforingsforskriften 3-1: leverandorspesifikasjon MA inneholde org.nr.
    /// Mapper til SAF-T Supplier/TaxRegistration/TaxRegistrationNumber.
    /// </summary>
    public string? Organisasjonsnummer { get; set; }

    /// <summary>
    /// Om leverandoren er MVA-registrert (org.nr etterfulgt av "MVA").
    /// </summary>
    public bool ErMvaRegistrert { get; set; }

    // --- Adresse ---

    /// <summary>
    /// Gateadresse linje 1.
    /// Mapper til SAF-T Supplier/Address/StreetName.
    /// </summary>
    public string? Adresse1 { get; set; }

    /// <summary>
    /// Gateadresse linje 2.
    /// Mapper til SAF-T Supplier/Address/AdditionalAddressDetail.
    /// </summary>
    public string? Adresse2 { get; set; }

    /// <summary>
    /// Postnummer.
    /// Mapper til SAF-T Supplier/Address/PostalCode.
    /// </summary>
    public string? Postnummer { get; set; }

    /// <summary>
    /// Poststed.
    /// Mapper til SAF-T Supplier/Address/City.
    /// </summary>
    public string? Poststed { get; set; }

    /// <summary>
    /// Landkode (ISO 3166-1 alpha-2, f.eks. "NO").
    /// Mapper til SAF-T Supplier/Address/Country.
    /// </summary>
    public string Landkode { get; set; } = "NO";

    // --- Kontakt ---

    /// <summary>
    /// Kontaktperson hos leverandor.
    /// Mapper til SAF-T Supplier/Contact/ContactPerson/FirstName + LastName.
    /// </summary>
    public string? Kontaktperson { get; set; }

    /// <summary>
    /// Telefonnummer.
    /// Mapper til SAF-T Supplier/Contact/Telephone.
    /// </summary>
    public string? Telefon { get; set; }

    /// <summary>
    /// E-postadresse.
    /// Mapper til SAF-T Supplier/Contact/Email.
    /// </summary>
    public string? Epost { get; set; }

    // --- Betaling ---

    /// <summary>
    /// Standard betalingsbetingelse for denne leverandoren.
    /// </summary>
    public Betalingsbetingelse Betalingsbetingelse { get; set; } = Betalingsbetingelse.Netto30;

    /// <summary>
    /// Antall dager for egendefinert betalingsbetingelse.
    /// Kun brukt nar Betalingsbetingelse = Egendefinert.
    /// </summary>
    public int? EgendefinertBetalingsfrist { get; set; }

    /// <summary>
    /// Leverandorens bankkonto (norsk 11-sifret BBAN).
    /// Brukes i betalingsfil (pain.001).
    /// Mapper til SAF-T Supplier/BankAccount/BankAccountNumber.
    /// </summary>
    public string? Bankkontonummer { get; set; }

    /// <summary>
    /// IBAN for utenlandske betalinger.
    /// Mapper til SAF-T Supplier/BankAccount/IBANNumber.
    /// </summary>
    public string? Iban { get; set; }

    /// <summary>
    /// BIC/SWIFT for utenlandske betalinger.
    /// Mapper til SAF-T Supplier/BankAccount/BIC.
    /// </summary>
    public string? Bic { get; set; }

    /// <summary>
    /// Bankens navn.
    /// </summary>
    public string? Banknavn { get; set; }

    // --- Bokforing ---

    /// <summary>
    /// Standard motkonto (kostnadskonto) for denne leverandoren.
    /// Brukes som default ved fakturaregistrering.
    /// </summary>
    public Guid? StandardKontoId { get; set; }

    /// <summary>
    /// Standard MVA-kode for kjop fra denne leverandoren.
    /// </summary>
    public string? StandardMvaKode { get; set; }

    /// <summary>
    /// Valutakode (ISO 4217). Default "NOK".
    /// Mapper til SAF-T Supplier/BankAccount/CurrencyCode.
    /// </summary>
    public string Valutakode { get; set; } = "NOK";

    /// <summary>
    /// Om leverandoren er aktiv.
    /// </summary>
    public bool ErAktiv { get; set; } = true;

    /// <summary>
    /// Om leverandoren er sperret for nye bestillinger/fakturaer.
    /// </summary>
    public bool ErSperret { get; set; }

    /// <summary>
    /// Fritekst notat.
    /// </summary>
    public string? Notat { get; set; }

    // --- SAF-T ---

    /// <summary>
    /// SAF-T SupplierID. Typisk lik Leverandornummer.
    /// </summary>
    public string SaftSupplierId => Leverandornummer;

    // --- Navigasjon ---

    /// <summary>
    /// Alle fakturaer fra denne leverandoren.
    /// </summary>
    public ICollection<LeverandorFaktura> Fakturaer { get; set; } = new List<LeverandorFaktura>();
}
```

**EF Core-konfigurasjon:**
- Unique index pa `Leverandornummer`
- Unique index pa `Organisasjonsnummer` (WHERE NOT NULL) -- hindrer duplikater
- Index pa `Navn` for sok
- `Leverandornummer` maks 20 tegn
- `Organisasjonsnummer` maks 9 tegn, regex-validering `^\d{9}$`
- `Bankkontonummer` maks 11 tegn, regex `^\d{11}$`
- `Iban` maks 34 tegn
- `Bic` maks 11 tegn
- `Landkode` maks 2 tegn

### Entity: LeverandorFaktura

Representerer en inngaende faktura (eller kreditnota) fra en leverandor.

```csharp
namespace Regnskap.Domain.Features.Leverandorreskontro;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Inngaende faktura fra leverandor.
/// Representerer en apne post i leverandorreskontro.
/// Hver faktura genererer et Bilag i hovedboken med posteringer:
///   Debet: kostnadskonto(er) + inngaende MVA
///   Kredit: 2400 Leverandorgjeld
/// </summary>
public class LeverandorFaktura : AuditableEntity
{
    /// <summary>
    /// FK til leverandoren.
    /// </summary>
    public Guid LeverandorId { get; set; }
    public Leverandor Leverandor { get; set; } = default!;

    /// <summary>
    /// Leverandorens fakturanummer (ekstern referanse).
    /// Bokforingsforskriften 5-5: dokumentasjon av kjop.
    /// </summary>
    public string EksternFakturanummer { get; set; } = default!;

    /// <summary>
    /// Internt fakturanummer (fortlopende i systemet).
    /// </summary>
    public int InternNummer { get; set; }

    /// <summary>
    /// Type transaksjon (Faktura eller Kreditnota).
    /// </summary>
    public LeverandorTransaksjonstype Type { get; set; } = LeverandorTransaksjonstype.Faktura;

    /// <summary>
    /// Fakturadato (utstedelsesdato fra leverandor).
    /// </summary>
    public DateOnly Fakturadato { get; set; }

    /// <summary>
    /// Mottaksdato (dato faktura ble mottatt/registrert).
    /// </summary>
    public DateOnly Mottaksdato { get; set; }

    /// <summary>
    /// Forfallsdato for betaling.
    /// Beregnes fra fakturadato + betalingsbetingelse.
    /// </summary>
    public DateOnly Forfallsdato { get; set; }

    /// <summary>
    /// Beskrivelse / hva fakturaen gjelder.
    /// </summary>
    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// Totalbelop ekskl. MVA.
    /// </summary>
    public Belop BelopEksMva { get; set; }

    /// <summary>
    /// MVA-belop.
    /// </summary>
    public Belop MvaBelop { get; set; }

    /// <summary>
    /// Totalbelop inkl. MVA (det som skal betales).
    /// </summary>
    public Belop BelopInklMva { get; set; }

    /// <summary>
    /// Gjenstaaende belop (utstaaende). Reduseres ved betaling.
    /// Nar 0: fakturaen er fullt betalt.
    /// </summary>
    public Belop GjenstaendeBelop { get; set; }

    /// <summary>
    /// Betalingsstatus.
    /// </summary>
    public FakturaStatus Status { get; set; } = FakturaStatus.Registrert;

    /// <summary>
    /// KID-nummer for betaling (fra leverandorens faktura).
    /// Brukes i pain.001 betalingsfil.
    /// </summary>
    public string? KidNummer { get; set; }

    /// <summary>
    /// Valutakode (ISO 4217).
    /// </summary>
    public string Valutakode { get; set; } = "NOK";

    /// <summary>
    /// Valutakurs brukt ved bokforing (for utenlandske fakturaer).
    /// </summary>
    public decimal? Valutakurs { get; set; }

    /// <summary>
    /// FK til bilaget som ble opprettet ved registrering av fakturaen.
    /// </summary>
    public Guid? BilagId { get; set; }
    public Bilag? Bilag { get; set; }

    /// <summary>
    /// Referanse til kreditnota (hvis denne fakturaen er kreditert).
    /// </summary>
    public Guid? KreditnotaForFakturaId { get; set; }
    public LeverandorFaktura? KreditnotaForFaktura { get; set; }

    /// <summary>
    /// Om fakturaen er sperret for betaling.
    /// </summary>
    public bool ErSperret { get; set; }

    /// <summary>
    /// Arsak til sperring.
    /// </summary>
    public string? SperreArsak { get; set; }

    // --- Navigasjon ---

    /// <summary>
    /// Fakturalinjer med kontering.
    /// </summary>
    public ICollection<LeverandorFakturaLinje> Linjer { get; set; } = new List<LeverandorFakturaLinje>();

    /// <summary>
    /// Betalinger knyttet til denne fakturaen.
    /// </summary>
    public ICollection<LeverandorBetaling> Betalinger { get; set; } = new List<LeverandorBetaling>();

    // --- Avledede egenskaper ---

    /// <summary>
    /// Om fakturaen er forfalt.
    /// </summary>
    public bool ErForfalt(DateOnly iDag) => Forfallsdato < iDag && GjenstaendeBelop.Verdi > 0;

    /// <summary>
    /// Antall dager forfalt.
    /// </summary>
    public int DagerForfalt(DateOnly iDag) =>
        ErForfalt(iDag) ? iDag.DayNumber - Forfallsdato.DayNumber : 0;

    /// <summary>
    /// Alderskategori for aldersfordeling.
    /// </summary>
    public Alderskategori HentAlderskategori(DateOnly iDag)
    {
        var dager = DagerForfalt(iDag);
        return dager switch
        {
            0 => Alderskategori.IkkeForfalt,
            <= 30 => Alderskategori.Dager0Til30,
            <= 60 => Alderskategori.Dager31Til60,
            <= 90 => Alderskategori.Dager61Til90,
            _ => Alderskategori.Over90Dager
        };
    }
}
```

**EF Core-konfigurasjon:**
- Unique index pa `(LeverandorId, EksternFakturanummer)` -- forhindrer dobbeltregistrering
- Unique index pa `InternNummer`
- Index pa `Forfallsdato` -- for betalingsforslag
- Index pa `Status` -- for apne poster
- Index pa `BilagId`
- `EksternFakturanummer` maks 50 tegn
- `KidNummer` maks 25 tegn
- `Valutakode` maks 3 tegn
- `BelopInklMva`, `BelopEksMva`, `MvaBelop`, `GjenstaendeBelop` med precision(18, 2)

### Entity: LeverandorFakturaLinje

Konteringslinje for en leverandorfaktura.

```csharp
namespace Regnskap.Domain.Features.Leverandorreskontro;

using Regnskap.Domain.Common;

/// <summary>
/// Konteringslinje for en leverandorfaktura.
/// Hver linje representerer en kostnad som fordeles pa en konto med MVA-kode.
/// </summary>
public class LeverandorFakturaLinje : AuditableEntity
{
    /// <summary>
    /// FK til fakturaen.
    /// </summary>
    public Guid LeverandorFakturaId { get; set; }
    public LeverandorFaktura LeverandorFaktura { get; set; } = default!;

    /// <summary>
    /// Linjenummer (1, 2, 3...).
    /// </summary>
    public int Linjenummer { get; set; }

    /// <summary>
    /// FK til kostnadskontoen (debet-konto).
    /// </summary>
    public Guid KontoId { get; set; }

    /// <summary>
    /// Kontonummer denormalisert.
    /// </summary>
    public string Kontonummer { get; set; } = default!;

    /// <summary>
    /// Beskrivelse av linjen.
    /// </summary>
    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// Nettobelop (eks. MVA) for denne linjen.
    /// </summary>
    public Belop Belop { get; set; }

    /// <summary>
    /// MVA-kode for denne linjen.
    /// </summary>
    public string? MvaKode { get; set; }

    /// <summary>
    /// MVA-sats brukt (snapshot).
    /// </summary>
    public decimal? MvaSats { get; set; }

    /// <summary>
    /// Beregnet MVA-belop for denne linjen.
    /// </summary>
    public Belop? MvaBelop { get; set; }

    /// <summary>
    /// Avdelingskode/kostnadssted.
    /// </summary>
    public string? Avdelingskode { get; set; }

    /// <summary>
    /// Prosjektkode.
    /// </summary>
    public string? Prosjektkode { get; set; }
}
```

### Entity: LeverandorBetaling

Representerer en betaling (hel eller delvis) av en leverandorfaktura.

```csharp
namespace Regnskap.Domain.Features.Leverandorreskontro;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Betaling knyttet til en leverandorfaktura.
/// Genererer bilag:
///   Debet: 2400 Leverandorgjeld
///   Kredit: 1920 Bank
/// </summary>
public class LeverandorBetaling : AuditableEntity
{
    /// <summary>
    /// FK til fakturaen som betales.
    /// </summary>
    public Guid LeverandorFakturaId { get; set; }
    public LeverandorFaktura LeverandorFaktura { get; set; } = default!;

    /// <summary>
    /// FK til betalingsforslaget (hvis generert via forslag).
    /// </summary>
    public Guid? BetalingsforslagId { get; set; }
    public Betalingsforslag? Betalingsforslag { get; set; }

    /// <summary>
    /// Betalingsdato.
    /// </summary>
    public DateOnly Betalingsdato { get; set; }

    /// <summary>
    /// Betalt belop.
    /// </summary>
    public Belop Belop { get; set; }

    /// <summary>
    /// FK til bilaget som ble opprettet for betalingen.
    /// </summary>
    public Guid? BilagId { get; set; }
    public Bilag? Bilag { get; set; }

    /// <summary>
    /// Bankreferanse / transaksjonsnummer fra bank.
    /// </summary>
    public string? Bankreferanse { get; set; }

    /// <summary>
    /// Betalingsmetode (bank, kontant, etc.).
    /// </summary>
    public string Betalingsmetode { get; set; } = "Bank";
}
```

### Entity: Betalingsforslag

Betalingsforslag samler fakturaer som skal betales.

```csharp
namespace Regnskap.Domain.Features.Leverandorreskontro;

using Regnskap.Domain.Common;

/// <summary>
/// Betalingsforslag for leverandorfakturaer.
/// Genereres basert pa forfallsdato og brukervalg.
/// Kan resultere i en pain.001 betalingsfil.
/// </summary>
public class Betalingsforslag : AuditableEntity
{
    /// <summary>
    /// Unikt forslagsnummer.
    /// </summary>
    public int Forslagsnummer { get; set; }

    /// <summary>
    /// Beskrivelse av forslaget.
    /// </summary>
    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// Dato forslaget ble opprettet.
    /// </summary>
    public DateOnly Opprettdato { get; set; }

    /// <summary>
    /// Onsket betalingsdato (ReqdExctnDt i pain.001).
    /// </summary>
    public DateOnly Betalingsdato { get; set; }

    /// <summary>
    /// Forfallsdato-grense: inkluder alle fakturaer med forfall t.o.m. denne datoen.
    /// </summary>
    public DateOnly ForfallTilOgMed { get; set; }

    /// <summary>
    /// Status for forslaget.
    /// </summary>
    public BetalingsforslagStatus Status { get; set; } = BetalingsforslagStatus.Utkast;

    /// <summary>
    /// Totalbelop i forslaget.
    /// </summary>
    public Belop TotalBelop { get; set; }

    /// <summary>
    /// Antall betalinger i forslaget.
    /// </summary>
    public int AntallBetalinger { get; set; }

    /// <summary>
    /// FK til bankkontoen betalingene skal trekkes fra.
    /// </summary>
    public Guid? FraBankkontoId { get; set; }

    /// <summary>
    /// IBAN/BBAN for debetkonto (snapshot for pain.001).
    /// </summary>
    public string? FraKontonummer { get; set; }

    /// <summary>
    /// BIC for debetbank.
    /// </summary>
    public string? FraBic { get; set; }

    /// <summary>
    /// Betalingsfilreferanse (filnavn/ID for generert pain.001).
    /// </summary>
    public string? BetalingsfilReferanse { get; set; }

    /// <summary>
    /// Tidspunkt filen ble generert.
    /// </summary>
    public DateTime? FilGenererTidspunkt { get; set; }

    /// <summary>
    /// Tidspunkt filen ble sendt til bank.
    /// </summary>
    public DateTime? SendtTilBankTidspunkt { get; set; }

    /// <summary>
    /// Godkjent av (bruker).
    /// </summary>
    public string? GodkjentAv { get; set; }

    /// <summary>
    /// Tidspunkt for godkjenning.
    /// </summary>
    public DateTime? GodkjentTidspunkt { get; set; }

    // --- Navigasjon ---

    /// <summary>
    /// Linjene i betalingsforslaget.
    /// </summary>
    public ICollection<BetalingsforslagLinje> Linjer { get; set; } = new List<BetalingsforslagLinje>();
}
```

### Entity: BetalingsforslagLinje

En enkelt betaling i et betalingsforslag.

```csharp
namespace Regnskap.Domain.Features.Leverandorreskontro;

using Regnskap.Domain.Common;

/// <summary>
/// En linje i et betalingsforslag. Representerer betaling av en faktura.
/// Mapper til en CdtTrfTxInf i pain.001.
/// </summary>
public class BetalingsforslagLinje : AuditableEntity
{
    /// <summary>
    /// FK til betalingsforslaget.
    /// </summary>
    public Guid BetalingsforslagId { get; set; }
    public Betalingsforslag Betalingsforslag { get; set; } = default!;

    /// <summary>
    /// FK til fakturaen som betales.
    /// </summary>
    public Guid LeverandorFakturaId { get; set; }
    public LeverandorFaktura LeverandorFaktura { get; set; } = default!;

    /// <summary>
    /// FK til leverandoren (denormalisert for enklere fil-generering).
    /// </summary>
    public Guid LeverandorId { get; set; }
    public Leverandor Leverandor { get; set; } = default!;

    /// <summary>
    /// Belop som betales for denne fakturaen.
    /// </summary>
    public Belop Belop { get; set; }

    /// <summary>
    /// Mottakers kontonummer (BBAN, kopieres fra leverandor).
    /// </summary>
    public string? MottakerKontonummer { get; set; }

    /// <summary>
    /// Mottakers IBAN.
    /// </summary>
    public string? MottakerIban { get; set; }

    /// <summary>
    /// Mottakers bank BIC.
    /// </summary>
    public string? MottakerBic { get; set; }

    /// <summary>
    /// KID-nummer for betalingen (fra fakturaen).
    /// Mapper til pain.001 RmtInf/Strd/CdtrRefInf/Ref.
    /// </summary>
    public string? KidNummer { get; set; }

    /// <summary>
    /// Fritekstmelding til mottaker (brukes nar KID mangler).
    /// </summary>
    public string? Melding { get; set; }

    /// <summary>
    /// EndToEndId for pain.001 sporing.
    /// </summary>
    public string? EndToEndId { get; set; }

    /// <summary>
    /// Om betalingen er inkludert (kan eksluderes manuelt).
    /// </summary>
    public bool ErInkludert { get; set; } = true;

    /// <summary>
    /// Resultatstatus etter bankbehandling.
    /// </summary>
    public bool? ErUtfort { get; set; }

    /// <summary>
    /// Feilmelding fra bank (ved avvisning).
    /// </summary>
    public string? Feilmelding { get; set; }
}
```

### Value Object: Alderskategori

```csharp
namespace Regnskap.Domain.Features.Leverandorreskontro;

/// <summary>
/// Alderskategorier for aldersfordeling av apne poster.
/// </summary>
public enum Alderskategori
{
    IkkeForfalt,
    Dager0Til30,
    Dager31Til60,
    Dager61Til90,
    Over90Dager
}
```

---

## API-kontrakt

### Leverandorregister

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/leverandorer` | Hent alle leverandorer (paginert) |
| GET | `/api/leverandorer/{id}` | Hent en leverandor |
| GET | `/api/leverandorer/sok?q={query}` | Sok etter leverandor (navn/org.nr) |
| POST | `/api/leverandorer` | Opprett ny leverandor |
| PUT | `/api/leverandorer/{id}` | Oppdater leverandor |
| DELETE | `/api/leverandorer/{id}` | Soft-delete leverandor |
| GET | `/api/leverandorer/{id}/utskrift?fraDato={}&tilDato={}` | Leverandorutskrift |
| GET | `/api/leverandorer/{id}/saldo` | Hent saldo for leverandor |

#### OpprettLeverandorRequest

```csharp
public record OpprettLeverandorRequest(
    string Leverandornummer,
    string Navn,
    string? Organisasjonsnummer,
    bool ErMvaRegistrert,
    string? Adresse1,
    string? Adresse2,
    string? Postnummer,
    string? Poststed,
    string Landkode,
    string? Kontaktperson,
    string? Telefon,
    string? Epost,
    Betalingsbetingelse Betalingsbetingelse,
    int? EgendefinertBetalingsfrist,
    string? Bankkontonummer,
    string? Iban,
    string? Bic,
    Guid? StandardKontoId,
    string? StandardMvaKode
);
```

**Validering:**
- `Leverandornummer`: Pakreves, maks 20 tegn, unik
- `Navn`: Pakreves, maks 200 tegn
- `Organisasjonsnummer`: Null eller 9 siffer, gyldig MOD11-sjekk
- `Bankkontonummer`: Null eller 11 siffer, gyldig MOD11-sjekk
- `Iban`: Null eller gyldig IBAN-format
- `Postnummer`: 4 siffer (norske)
- `Landkode`: 2 tegn ISO 3166-1 alpha-2
- `EgendefinertBetalingsfrist`: Pakreves nar Betalingsbetingelse = Egendefinert, 1-365 dager

#### LeverandorDto

```csharp
public record LeverandorDto(
    Guid Id,
    string Leverandornummer,
    string Navn,
    string? Organisasjonsnummer,
    bool ErMvaRegistrert,
    string? Adresse1,
    string? Postnummer,
    string? Poststed,
    string Landkode,
    string? Kontaktperson,
    string? Epost,
    Betalingsbetingelse Betalingsbetingelse,
    string? Bankkontonummer,
    string? Iban,
    bool ErAktiv,
    Belop Saldo
);
```

### Inngaende faktura

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/leverandorfakturaer` | Hent fakturaer (paginert, filtrering) |
| GET | `/api/leverandorfakturaer/{id}` | Hent fakturadetaljer |
| POST | `/api/leverandorfakturaer` | Registrer inngaende faktura |
| PUT | `/api/leverandorfakturaer/{id}/godkjenn` | Godkjenn faktura for betaling |
| PUT | `/api/leverandorfakturaer/{id}/sperr` | Sperr faktura |
| PUT | `/api/leverandorfakturaer/{id}/opphev-sperring` | Opphev sperring |
| GET | `/api/leverandorfakturaer/apne-poster` | Alle apne poster |
| GET | `/api/leverandorfakturaer/aldersfordeling?dato={}` | Aldersfordeling |

#### RegistrerFakturaRequest

```csharp
public record RegistrerFakturaRequest(
    Guid LeverandorId,
    string EksternFakturanummer,
    LeverandorTransaksjonstype Type,
    DateOnly Fakturadato,
    DateOnly? Forfallsdato,       // Null = beregnes fra betalingsbetingelse
    string Beskrivelse,
    string? KidNummer,
    string Valutakode,
    decimal? Valutakurs,
    List<FakturaLinjeRequest> Linjer
);

public record FakturaLinjeRequest(
    Guid KontoId,
    string Beskrivelse,
    decimal Belop,               // Netto eks. MVA
    string? MvaKode,
    string? Avdelingskode,
    string? Prosjektkode
);
```

**Validering:**
- `LeverandorId`: Leverandor ma eksistere og vaere aktiv
- `EksternFakturanummer`: Pakreves, unik per leverandor
- `Fakturadato`: Kan ikke vaere i fremtiden
- `Linjer`: Minimum 1 linje
- `Linjer[].Belop`: Positivt belop
- `Linjer[].KontoId`: Konto ma eksistere, vaere bokforbar, og vaere en kostnadskonto (klasse 4-7) eller eiendel (klasse 1)
- `MvaKode`: Ma vaere gyldig inngaende MVA-kode (retning = Inngaende)

### Betalingsforslag

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/betalingsforslag` | Hent alle forslag (paginert) |
| GET | `/api/betalingsforslag/{id}` | Hent forslagsdetaljer |
| POST | `/api/betalingsforslag/generer` | Generer nytt forslag |
| PUT | `/api/betalingsforslag/{id}/godkjenn` | Godkjenn forslaget |
| PUT | `/api/betalingsforslag/{id}/linjer/{linjeId}/ekskluder` | Ekskluder en linje |
| PUT | `/api/betalingsforslag/{id}/linjer/{linjeId}/inkluder` | Inkluder en linje |
| POST | `/api/betalingsforslag/{id}/generer-fil` | Generer pain.001 betalingsfil |
| GET | `/api/betalingsforslag/{id}/fil` | Last ned generert fil |
| PUT | `/api/betalingsforslag/{id}/marker-sendt` | Marker som sendt til bank |
| DELETE | `/api/betalingsforslag/{id}` | Kanseller forslag (kun Utkast) |

#### GenererBetalingsforslagRequest

```csharp
public record GenererBetalingsforslagRequest(
    DateOnly ForfallTilOgMed,
    DateOnly Betalingsdato,
    Guid? FraBankkontoId,
    string? FraKontonummer,
    bool InkluderAllerede Godkjente,   // Bare godkjente fakturaer, eller ogsaa Registrerte?
    List<Guid>? LeverandorIder         // Null = alle leverandorer
);
```

### Rapporter

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/rapporter/leverandor/aldersfordeling?dato={}` | Aldersfordeling alle leverandorer |
| GET | `/api/rapporter/leverandor/aldersfordeling/{leverandorId}?dato={}` | Aldersfordeling en leverandor |
| GET | `/api/rapporter/leverandor/apne-poster?dato={}` | Alle apne poster |
| GET | `/api/rapporter/leverandor/utskrift/{leverandorId}?fra={}&til={}` | Leverandorutskrift |
| GET | `/api/rapporter/leverandor/leverandorspesifikasjon?ar={}&periode={}` | Bokforingsforskriften 3-1 |
| GET | `/api/rapporter/leverandor/forfallsliste?fraDato={}&tilDato={}` | Forfallsoversikt |

#### AldersfordelingDto

```csharp
public record AldersfordelingDto(
    List<AldersfordelingLeverandorDto> Leverandorer,
    AldersfordelingSummaryDto Totalt,
    DateOnly Dato
);

public record AldersfordelingLeverandorDto(
    Guid LeverandorId,
    string Leverandornummer,
    string Navn,
    Belop IkkeForfalt,
    Belop Dager0Til30,
    Belop Dager31Til60,
    Belop Dager61Til90,
    Belop Over90Dager,
    Belop Totalt
);

public record AldersfordelingSummaryDto(
    Belop IkkeForfalt,
    Belop Dager0Til30,
    Belop Dager31Til60,
    Belop Dager61Til90,
    Belop Over90Dager,
    Belop Totalt
);
```

#### LeverandorutskriftDto

```csharp
public record LeverandorutskriftDto(
    Guid LeverandorId,
    string Leverandornummer,
    string Navn,
    Belop InngaaendeSaldo,
    List<LeverandorutskriftLinjeDto> Transaksjoner,
    Belop UtgaaendeSaldo,
    DateOnly FraDato,
    DateOnly TilDato
);

public record LeverandorutskriftLinjeDto(
    DateOnly Dato,
    string BilagsId,
    string Beskrivelse,
    LeverandorTransaksjonstype Type,
    Belop? Debet,
    Belop? Kredit,
    Belop Saldo,
    string? EksternFakturanummer
);
```

---

## Forretningsregler

### FR-L01: Registrering av inngaende faktura

1. Ved registrering av en inngaende faktura skal systemet automatisk opprette et Bilag med folgende posteringer:
   - **Debet** pa kostnadskonto(er) angitt i fakturalinjer (nettobelop)
   - **Debet** pa inngaende MVA-konto (1600-serien) for hver MVA-linje
   - **Kredit** pa konto 2400 Leverandorgjeld (bruttobelop inkl. MVA)
2. Alle posteringer i bilaget skal ha `LeverandorId` satt for SAF-T-sporing.
3. Bilaget opprettes i bilagserie "IF" (Inngaende Faktura) med BilagType.InngaendeFaktura.
4. `GjenstaendeBelop` settes lik `BelopInklMva` ved registrering.

**Eksempel:** Faktura fra Leverandor A pa NOK 10 000 + 25% MVA
- Debet 6300 Husleie: 10 000
- Debet 2710 Inngaende MVA: 2 500
- Kredit 2400 Leverandorgjeld: 12 500
- GjenstaendeBelop = 12 500

### FR-L02: Forfallsdato-beregning

1. Hvis `Forfallsdato` ikke er angitt, beregnes den fra `Fakturadato` + betalingsbetingelse:
   - Netto10 = 10 dager, Netto14 = 14 dager, osv.
   - Kontant = forfallsdato = fakturadato
   - Egendefinert = leverandorens `EgendefinertBetalingsfrist` dager
2. Forfallsdato som faller pa lordag/sondag flyttes til neste mandag.

### FR-L03: Kreditnota-handtering

1. En kreditnota registreres med `Type = Kreditnota` og positive belop.
2. Bilagsposteringer er speilvendt:
   - **Debet** 2400 Leverandorgjeld (bruttobelop)
   - **Kredit** kostnadskonto(er) (nettobelop)
   - **Kredit** inngaende MVA-konto (MVA-belop)
3. Kreditnota kan knyttes til en spesifikk faktura via `KreditnotaForFakturaId`.
4. Nar kreditnota knyttes, reduseres `GjenstaendeBelop` pa opprinnelig faktura.

### FR-L04: Betalingsforslag

1. Generering av betalingsforslag velger alle fakturaer der:
   - `Status` = Godkjent
   - `Forfallsdato` <= `ForfallTilOgMed`
   - `GjenstaendeBelop` > 0
   - `ErSperret` = false
   - Leverandor er ikke sperret
2. Fakturaer grupperes per leverandor.
3. Hver faktura faar en BetalingsforslagLinje med leverandorens bankkontoinfo.
4. Brukeren kan ekskludere/inkludere individuelle linjer for godkjenning.
5. Totalbelop og antall oppdateres automatisk.

### FR-L05: Betalingsfil (pain.001)

1. Generering krever at forslaget har status Godkjent.
2. pain.001 XML genereres ihht ISO 20022:
   - GrpHdr: MsgId (unik GUID), CreDtTm, NbOfTxs, CtrlSum
   - PmtInf: en blokk per betalingsdato/debetkonto-kombinasjon
   - CdtTrfTxInf: en per betaling med EndToEndId, belop, mottakerinfo
3. KID-nummer legges i RmtInf/Strd/CdtrRefInf/Ref.
4. Nar KID mangler, legges fakturanummer i RmtInf/Ustrd.
5. Norske innenlandsbetalinger bruker BBAN; utenlandske bruker IBAN+BIC.
6. Filen lagres og referanse legges pa Betalingsforslag.

### FR-L06: Registrering av betaling

1. Nar betaling bekreftes (manuelt eller via bankavstemmning):
   - Opprett LeverandorBetaling med belop og referanse
   - Reduser `GjenstaendeBelop` pa fakturaen
   - Oppdater `Status`: Betalt (GjenstaendeBelop = 0) eller DelvisBetalt
   - Opprett Bilag: Debet 2400, Kredit 1920 (bank)
2. Alle posteringer i bilaget skal ha `LeverandorId`.

**Eksempel:** Betaling av faktura pa 12 500:
- Debet 2400 Leverandorgjeld: 12 500
- Kredit 1920 Bank: 12 500
- GjenstaendeBelop: 12 500 -> 0
- Status: Betalt

### FR-L07: Apne poster

1. En apne post er en faktura der `GjenstaendeBelop` > 0 og `Status` ikke er Betalt eller Kreditert.
2. Apne poster-rapporten summerer per leverandor og totalt.
3. Sum apne poster MA stemme med saldo pa konto 2400 Leverandorgjeld.
4. Avvik mellom reskontroen og hovedbok-saldo krever forklaring (NBS 5).

### FR-L08: Aldersfordeling

1. Aldersfordeling beregnes per faktura basert pa dager mellom forfallsdato og valgt rapportdato.
2. Kategorier: Ikke forfalt, 0-30 dager, 31-60 dager, 61-90 dager, 90+ dager.
3. Kreditnotaer trekkes fra den tilknyttede fakturaens kategori, eller plasseres i "Ikke forfalt" hvis ikke knyttet.
4. Delbetalinger reduserer belop i fakturaens alderskategori.

### FR-L09: Leverandorutskrift

1. Viser alle transaksjoner for en leverandor i en periode.
2. Inkluderer inngaaende saldo ved periodens start.
3. Viser hver transaksjon: faktura, kreditnota, betaling med bilagsreferanse.
4. Lopende saldo beregnes for hver linje.
5. Mapper til bokforingsforskriften 3-1 leverandorspesifikasjon.

### FR-L10: Duplikatkontroll

1. Systemet skal advare nar en ny faktura registreres med samme `EksternFakturanummer` fra samme leverandor.
2. Systemet skal advare nar belop og dato matcher en eksisterende faktura fra samme leverandor (mulig duplikat).
3. Duplikatsjekk er en advarsel, ikke en blokkering (unntak: eksakt EksternFakturanummer-duplikat blokkeres).

### FR-L11: Leverandornummer-tildeling

1. Leverandornummer kan tildeles manuelt eller automatisk.
2. Automatisk: neste ledige nummer i sekvensen (konfigurerbart startpunkt, typisk 10001).
3. Leverandornummer kan ikke gjenbrukes etter sletting (soft delete).

---

## MVA-handtering

### Relevante MVA-koder

| Kode | SAF-T | Sats | Bruk i leverandorreskontro |
|------|-------|------|---------------------------|
| 1 (inn) | 1 | 25% | Inngaende MVA, full fradrag (vanlige kjop) |
| 11 (inn) | 11 | 15% | Inngaende MVA, naringsmiddel |
| 13 (inn) | 13 | 12% | Inngaende MVA, persontransport etc. |
| 14 | 14 | 25% | Inngaende MVA, snudd avregning (import tjenester) |
| 0 | 0 | 0% | MVA-fritt kjop |

### Beregningslogikk

1. MVA beregnes per fakturalinje: `MvaBelop = Belop * MvaSats / 100`
2. Avrunding til narest ore (2 desimaler).
3. Total MVA pa faktura = sum av MVA per linje.
4. `BelopInklMva = BelopEksMva + MvaBelop` (pa fakturaniva).
5. Ved snudd avregning (kode 14/15): bade inngaende og utgaende MVA bokfores.

### SAF-T Mapping

- Posteringer mot leverandorkonto (2400) ma ha `SupplierID` i SAF-T Line.
- MVA-informasjon per linje i TaxInformation: TaxType="MVA", TaxCode, TaxPercentage, TaxBase, TaxAmount.
- Leverandor eksporteres i MasterFiles > Suppliers med opening/closing balanse (v1.30).

---

## Avhengigheter

| Modul | Interface/Service | Bruk |
|-------|-------------------|------|
| Kontoplan | `IKontoplanRepository` | Hent kontoer for kontering |
| Hovedbok | `Bilag`, `Postering` | Opprette bilag med posteringer |
| Bilagsregistrering | `IBilagRegistreringService` | Opprette og bokfore bilag |
| MVA | MvaKode-entitet | Hente MVA-satser og kontoer |

### Interfaces definert av denne modulen

```csharp
namespace Regnskap.Application.Features.Leverandorreskontro;

public interface ILeverandorService
{
    // Leverandorregister
    Task<LeverandorDto> OpprettAsync(OpprettLeverandorRequest request, CancellationToken ct = default);
    Task<LeverandorDto> OppdaterAsync(Guid id, OppdaterLeverandorRequest request, CancellationToken ct = default);
    Task<LeverandorDto> HentAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<LeverandorDto>> SokAsync(LeverandorSokRequest request, CancellationToken ct = default);
    Task SlettAsync(Guid id, CancellationToken ct = default);
}

public interface ILeverandorFakturaService
{
    // Fakturaregistrering
    Task<LeverandorFakturaDto> RegistrerFakturaAsync(RegistrerFakturaRequest request, CancellationToken ct = default);
    Task<LeverandorFakturaDto> GodkjennAsync(Guid id, CancellationToken ct = default);
    Task<LeverandorFakturaDto> SperrAsync(Guid id, string arsak, CancellationToken ct = default);
    Task<LeverandorFakturaDto> OpphevSperringAsync(Guid id, CancellationToken ct = default);
    Task<LeverandorFakturaDto> HentAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<LeverandorFakturaDto>> SokAsync(FakturaSokRequest request, CancellationToken ct = default);

    // Rapporter
    Task<List<LeverandorFakturaDto>> HentApnePosterAsync(DateOnly? dato = null, CancellationToken ct = default);
    Task<AldersfordelingDto> HentAldersfordelingAsync(DateOnly dato, CancellationToken ct = default);
    Task<LeverandorutskriftDto> HentUtskriftAsync(Guid leverandorId, DateOnly fraDato, DateOnly tilDato, CancellationToken ct = default);
}

public interface IBetalingsforslagService
{
    Task<BetalingsforslagDto> GenererAsync(GenererBetalingsforslagRequest request, CancellationToken ct = default);
    Task<BetalingsforslagDto> GodkjennAsync(Guid id, string godkjentAv, CancellationToken ct = default);
    Task<byte[]> GenererFilAsync(Guid id, CancellationToken ct = default);
    Task MarkerSendtAsync(Guid id, CancellationToken ct = default);
    Task KansellerAsync(Guid id, CancellationToken ct = default);
    Task EkskluderLinjeAsync(Guid forslagId, Guid linjeId, CancellationToken ct = default);
    Task InkluderLinjeAsync(Guid forslagId, Guid linjeId, CancellationToken ct = default);
}

public interface ILeverandorReskontroRepository
{
    // Leverandor
    Task<Leverandor?> HentLeverandorAsync(Guid id, CancellationToken ct = default);
    Task<Leverandor?> HentLeverandorMedNummerAsync(string leverandornummer, CancellationToken ct = default);
    Task<bool> LeverandornummerEksistererAsync(string leverandornummer, CancellationToken ct = default);
    Task LeggTilLeverandorAsync(Leverandor leverandor, CancellationToken ct = default);
    Task OppdaterLeverandorAsync(Leverandor leverandor, CancellationToken ct = default);

    // Faktura
    Task<LeverandorFaktura?> HentFakturaAsync(Guid id, CancellationToken ct = default);
    Task<bool> EksternFakturaDuplikatAsync(Guid leverandorId, string eksternNummer, CancellationToken ct = default);
    Task LeggTilFakturaAsync(LeverandorFaktura faktura, CancellationToken ct = default);
    Task OppdaterFakturaAsync(LeverandorFaktura faktura, CancellationToken ct = default);
    Task<List<LeverandorFaktura>> HentApnePosterAsync(DateOnly? dato = null, CancellationToken ct = default);
    Task<List<LeverandorFaktura>> HentForfalteFakturaerAsync(DateOnly forfallTilOgMed, CancellationToken ct = default);

    // Betalingsforslag
    Task<Betalingsforslag?> HentBetalingsforslagAsync(Guid id, CancellationToken ct = default);
    Task LeggTilBetalingsforslagAsync(Betalingsforslag forslag, CancellationToken ct = default);
    Task OppdaterBetalingsforslagAsync(Betalingsforslag forslag, CancellationToken ct = default);

    Task LagreEndringerAsync(CancellationToken ct = default);
}
```
