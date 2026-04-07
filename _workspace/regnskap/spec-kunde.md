# Spesifikasjon: Kundereskontro (Accounts Receivable)

**Modul:** Kundereskontro
**Status:** Komplett spesifikasjon
**Avhengigheter:** Kontoplan, Hovedbok, Bilagsregistrering, MVA
**SAF-T-seksjon:** MasterFiles > Customers, GeneralLedgerEntries (CustomerID per linje)
**Bokforingsloven:** Krav om kundespesifikasjon (5, 3-1)

---

## Datamodell

### Enums

```csharp
namespace Regnskap.Domain.Features.Kundereskontro;

/// <summary>
/// Betalingsstatus for en kundefaktura.
/// </summary>
public enum KundeFakturaStatus
{
    /// <summary>Faktura er utstedt.</summary>
    Utstedt,

    /// <summary>Betalt i sin helhet.</summary>
    Betalt,

    /// <summary>Delvis betalt.</summary>
    DelvisBetalt,

    /// <summary>Kreditert (kreditnota utstedt).</summary>
    Kreditert,

    /// <summary>Forste purring sendt.</summary>
    Purring1,

    /// <summary>Andre purring sendt.</summary>
    Purring2,

    /// <summary>Tredje purring sendt (siste varsel).</summary>
    Purring3,

    /// <summary>Sendt til inkasso.</summary>
    Inkasso,

    /// <summary>Konstatert tap / avskrevet.</summary>
    Tap
}

/// <summary>
/// Type kundetransaksjon.
/// </summary>
public enum KundeTransaksjonstype
{
    Faktura,
    Kreditnota,
    Innbetaling,
    Purregebyr,
    Tap
}

/// <summary>
/// Betalingsbetingelser for kunder.
/// </summary>
public enum KundeBetalingsbetingelse
{
    Netto10,
    Netto14,
    Netto20,
    Netto30,
    Netto45,
    Netto60,
    Kontant,
    Forskudd,
    Egendefinert
}

/// <summary>
/// KID-algoritme.
/// </summary>
public enum KidAlgoritme
{
    MOD10,
    MOD11
}

/// <summary>
/// Purringsstatus / -type.
/// </summary>
public enum PurringType
{
    /// <summary>Forste purring (betalingspaaminnelse).</summary>
    Purring1,

    /// <summary>Andre purring (med gebyr).</summary>
    Purring2,

    /// <summary>Tredje og siste purring / inkassovarsel.</summary>
    Purring3Inkassovarsel
}

/// <summary>
/// Alderskategorier for aldersfordeling av apne poster.
/// Speilet av Leverandorreskontro.Alderskategori for konsistens.
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

### Entity: Kunde

Representerer en kunde i kunderegisteret. Mapper til SAF-T MasterFiles > Customers > Customer.

```csharp
namespace Regnskap.Domain.Features.Kundereskontro;

using Regnskap.Domain.Common;

/// <summary>
/// Kunde (customer). Grunndata for kundereskontro.
/// Mapper til SAF-T: MasterFiles > Customers > Customer.
/// Bokforingsforskriften 3-1: kundespesifikasjon krever kundekode og navn.
/// </summary>
public class Kunde : AuditableEntity
{
    /// <summary>
    /// Unikt kundenummer. Brukes som intern referanse.
    /// Mapper til SAF-T CustomerID.
    /// </summary>
    public string Kundenummer { get; set; } = default!;

    /// <summary>
    /// Kundens fulle navn.
    /// Mapper til SAF-T Customer/Name.
    /// </summary>
    public string Navn { get; set; } = default!;

    /// <summary>
    /// Organisasjonsnummer (9 siffer) for bedriftskunder.
    /// Mapper til SAF-T Customer/TaxRegistration/TaxRegistrationNumber.
    /// </summary>
    public string? Organisasjonsnummer { get; set; }

    /// <summary>
    /// Fodselsnummer/D-nummer (11 siffer) for privatpersoner.
    /// </summary>
    public string? Fodselsnummer { get; set; }

    /// <summary>
    /// Om kunden er en bedrift (true) eller privatperson (false).
    /// </summary>
    public bool ErBedrift { get; set; } = true;

    // --- Adresse ---

    /// <summary>
    /// Gateadresse linje 1.
    /// Mapper til SAF-T Customer/Address/StreetName.
    /// </summary>
    public string? Adresse1 { get; set; }

    /// <summary>
    /// Gateadresse linje 2.
    /// Mapper til SAF-T Customer/Address/AdditionalAddressDetail.
    /// </summary>
    public string? Adresse2 { get; set; }

    /// <summary>
    /// Postnummer.
    /// Mapper til SAF-T Customer/Address/PostalCode.
    /// </summary>
    public string? Postnummer { get; set; }

    /// <summary>
    /// Poststed.
    /// Mapper til SAF-T Customer/Address/City.
    /// </summary>
    public string? Poststed { get; set; }

    /// <summary>
    /// Landkode (ISO 3166-1 alpha-2).
    /// Mapper til SAF-T Customer/Address/Country.
    /// </summary>
    public string Landkode { get; set; } = "NO";

    // --- Kontakt ---

    /// <summary>
    /// Kontaktperson.
    /// Mapper til SAF-T Customer/Contact/ContactPerson.
    /// </summary>
    public string? Kontaktperson { get; set; }

    /// <summary>
    /// Telefonnummer.
    /// Mapper til SAF-T Customer/Contact/Telephone.
    /// </summary>
    public string? Telefon { get; set; }

    /// <summary>
    /// E-postadresse.
    /// Mapper til SAF-T Customer/Contact/Email.
    /// </summary>
    public string? Epost { get; set; }

    // --- Betaling ---

    /// <summary>
    /// Standard betalingsbetingelse.
    /// </summary>
    public KundeBetalingsbetingelse Betalingsbetingelse { get; set; } = KundeBetalingsbetingelse.Netto14;

    /// <summary>
    /// Antall dager for egendefinert betalingsbetingelse.
    /// </summary>
    public int? EgendefinertBetalingsfrist { get; set; }

    // --- KID ---

    /// <summary>
    /// KID-algoritme for denne kunden (MOD10 eller MOD11).
    /// Arves fra systeminnstilling hvis null.
    /// </summary>
    public KidAlgoritme? KidAlgoritme { get; set; }

    // --- Bokforing ---

    /// <summary>
    /// Standard inntektskonto for denne kunden.
    /// Brukes som default ved fakturering.
    /// </summary>
    public Guid? StandardKontoId { get; set; }

    /// <summary>
    /// Standard MVA-kode for salg til denne kunden.
    /// </summary>
    public string? StandardMvaKode { get; set; }

    /// <summary>
    /// Valutakode (ISO 4217). Default "NOK".
    /// </summary>
    public string Valutakode { get; set; } = "NOK";

    /// <summary>
    /// Kredittgrense for kunden (0 = ingen grense).
    /// </summary>
    public Belop Kredittgrense { get; set; } = Belop.Null;

    /// <summary>
    /// Om kunden er aktiv.
    /// </summary>
    public bool ErAktiv { get; set; } = true;

    /// <summary>
    /// Om kunden er sperret for nye fakturaer.
    /// </summary>
    public bool ErSperret { get; set; }

    /// <summary>
    /// Fritekst notat.
    /// </summary>
    public string? Notat { get; set; }

    // --- EHF / PEPPOL ---

    /// <summary>
    /// PEPPOL-deltaker-ID (for elektronisk fakturering).
    /// Format: "0192:" + org.nr (norske bedrifter).
    /// </summary>
    public string? PeppolId { get; set; }

    /// <summary>
    /// Om kunden kan motta EHF-faktura.
    /// </summary>
    public bool KanMottaEhf { get; set; }

    // --- SAF-T ---

    /// <summary>
    /// SAF-T CustomerID. Typisk lik Kundenummer.
    /// </summary>
    public string SaftCustomerId => Kundenummer;

    // --- Navigasjon ---

    /// <summary>
    /// Alle fakturaer til denne kunden.
    /// </summary>
    public ICollection<KundeFaktura> Fakturaer { get; set; } = new List<KundeFaktura>();
}
```

**EF Core-konfigurasjon:**
- Unique index pa `Kundenummer`
- Unique index pa `Organisasjonsnummer` (WHERE NOT NULL)
- Index pa `Navn` for sok
- Index pa `Fodselsnummer` (WHERE NOT NULL)
- `Kundenummer` maks 20 tegn
- `Organisasjonsnummer` maks 9 tegn, regex `^\d{9}$`
- `Fodselsnummer` maks 11 tegn, regex `^\d{11}$`
- `Landkode` maks 2 tegn
- `PeppolId` maks 50 tegn

### Entity: KundeFaktura

Representerer en utgaaende faktura (eller kreditnota) til en kunde.

```csharp
namespace Regnskap.Domain.Features.Kundereskontro;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Utgaaende faktura til kunde.
/// Representerer en apne post i kundereskontro.
/// Hver faktura genererer et Bilag i hovedboken med posteringer:
///   Debet: 1500 Kundefordringer
///   Kredit: inntektskonto(er) + utgaaende MVA
/// </summary>
public class KundeFaktura : AuditableEntity
{
    /// <summary>
    /// FK til kunden.
    /// </summary>
    public Guid KundeId { get; set; }
    public Kunde Kunde { get; set; } = default!;

    /// <summary>
    /// Fakturanummer (fortlopende, ihht bokforingsforskriften 5-1-1).
    /// Internt tildelt, unik, sammenhengende sekvens.
    /// </summary>
    public int Fakturanummer { get; set; }

    /// <summary>
    /// Type transaksjon.
    /// </summary>
    public KundeTransaksjonstype Type { get; set; } = KundeTransaksjonstype.Faktura;

    /// <summary>
    /// Fakturadato (utstedelsesdato).
    /// Bokforingsforskriften 5-1-1: obligatorisk felt.
    /// </summary>
    public DateOnly Fakturadato { get; set; }

    /// <summary>
    /// Forfallsdato.
    /// Bokforingsforskriften 5-1-1: obligatorisk felt.
    /// </summary>
    public DateOnly Forfallsdato { get; set; }

    /// <summary>
    /// Leveringsdato / tjenesteperiode.
    /// Bokforingsforskriften 5-1-1: dato for levering.
    /// </summary>
    public DateOnly? Leveringsdato { get; set; }

    /// <summary>
    /// Beskrivelse / ordrereferanse.
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
    /// Totalbelop inkl. MVA.
    /// </summary>
    public Belop BelopInklMva { get; set; }

    /// <summary>
    /// Gjenstaaende belop. Reduseres ved innbetaling.
    /// </summary>
    public Belop GjenstaendeBelop { get; set; }

    /// <summary>
    /// Betalingsstatus.
    /// </summary>
    public KundeFakturaStatus Status { get; set; } = KundeFakturaStatus.Utstedt;

    /// <summary>
    /// KID-nummer generert for denne fakturaen.
    /// Brukes av kunden for betaling og matching.
    /// </summary>
    public string? KidNummer { get; set; }

    /// <summary>
    /// Valutakode.
    /// </summary>
    public string Valutakode { get; set; } = "NOK";

    /// <summary>
    /// Valutakurs (for utenlandske fakturaer).
    /// </summary>
    public decimal? Valutakurs { get; set; }

    /// <summary>
    /// FK til bilaget opprettet ved fakturering.
    /// </summary>
    public Guid? BilagId { get; set; }
    public Bilag? Bilag { get; set; }

    /// <summary>
    /// Referanse til kreditnota (hvis faktura er kreditert).
    /// </summary>
    public Guid? KreditnotaForFakturaId { get; set; }
    public KundeFaktura? KreditnotaForFaktura { get; set; }

    /// <summary>
    /// Ekstern referanse / ordrenummer fra kunden.
    /// </summary>
    public string? EksternReferanse { get; set; }

    /// <summary>
    /// Kundens bestillingsnummer (for EHF BuyerReference).
    /// </summary>
    public string? Bestillingsnummer { get; set; }

    /// <summary>
    /// Antall purringer sendt.
    /// </summary>
    public int AntallPurringer { get; set; }

    /// <summary>
    /// Dato siste purring ble sendt.
    /// </summary>
    public DateOnly? SistePurringDato { get; set; }

    /// <summary>
    /// Totalt purregebyr paalopet.
    /// </summary>
    public Belop PurregebyrTotalt { get; set; } = Belop.Null;

    // --- Navigasjon ---

    /// <summary>
    /// Fakturalinjer.
    /// </summary>
    public ICollection<KundeFakturaLinje> Linjer { get; set; } = new List<KundeFakturaLinje>();

    /// <summary>
    /// Innbetalinger knyttet til denne fakturaen.
    /// </summary>
    public ICollection<KundeInnbetaling> Innbetalinger { get; set; } = new List<KundeInnbetaling>();

    /// <summary>
    /// Purringer sendt for denne fakturaen.
    /// </summary>
    public ICollection<Purring> Purringer { get; set; } = new List<Purring>();

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
- Unique index pa `Fakturanummer` -- fortlopende, kontrollerbar sekvens
- Index pa `(KundeId, Fakturadato)` -- for kundeutskrift
- Index pa `Forfallsdato` -- for purring
- Index pa `Status` -- for apne poster
- Index pa `KidNummer` -- for innbetalingsmatching
- Index pa `BilagId`
- `KidNummer` maks 25 tegn
- `Valutakode` maks 3 tegn
- `EksternReferanse` maks 100 tegn
- `Bestillingsnummer` maks 50 tegn
- Belop-felter med precision(18, 2)

### Entity: KundeFakturaLinje

```csharp
namespace Regnskap.Domain.Features.Kundereskontro;

using Regnskap.Domain.Common;

/// <summary>
/// Fakturalinje for en kundefaktura.
/// Bokforingsforskriften 5-1-1: beskrivelse av varer/tjenester, antall, enhetspris.
/// </summary>
public class KundeFakturaLinje : AuditableEntity
{
    /// <summary>
    /// FK til fakturaen.
    /// </summary>
    public Guid KundeFakturaId { get; set; }
    public KundeFaktura KundeFaktura { get; set; } = default!;

    /// <summary>
    /// Linjenummer (1, 2, 3...).
    /// </summary>
    public int Linjenummer { get; set; }

    /// <summary>
    /// FK til inntektskontoen (kredit-konto).
    /// </summary>
    public Guid KontoId { get; set; }

    /// <summary>
    /// Kontonummer denormalisert.
    /// </summary>
    public string Kontonummer { get; set; } = default!;

    /// <summary>
    /// Beskrivelse av vare/tjeneste.
    /// Bokforingsforskriften 5-1-1: klar identifikasjon.
    /// </summary>
    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// Antall enheter.
    /// </summary>
    public decimal Antall { get; set; } = 1;

    /// <summary>
    /// Enhetspris ekskl. MVA.
    /// Bokforingsforskriften 5-1-1: enhetspris.
    /// </summary>
    public Belop Enhetspris { get; set; }

    /// <summary>
    /// Nettobelop (Antall * Enhetspris).
    /// </summary>
    public Belop Belop { get; set; }

    /// <summary>
    /// MVA-kode.
    /// </summary>
    public string? MvaKode { get; set; }

    /// <summary>
    /// MVA-sats brukt (snapshot).
    /// </summary>
    public decimal? MvaSats { get; set; }

    /// <summary>
    /// Beregnet MVA-belop.
    /// </summary>
    public Belop? MvaBelop { get; set; }

    /// <summary>
    /// Rabatt i prosent (0-100).
    /// </summary>
    public decimal Rabatt { get; set; }

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

### Entity: KundeInnbetaling

```csharp
namespace Regnskap.Domain.Features.Kundereskontro;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Innbetaling fra kunde, koblet til en faktura.
/// Genererer bilag:
///   Debet: 1920 Bank
///   Kredit: 1500 Kundefordringer
/// </summary>
public class KundeInnbetaling : AuditableEntity
{
    /// <summary>
    /// FK til fakturaen som betales.
    /// </summary>
    public Guid KundeFakturaId { get; set; }
    public KundeFaktura KundeFaktura { get; set; } = default!;

    /// <summary>
    /// Dato innbetaling ble mottatt.
    /// </summary>
    public DateOnly Innbetalingsdato { get; set; }

    /// <summary>
    /// Innbetalt belop.
    /// </summary>
    public Belop Belop { get; set; }

    /// <summary>
    /// FK til bilaget opprettet for innbetalingen.
    /// </summary>
    public Guid? BilagId { get; set; }
    public Bilag? Bilag { get; set; }

    /// <summary>
    /// Bankreferanse / transaksjonsnummer.
    /// </summary>
    public string? Bankreferanse { get; set; }

    /// <summary>
    /// KID-nummer brukt ved innbetaling (fra CAMT.053 eller manuelt).
    /// </summary>
    public string? KidNummer { get; set; }

    /// <summary>
    /// Om innbetalingen ble automatisk matchet via KID.
    /// </summary>
    public bool ErAutoMatchet { get; set; }

    /// <summary>
    /// Betalingsmetode (bank, kontant, kort, etc.).
    /// </summary>
    public string Betalingsmetode { get; set; } = "Bank";
}
```

### Entity: Purring

```csharp
namespace Regnskap.Domain.Features.Kundereskontro;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Purring (betalingspaaminnelse) sendt til kunde for forfalt faktura.
/// Inkassoloven 9 regulerer purregebyrer:
///   - 1/10 av inkassosatsen for skriftlig purring (2026: ca NOK 70)
///   - Maks 3 purringer for gebyr
///   - Minimum 14 dager mellom purringer
/// </summary>
public class Purring : AuditableEntity
{
    /// <summary>
    /// FK til fakturaen som purres.
    /// </summary>
    public Guid KundeFakturaId { get; set; }
    public KundeFaktura KundeFaktura { get; set; } = default!;

    /// <summary>
    /// Type purring (1., 2. eller 3. purring).
    /// </summary>
    public PurringType Type { get; set; }

    /// <summary>
    /// Purringsdato.
    /// </summary>
    public DateOnly Purringsdato { get; set; }

    /// <summary>
    /// Ny betalingsfrist gitt i purringen.
    /// Minimum 14 dager fra purringsdato.
    /// </summary>
    public DateOnly NyForfallsdato { get; set; }

    /// <summary>
    /// Purregebyr belop. 0 for forste betalingspaaminnelse.
    /// </summary>
    public Belop Gebyr { get; set; } = Belop.Null;

    /// <summary>
    /// Forsinkelsesrente beregnet (arsrente ihht Forsinkelsesrenteloven).
    /// </summary>
    public Belop Forsinkelsesrente { get; set; } = Belop.Null;

    /// <summary>
    /// FK til bilaget for purregebyr (hvis gebyr > 0).
    /// Debet: 1500 Kundefordringer, Kredit: 3400 Purregebyr-inntekt.
    /// </summary>
    public Guid? GebyrBilagId { get; set; }
    public Bilag? GebyrBilag { get; set; }

    /// <summary>
    /// Om purringen er sendt (e-post/brev).
    /// </summary>
    public bool ErSendt { get; set; }

    /// <summary>
    /// Tidspunkt purringen ble sendt.
    /// </summary>
    public DateTime? SendtTidspunkt { get; set; }

    /// <summary>
    /// Sendemetode (Epost, Brev, EHF).
    /// </summary>
    public string? Sendemetode { get; set; }
}
```

### KID-nummer Value Objects og hjelpeklasse

```csharp
namespace Regnskap.Domain.Features.Kundereskontro;

/// <summary>
/// KID-nummer generator og validator.
/// Stotter bade MOD10 (Luhn) og MOD11 algoritmer.
/// KID: 2-25 siffer, siste siffer er kontrollsiffer.
/// </summary>
public static class KidGenerator
{
    /// <summary>
    /// Generer KID-nummer fra kundenummer og fakturanummer.
    /// Typisk format: [kundenummer (6 siffer)][fakturanummer (6 siffer)][kontrollsiffer].
    /// </summary>
    public static string Generer(string kundenummer, int fakturanummer, KidAlgoritme algoritme)
    {
        var payload = $"{kundenummer.PadLeft(6, '0')}{fakturanummer.ToString().PadLeft(6, '0')}";
        var kontrollsiffer = algoritme switch
        {
            KidAlgoritme.MOD10 => BeregnMod10(payload),
            KidAlgoritme.MOD11 => BeregnMod11(payload),
            _ => throw new ArgumentOutOfRangeException(nameof(algoritme))
        };

        if (kontrollsiffer < 0)
            throw new InvalidOperationException(
                $"MOD11 gir ugyldig kontrollsiffer for payload '{payload}'. Bruk neste fakturanummer.");

        return payload + kontrollsiffer;
    }

    /// <summary>
    /// Valider et KID-nummer.
    /// </summary>
    public static bool Valider(string kid, KidAlgoritme algoritme)
    {
        if (string.IsNullOrWhiteSpace(kid) || kid.Length < 2 || kid.Length > 25)
            return false;

        if (!kid.All(char.IsDigit))
            return false;

        var payload = kid[..^1];
        var oppgittKontroll = int.Parse(kid[^1..]);

        var beregnetKontroll = algoritme switch
        {
            KidAlgoritme.MOD10 => BeregnMod10(payload),
            KidAlgoritme.MOD11 => BeregnMod11(payload),
            _ => -1
        };

        return beregnetKontroll == oppgittKontroll;
    }

    /// <summary>
    /// MOD10 (Luhn) kontrollsiffer.
    /// </summary>
    public static int BeregnMod10(string payload)
    {
        // Fra hoyre, doble annethvert siffer
        var sum = 0;
        for (int i = payload.Length - 1, vekt = 2; i >= 0; i--, vekt = vekt == 2 ? 1 : 2)
        {
            var produkt = (payload[i] - '0') * vekt;
            sum += produkt > 9 ? produkt - 9 : produkt;
        }
        return (10 - (sum % 10)) % 10;
    }

    /// <summary>
    /// MOD11 kontrollsiffer. Returnerer -1 hvis rest = 10 (ugyldig).
    /// </summary>
    public static int BeregnMod11(string payload)
    {
        var vekter = new[] { 2, 3, 4, 5, 6, 7 };
        var sum = 0;
        for (int i = payload.Length - 1, v = 0; i >= 0; i--, v++)
        {
            sum += (payload[i] - '0') * vekter[v % vekter.Length];
        }
        var rest = sum % 11;
        if (rest == 0) return 0;
        if (rest == 1) return -1; // 11 - 1 = 10, ugyldig
        return 11 - rest;
    }
}
```

---

## API-kontrakt

### Kunderegister

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/kunder` | Hent alle kunder (paginert) |
| GET | `/api/kunder/{id}` | Hent en kunde |
| GET | `/api/kunder/sok?q={query}` | Sok etter kunde (navn/org.nr/kundenr) |
| POST | `/api/kunder` | Opprett ny kunde |
| PUT | `/api/kunder/{id}` | Oppdater kunde |
| DELETE | `/api/kunder/{id}` | Soft-delete kunde |
| GET | `/api/kunder/{id}/utskrift?fraDato={}&tilDato={}` | Kundeutskrift |
| GET | `/api/kunder/{id}/saldo` | Hent saldo for kunde |

#### OpprettKundeRequest

```csharp
public record OpprettKundeRequest(
    string Kundenummer,
    string Navn,
    bool ErBedrift,
    string? Organisasjonsnummer,
    string? Fodselsnummer,
    string? Adresse1,
    string? Adresse2,
    string? Postnummer,
    string? Poststed,
    string Landkode,
    string? Kontaktperson,
    string? Telefon,
    string? Epost,
    KundeBetalingsbetingelse Betalingsbetingelse,
    int? EgendefinertBetalingsfrist,
    Guid? StandardKontoId,
    string? StandardMvaKode,
    decimal? Kredittgrense,
    string? PeppolId,
    bool KanMottaEhf
);
```

**Validering:**
- `Kundenummer`: Pakreves, maks 20 tegn, unik
- `Navn`: Pakreves, maks 200 tegn
- `Organisasjonsnummer`: Null eller 9 siffer, gyldig MOD11. Pakreves nar ErBedrift = true og Landkode = "NO"
- `Fodselsnummer`: Null eller 11 siffer. Bare for privatpersoner
- `Postnummer`: 4 siffer (norske)
- `Landkode`: 2 tegn ISO 3166-1
- `EgendefinertBetalingsfrist`: Pakreves nar Betalingsbetingelse = Egendefinert, 1-365
- `Kredittgrense`: >= 0

### Fakturaoppfolging

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/kundefakturaer` | Hent fakturaer (paginert, filtrering) |
| GET | `/api/kundefakturaer/{id}` | Hent fakturadetaljer |
| POST | `/api/kundefakturaer` | Registrer utgaaende faktura |
| GET | `/api/kundefakturaer/apne-poster` | Alle apne poster |
| GET | `/api/kundefakturaer/aldersfordeling?dato={}` | Aldersfordeling |

#### RegistrerFakturaRequest

```csharp
public record RegistrerKundeFakturaRequest(
    Guid KundeId,
    KundeTransaksjonstype Type,
    DateOnly Fakturadato,
    DateOnly? Forfallsdato,
    DateOnly? Leveringsdato,
    string Beskrivelse,
    string? EksternReferanse,
    string? Bestillingsnummer,
    string Valutakode,
    decimal? Valutakurs,
    List<KundeFakturaLinjeRequest> Linjer
);

public record KundeFakturaLinjeRequest(
    Guid KontoId,
    string Beskrivelse,
    decimal Antall,
    decimal Enhetspris,
    string? MvaKode,
    decimal Rabatt,
    string? Avdelingskode,
    string? Prosjektkode
);
```

**Validering:**
- `KundeId`: Kunde ma eksistere og vaere aktiv, ikke sperret
- `Fakturadato`: Ikke i fremtiden
- `Linjer`: Minimum 1 linje
- `Linjer[].Antall`: > 0
- `Linjer[].Enhetspris`: > 0
- `Linjer[].KontoId`: Konto ma eksistere, vaere bokforbar, vaere inntektskonto (klasse 3)
- `Linjer[].Rabatt`: 0-100
- `MvaKode`: Ma vaere gyldig utgaaende MVA-kode (retning = Utgaende)
- Kontroller kredittgrense: eksisterende saldo + ny faktura <= Kredittgrense (nar > 0)

### Innbetalingsregistrering

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| POST | `/api/kundeinnbetalinger` | Registrer innbetaling manuelt |
| POST | `/api/kundeinnbetalinger/match-kid` | Match innbetaling mot KID |
| POST | `/api/kundeinnbetalinger/importer-camt053` | Importer innbetalinger fra CAMT.053 |
| GET | `/api/kundeinnbetalinger/umatchede` | Vis umatchede innbetalinger |

#### RegistrerInnbetalingRequest

```csharp
public record RegistrerInnbetalingRequest(
    Guid KundeFakturaId,
    DateOnly Innbetalingsdato,
    decimal Belop,
    string? Bankreferanse,
    string? KidNummer,
    string Betalingsmetode
);

public record MatchKidRequest(
    string KidNummer,
    decimal Belop,
    DateOnly Innbetalingsdato,
    string? Bankreferanse
);
```

### KID-nummer

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/kid/generer?kundeId={}&fakturanummer={}` | Generer KID for faktura |
| POST | `/api/kid/valider` | Valider et KID-nummer |

### Purring

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/purringer/forslag?dato={}` | Generer purreforslag (preview) |
| POST | `/api/purringer/opprett` | Opprett purringer fra forslag |
| POST | `/api/purringer/{id}/send` | Marker purring som sendt |
| GET | `/api/purringer` | Hent alle purringer (paginert) |

#### PurreforslagRequest

```csharp
public record PurreforslagRequest(
    DateOnly Dato,
    int MinimumDagerForfalt,       // Default 14
    bool InkluderPurring1,
    bool InkluderPurring2,
    bool InkluderPurring3,
    List<Guid>? KundeIder           // Null = alle kunder
);
```

### Rapporter

| Metode | URL | Beskrivelse |
|--------|-----|-------------|
| GET | `/api/rapporter/kunde/aldersfordeling?dato={}` | Aldersfordeling alle kunder |
| GET | `/api/rapporter/kunde/aldersfordeling/{kundeId}?dato={}` | Aldersfordeling en kunde |
| GET | `/api/rapporter/kunde/apne-poster?dato={}` | Alle apne poster |
| GET | `/api/rapporter/kunde/utskrift/{kundeId}?fra={}&til={}` | Kundeutskrift |
| GET | `/api/rapporter/kunde/kundespesifikasjon?ar={}&periode={}` | Bokforingsforskriften 3-1 |
| GET | `/api/rapporter/kunde/forfallsliste?fraDato={}&tilDato={}` | Forfallsoversikt |

#### AldersfordelingDto (Kunde)

```csharp
public record KundeAldersfordelingDto(
    List<AldersfordelingKundeDto> Kunder,
    AldersfordelingSummaryDto Totalt,
    DateOnly Dato
);

public record AldersfordelingKundeDto(
    Guid KundeId,
    string Kundenummer,
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

#### KundeutskriftDto

```csharp
public record KundeutskriftDto(
    Guid KundeId,
    string Kundenummer,
    string Navn,
    Belop InngaaendeSaldo,
    List<KundeutskriftLinjeDto> Transaksjoner,
    Belop UtgaaendeSaldo,
    DateOnly FraDato,
    DateOnly TilDato
);

public record KundeutskriftLinjeDto(
    DateOnly Dato,
    string BilagsId,
    string Beskrivelse,
    KundeTransaksjonstype Type,
    Belop? Debet,
    Belop? Kredit,
    Belop Saldo,
    int? Fakturanummer,
    string? KidNummer
);
```

---

## Forretningsregler

### FR-K01: Registrering av utgaaende faktura

1. Ved registrering av en kundefaktura skal systemet automatisk opprette et Bilag med posteringer:
   - **Debet** 1500 Kundefordringer (bruttobelop inkl. MVA)
   - **Kredit** inntektskonto(er) angitt i fakturalinjer (nettobelop)
   - **Kredit** utgaaende MVA-konto (2700-serien) for hver MVA-linje
2. Alle posteringer i bilaget skal ha `KundeId` satt for SAF-T-sporing.
3. Bilaget opprettes i bilagserie "UF" (Utgaaende Faktura) med BilagType.UtgaendeFaktura.
4. `GjenstaendeBelop` settes lik `BelopInklMva`.
5. `Fakturanummer` tildeles automatisk som neste i fortlopende sekvens.
6. `KidNummer` genereres automatisk (MOD10 eller MOD11 ihht kundens/systemets innstilling).

**Eksempel:** Faktura til Kunde B pa NOK 20 000 + 25% MVA
- Debet 1500 Kundefordringer: 25 000
- Kredit 3000 Salgsinntekt: 20 000
- Kredit 2700 Utgaaende MVA: 5 000
- GjenstaendeBelop = 25 000
- KID f.eks. "0001000000016" (MOD10)

### FR-K02: Forfallsdato-beregning

1. Identisk med leverandorreskontro (FR-L02), med kundens betalingsbetingelse.
2. Forskudd: forfallsdato = fakturadato (skal betales for levering).
3. Forfallsdato som faller pa helg flyttes til neste mandag.

### FR-K03: Kreditnota-handtering

1. Kreditnota registreres med `Type = Kreditnota` og positive belop.
2. Bilagsposteringer er speilvendt:
   - **Kredit** 1500 Kundefordringer (bruttobelop)
   - **Debet** inntektskonto(er) (nettobelop)
   - **Debet** utgaaende MVA-konto (MVA-belop)
3. Bilaget opprettes med BilagType.Kreditnota.
4. Kreditnota kan knyttes til opprinnelig faktura via `KreditnotaForFakturaId`.
5. Fakturanummer for kreditnota er i samme sekvens som vanlige fakturaer.

### FR-K04: KID-nummer

1. KID genereres ved opprettelse av faktura.
2. Format: `[kundenummer padded 6][fakturanummer padded 6][kontrollsiffer]` = 13 siffer.
3. Algoritme: MOD10 (default) eller MOD11, konfigurerbart per kunde eller systeminnstilling.
4. MOD11: hvis kontrollsiffer = 10 (ugyldig), hopp over og bruk neste fakturanummer.
5. KID lagres pa fakturaen og brukes for matching ved innbetaling.
6. KID trykkes pa faktura og legges i betalingsinformasjon (EHF PaymentMeans).

### FR-K05: Innbetalingsregistrering

1. Innbetalinger kan registreres:
   a. **Manuelt**: bruker velger faktura og belop
   b. **Via KID-matching**: KID-nummer matcher mot faktura
   c. **Via CAMT.053-import**: banktransaksjoner med KID matches automatisk
2. Ved innbetaling:
   - Opprett KundeInnbetaling
   - Reduser `GjenstaendeBelop` pa fakturaen
   - Oppdater `Status`: Betalt (GjenstaendeBelop = 0) eller DelvisBetalt
   - Opprett Bilag: Debet 1920 Bank, Kredit 1500 Kundefordringer
3. Alle posteringer i bilaget skal ha `KundeId`.
4. Overbetaling: GjenstaendeBelop blir negativ, krever manuell behandling (tilbakebetaling eller forskudd).

**Eksempel:** Innbetaling NOK 25 000 via KID:
- KID "0001000000016" matcher Faktura #1 for Kunde 000100
- Debet 1920 Bank: 25 000
- Kredit 1500 Kundefordringer: 25 000
- GjenstaendeBelop: 25 000 -> 0
- Status: Betalt

### FR-K06: KID-matching (CAMT.053)

1. Parser CAMT.053 XML og ekstraher Ntry-elementer.
2. For hver Ntry med CdtDbtInd = CRDT (innbetaling):
   - Les KID fra RmtInf/Strd/CdtrRefInf/Ref
   - Valider KID med konfigurert algoritme
   - Sok i KundeFaktura etter matchende KidNummer
   - Hvis unik match: auto-registrer innbetaling
   - Hvis flertydig eller ingen match: legg i koe for manuell behandling
3. Belop ma stemme med GjenstaendeBelop (eller tillat delbetaling).

### FR-K07: Purring

1. **Forste purring (betalingspaaminnelse)**:
   - Kan sendes nar faktura er >= 14 dager forfalt.
   - Gebyr: NOK 0 (forste paaminnelse er gebyrfri ihht praksis, men konfigurerbart).
   - Ny betalingsfrist: minimum 14 dager fra purringsdato.
2. **Andre purring**:
   - Kan sendes >= 14 dager etter forste purring.
   - Gebyr: 1/10 av inkassosatsen (2026: ca NOK 70, konfigurerbart).
   - Bilag for gebyr: Debet 1500, Kredit 3400 Purregebyr-inntekt.
   - Gebyr legges til `GjenstaendeBelop` pa fakturaen.
3. **Tredje purring / inkassovarsel**:
   - Kan sendes >= 14 dager etter andre purring.
   - Gebyr: 1/10 av inkassosatsen.
   - MA inneholde tekst om at kravet oversendes inkasso ved manglende betaling.
   - Ny betalingsfrist: minimum 14 dager.
4. Purring skal IKKE sendes for:
   - Fakturaer som er sperret
   - Kunder som er sperret
   - Fakturaer med kreditnota som dekker hele belopet
   - Fakturaer der Status = Betalt
5. Fakturaens `Status` oppdateres til Purring1/Purring2/Purring3 etter sending.

### FR-K08: Forsinkelsesrente

1. Forsinkelsesrente beregnes ihht forsinkelsesrenteloven.
2. Rente = Styringsrenten + 8 prosentpoeng (arlig).
3. Beregnes fra forfallsdato til purringsdato: `Belop * Arsrente * DagerForfalt / 365`.
4. Forsinkelsesrente bokfores som: Debet 1500, Kredit 8050 Renteinntekter.
5. Forsinkelsesrente er et eget konfigurerbart element og kan slas av.

### FR-K09: Apne poster

1. Identisk logikk som leverandorreskontro (FR-L07).
2. Sum apne poster MA stemme med saldo pa konto 1500 Kundefordringer.
3. Inkluderer purregebyr som er lagt til GjenstaendeBelop.

### FR-K10: Aldersfordeling

1. Identisk logikk som leverandorreskontro (FR-L08).
2. Kategorier: Ikke forfalt, 0-30, 31-60, 61-90, 90+ dager.
3. Purregebyr inkluderes i fakturaens alderskategori.

### FR-K11: Kundeutskrift

1. Identisk logikk som leverandorutskrift (FR-L09), speilt for kunder.
2. Inkluderer: fakturaer, kreditnotaer, innbetalinger, purregebyr.
3. Viser KID-nummer og fakturanummer for hver linje.
4. Mapper til bokforingsforskriften 3-1 kundespesifikasjon.

### FR-K12: Kundenummer-tildeling

1. Identisk logikk som leverandornummer (FR-L11), med eget konfigurerbart startpunkt (typisk 10001).

### FR-K13: Kredittgrensekontroll

1. Ved registrering av ny faktura: beregn netto utstaaende = sum GjenstaendeBelop for alle apne fakturaer.
2. Hvis netto utstaaende + ny faktura > Kredittgrense: advarsel (ikke blokkering, konfigurerbart).
3. Kredittgrense = 0 betyr ingen grense.

### FR-K14: Tap pa fordringer

1. Nar en fordring anses uerholdelig, kan den avskrives:
   - Debet 7830 Tap pa fordringer: belopet
   - Kredit 1500 Kundefordringer: belopet
   - Tilbakefore utgaaende MVA: Debet 2700, Kredit 7830 (MVA-andelen)
2. Fakturaens status settes til Tap.
3. Krever minimum 3 purringer + dokumentasjon av inndrivingsforsok.

---

## MVA-handtering

### Relevante MVA-koder

| Kode | SAF-T | Sats | Bruk i kundereskontro |
|------|-------|------|----------------------|
| 3 | 3 | 25% | Utgaaende MVA, alminnelig sats |
| 31 | 31 | 15% | Utgaaende MVA, naringsmiddel |
| 33 | 33 | 12% | Utgaaende MVA, persontransport |
| 5 | 5 | 0% | Innenlands omsetning, fritatt |
| 6 | 6 | 0% | Utforsel (eksport) |
| 0 | 0 | 0% | Utenfor MVA-omradet |

### Beregningslogikk

1. MVA beregnes per fakturalinje: `MvaBelop = (Antall * Enhetspris * (1 - Rabatt/100)) * MvaSats / 100`
2. Avrunding til narest ore (2 desimaler).
3. Linje.Belop = `Antall * Enhetspris * (1 - Rabatt/100)` (netto etter rabatt, for MVA).
4. Total MVA pa faktura = sum av MVA per linje.
5. `BelopInklMva = BelopEksMva + MvaBelop`.

### SAF-T Mapping

- Posteringer mot kundefordringer (1500) ma ha `CustomerID` i SAF-T Line.
- MVA-informasjon per linje i TaxInformation.
- Kunde eksporteres i MasterFiles > Customers med opening/closing balanse (v1.30).
- Fakturanummer mapper til SourceDocuments > SalesInvoices (hvis implementert).

---

## Avhengigheter

| Modul | Interface/Service | Bruk |
|-------|-------------------|------|
| Kontoplan | `IKontoplanRepository` | Hent kontoer for kontering |
| Hovedbok | `Bilag`, `Postering` | Opprette bilag med posteringer |
| Bilagsregistrering | `IBilagRegistreringService` | Opprette og bokfore bilag |
| MVA | MvaKode-entitet | Hente MVA-satser og kontoer |
| Leverandorreskontro | `Alderskategori` (delt enum) | Konsistente alderskategorier |

### Interfaces definert av denne modulen

```csharp
namespace Regnskap.Application.Features.Kundereskontro;

public interface IKundeService
{
    Task<KundeDto> OpprettAsync(OpprettKundeRequest request, CancellationToken ct = default);
    Task<KundeDto> OppdaterAsync(Guid id, OppdaterKundeRequest request, CancellationToken ct = default);
    Task<KundeDto> HentAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<KundeDto>> SokAsync(KundeSokRequest request, CancellationToken ct = default);
    Task SlettAsync(Guid id, CancellationToken ct = default);
}

public interface IKundeFakturaService
{
    // Faktura
    Task<KundeFakturaDto> RegistrerFakturaAsync(RegistrerKundeFakturaRequest request, CancellationToken ct = default);
    Task<KundeFakturaDto> HentAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<KundeFakturaDto>> SokAsync(KundeFakturaSokRequest request, CancellationToken ct = default);

    // Innbetaling
    Task<KundeInnbetalingDto> RegistrerInnbetalingAsync(RegistrerInnbetalingRequest request, CancellationToken ct = default);
    Task<KundeInnbetalingDto> MatchKidAsync(MatchKidRequest request, CancellationToken ct = default);
    Task<Camt053ImportResultDto> ImporterCamt053Async(Stream xmlStream, CancellationToken ct = default);
    Task<List<UmatchetInnbetalingDto>> HentUmatchedeAsync(CancellationToken ct = default);

    // Tap
    Task<KundeFakturaDto> AvskrivTapAsync(Guid fakturaId, string begrunnelse, CancellationToken ct = default);

    // Rapporter
    Task<List<KundeFakturaDto>> HentApnePosterAsync(DateOnly? dato = null, CancellationToken ct = default);
    Task<KundeAldersfordelingDto> HentAldersfordelingAsync(DateOnly dato, CancellationToken ct = default);
    Task<KundeutskriftDto> HentUtskriftAsync(Guid kundeId, DateOnly fraDato, DateOnly tilDato, CancellationToken ct = default);
}

public interface IKidService
{
    string Generer(string kundenummer, int fakturanummer, KidAlgoritme algoritme);
    bool Valider(string kid, KidAlgoritme algoritme);
    KundeFaktura? FinnFakturaMedKid(string kid);
}

public interface IPurringService
{
    Task<List<PurreforslagDto>> GenererForslagAsync(PurreforslagRequest request, CancellationToken ct = default);
    Task<List<PurringDto>> OpprettPurringerAsync(List<Guid> fakturaIder, PurringType type, CancellationToken ct = default);
    Task MarkerSendtAsync(Guid purringId, string sendemetode, CancellationToken ct = default);
}

public interface IKundeReskontroRepository
{
    // Kunde
    Task<Kunde?> HentKundeAsync(Guid id, CancellationToken ct = default);
    Task<Kunde?> HentKundeMedNummerAsync(string kundenummer, CancellationToken ct = default);
    Task<bool> KundenummerEksistererAsync(string kundenummer, CancellationToken ct = default);
    Task LeggTilKundeAsync(Kunde kunde, CancellationToken ct = default);
    Task OppdaterKundeAsync(Kunde kunde, CancellationToken ct = default);

    // Faktura
    Task<KundeFaktura?> HentFakturaAsync(Guid id, CancellationToken ct = default);
    Task<KundeFaktura?> HentFakturaMedKidAsync(string kidNummer, CancellationToken ct = default);
    Task<int> NesteNummer(CancellationToken ct = default);
    Task LeggTilFakturaAsync(KundeFaktura faktura, CancellationToken ct = default);
    Task OppdaterFakturaAsync(KundeFaktura faktura, CancellationToken ct = default);
    Task<List<KundeFaktura>> HentApnePosterAsync(DateOnly? dato = null, CancellationToken ct = default);
    Task<List<KundeFaktura>> HentForfalteFakturaerAsync(DateOnly dato, int minimumDagerForfalt, CancellationToken ct = default);

    // Innbetaling
    Task LeggTilInnbetalingAsync(KundeInnbetaling innbetaling, CancellationToken ct = default);

    // Purring
    Task LeggTilPurringAsync(Purring purring, CancellationToken ct = default);
    Task<Purring?> HentSistePurringAsync(Guid fakturaId, CancellationToken ct = default);

    Task LagreEndringerAsync(CancellationToken ct = default);
}
```

### Camt053ImportResultDto

```csharp
public record Camt053ImportResultDto(
    int TotaltAntall,
    int AutoMatchet,
    int ManuellBehandling,
    int Feilet,
    List<Camt053TransaksjonDto> Transaksjoner
);

public record Camt053TransaksjonDto(
    string Bankreferanse,
    decimal Belop,
    DateOnly Dato,
    string? KidNummer,
    Guid? MatchetFakturaId,
    string? MatchetKundenavn,
    string Status       // "AutoMatchet", "IkkeFunnet", "FlereMatch", "BelopAvvik"
);
```
