# Spesifikasjon: Hovedbok (General Ledger)

**Modul:** Hovedbok
**Status:** Komplett spesifikasjon
**Avhengigheter:** Kontoplan (implementert)
**SAF-T-seksjon:** GeneralLedgerEntries
**Bokforingsloven:** 4, 5, 7, 10

---

## Oversikt

Hovedboken er kjernen i dobbelt bokholderi. Den holder alle posteringer organisert per konto og per periode, og sikrer at debet alltid er lik kredit. Alle posteringer skjer via bilag (vouchers). Modulen gir grunnlag for:

- Kontospesifikasjon (bokforingsforskriften 3-1)
- Saldobalanse (trial balance)
- Periodeavstemming og periodelukking
- SAF-T GeneralLedgerEntries-eksport

---

## Datamodell

### Enums

```csharp
namespace Regnskap.Domain.Features.Hovedbok;

/// <summary>
/// Status for en regnskapsperiode.
/// </summary>
public enum PeriodeStatus
{
    /// <summary>Perioden er open for bokforing.</summary>
    Apen,

    /// <summary>Perioden er midlertidig sperret (f.eks. under avstemming).</summary>
    Sperret,

    /// <summary>Perioden er endelig lukket. Ingen posteringer kan legges til.</summary>
    Lukket
}

/// <summary>
/// Type bilag/journal. Mapper til SAF-T Journal/Type.
/// </summary>
public enum BilagType
{
    /// <summary>Manuelt bilag (generell journalforing).</summary>
    Manuelt,

    /// <summary>Inngaende faktura.</summary>
    InngaendeFaktura,

    /// <summary>Utgaende faktura.</summary>
    UtgaendeFaktura,

    /// <summary>Bankbilag (betaling).</summary>
    Bank,

    /// <summary>Lonsbilag.</summary>
    Lonn,

    /// <summary>Avskrivninger.</summary>
    Avskrivning,

    /// <summary>MVA-oppgjor.</summary>
    MvaOppgjor,

    /// <summary>Arsavslutning.</summary>
    Arsavslutning,

    /// <summary>Apningsbalanse.</summary>
    Apningsbalanse,

    /// <summary>Kreditnota.</summary>
    Kreditnota,

    /// <summary>Korrigeringsbilag.</summary>
    Korreksjon
}

/// <summary>
/// Side i et dobbelt bokholderi (debet eller kredit).
/// </summary>
public enum BokforingSide
{
    Debet,
    Kredit
}
```

### Entity: Regnskapsperiode

Representerer en regnskapsperiode (maned). Perioder styrer nar posteringer kan bokfores.

```csharp
namespace Regnskap.Domain.Features.Hovedbok;

using Regnskap.Domain.Common;

/// <summary>
/// En regnskapsperiode (maned i et regnskapsar).
/// Perioder opprettes per regnskapsar og styrer tilgang til bokforing.
/// </summary>
public class Regnskapsperiode : AuditableEntity
{
    /// <summary>
    /// Regnskapsaret (f.eks. 2026).
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Maned (1-12). Periode 0 brukes for apningsbalanse, periode 13 for arsavslutning.
    /// </summary>
    public int Periode { get; set; }

    /// <summary>
    /// Periodens forste dato (inklusiv).
    /// </summary>
    public DateOnly FraDato { get; set; }

    /// <summary>
    /// Periodens siste dato (inklusiv).
    /// </summary>
    public DateOnly TilDato { get; set; }

    /// <summary>
    /// Periodens status: Apen, Sperret, eller Lukket.
    /// </summary>
    public PeriodeStatus Status { get; set; } = PeriodeStatus.Apen;

    /// <summary>
    /// Tidspunkt da perioden ble lukket. Null hvis apen.
    /// </summary>
    public DateTime? LukketTidspunkt { get; set; }

    /// <summary>
    /// Hvem som lukket perioden.
    /// </summary>
    public string? LukketAv { get; set; }

    /// <summary>
    /// Begrunnelse for lukking eller sperring.
    /// </summary>
    public string? Merknad { get; set; }

    // --- Navigation ---

    /// <summary>
    /// Alle kontosaldoer for denne perioden.
    /// </summary>
    public ICollection<KontoSaldo> KontoSaldoer { get; set; } = new List<KontoSaldo>();

    // --- Avledede egenskaper ---

    /// <summary>
    /// Menneskelig lesbart periodenavn (f.eks. "2026-01", "2026-00 Apningsbalanse").
    /// </summary>
    public string Periodenavn => Periode switch
    {
        0 => $"{Ar}-00 Apningsbalanse",
        13 => $"{Ar}-13 Arsavslutning",
        _ => $"{Ar}-{Periode:D2}"
    };

    /// <summary>
    /// Om perioden aksepterer nye posteringer.
    /// </summary>
    public bool ErApen => Status == PeriodeStatus.Apen;

    /// <summary>
    /// Om perioden er endelig lukket.
    /// </summary>
    public bool ErLukket => Status == PeriodeStatus.Lukket;

    // --- Forretningslogikk ---

    /// <summary>
    /// Valider at en dato faller innenfor perioden.
    /// </summary>
    public bool DatoErInnenforPeriode(DateOnly dato) =>
        dato >= FraDato && dato <= TilDato;

    /// <summary>
    /// Kast exception hvis perioden ikke er apen for bokforing.
    /// </summary>
    public void ValiderApen()
    {
        if (Status != PeriodeStatus.Apen)
            throw new PeriodeLukketException(Ar, Periode);
    }
}
```

**EF Core-konfigurasjon:**

```csharp
public class RegnskapsperiodeConfiguration : IEntityTypeConfiguration<Regnskapsperiode>
{
    public void Configure(EntityTypeBuilder<Regnskapsperiode> builder)
    {
        builder.ToTable("Regnskapsperioder");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Ar).IsRequired();
        builder.Property(e => e.Periode).IsRequired();
        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();
        builder.Property(e => e.Merknad).HasMaxLength(500);
        builder.Property(e => e.LukketAv).HasMaxLength(256);

        // Unik kombinasjon av ar og periode
        builder.HasIndex(e => new { e.Ar, e.Periode })
            .IsUnique()
            .HasFilter("IsDeleted = 0");

        // Ytelseindeks
        builder.HasIndex(e => e.Status);
    }
}
```

### Entity: Bilag (Voucher) — Interface

Bilag-modulen bygges separat, men Hovedboken definerer grensesnittet her.

```csharp
namespace Regnskap.Domain.Features.Hovedbok;

using Regnskap.Domain.Common;

/// <summary>
/// Et bilag (voucher) — den grunnleggende bokforingsenheten.
/// Alle posteringer i hovedboken tilhorer et bilag.
/// Et bilag MÅ vaere i balanse: sum debet = sum kredit.
///
/// Mapper til SAF-T: GeneralLedgerEntries > Journal > Transaction
/// </summary>
public class Bilag : AuditableEntity
{
    /// <summary>
    /// Fortlopende bilagsnummer innenfor regnskapsaret.
    /// Bokforingsloven krever fortlopende nummerering uten hull.
    /// </summary>
    public int Bilagsnummer { get; set; }

    /// <summary>
    /// Regnskapsaret bilaget tilhorer.
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Bilagstype. Mapper til SAF-T Journal/Type.
    /// </summary>
    public BilagType Type { get; set; }

    /// <summary>
    /// Bilagsdato — den faktiske transaksjonsdatoen.
    /// Mapper til SAF-T TransactionDate.
    /// </summary>
    public DateOnly Bilagsdato { get; set; }

    /// <summary>
    /// Dato bilaget ble registrert i systemet.
    /// Mapper til SAF-T SystemEntryDate.
    /// </summary>
    public DateTime Registreringsdato { get; set; }

    /// <summary>
    /// Beskrivelse av bilaget.
    /// Mapper til SAF-T Transaction/Description.
    /// </summary>
    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// FK til regnskapsperioden bilaget er bokfort i.
    /// </summary>
    public Guid RegnskapsperiodeId { get; set; }
    public Regnskapsperiode Regnskapsperiode { get; set; } = default!;

    /// <summary>
    /// Ekstern referanse (fakturanummer, betalingsreferanse, etc.).
    /// </summary>
    public string? EksternReferanse { get; set; }

    /// <summary>
    /// Alle posteringer (linjer) i bilaget.
    /// </summary>
    public ICollection<Postering> Posteringer { get; set; } = new List<Postering>();

    // --- Avledede egenskaper ---

    /// <summary>
    /// Unik bilagsreferanse (f.eks. "2026-00042").
    /// Mapper til SAF-T TransactionID.
    /// </summary>
    public string BilagsId => $"{Ar}-{Bilagsnummer:D5}";

    /// <summary>
    /// SAF-T-periode (1-12).
    /// </summary>
    public int SaftPeriode => Bilagsdato.Month;

    // --- Forretningslogikk ---

    /// <summary>
    /// Beregner sum debet for alle posteringer.
    /// </summary>
    public Belop SumDebet() => Posteringer
        .Where(p => p.Side == BokforingSide.Debet)
        .Aggregate(Belop.Null, (sum, p) => sum + p.Belop);

    /// <summary>
    /// Beregner sum kredit for alle posteringer.
    /// </summary>
    public Belop SumKredit() => Posteringer
        .Where(p => p.Side == BokforingSide.Kredit)
        .Aggregate(Belop.Null, (sum, p) => sum + p.Belop);

    /// <summary>
    /// Validerer at bilaget er i balanse og har minimum 2 linjer.
    /// Kaster AccountingBalanceException hvis ikke i balanse.
    /// </summary>
    public void ValiderBalanse()
    {
        if (Posteringer.Count < 2)
            throw new BilagValideringException(
                BilagsId, "Et bilag ma ha minimum 2 posteringer.");

        var debet = SumDebet();
        var kredit = SumKredit();

        if (debet.Verdi != kredit.Verdi)
            throw new AccountingBalanceException(debet.Verdi, kredit.Verdi);
    }
}
```

**EF Core-konfigurasjon:**

```csharp
public class BilagConfiguration : IEntityTypeConfiguration<Bilag>
{
    public void Configure(EntityTypeBuilder<Bilag> builder)
    {
        builder.ToTable("Bilag");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Bilagsnummer).IsRequired();
        builder.Property(e => e.Ar).IsRequired();
        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(e => e.Beskrivelse)
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(e => e.EksternReferanse).HasMaxLength(100);

        // Unik bilagsnummer per ar
        builder.HasIndex(e => new { e.Ar, e.Bilagsnummer })
            .IsUnique()
            .HasFilter("IsDeleted = 0");

        // FK til regnskapsperiode
        builder.HasOne(e => e.Regnskapsperiode)
            .WithMany()
            .HasForeignKey(e => e.RegnskapsperiodeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ytelseindekser
        builder.HasIndex(e => e.Bilagsdato);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.RegnskapsperiodeId);
    }
}
```

### Entity: Postering (Ledger Entry Line)

En enkelt postering i hovedboken — en linje i et bilag, bokfort mot en konto.

```csharp
namespace Regnskap.Domain.Features.Hovedbok;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kontoplan;

/// <summary>
/// En postering i hovedboken. Representerer en enkelt debet- eller kredit-linje
/// i et bilag, bokfort mot en spesifikk konto.
///
/// Mapper til SAF-T: GeneralLedgerEntries > Journal > Transaction > Line
/// </summary>
public class Postering : AuditableEntity
{
    /// <summary>
    /// FK til bilaget denne posteringen tilhorer.
    /// </summary>
    public Guid BilagId { get; set; }
    public Bilag Bilag { get; set; } = default!;

    /// <summary>
    /// Linjenummer innenfor bilaget (1, 2, 3...).
    /// Mapper til SAF-T Line/RecordID.
    /// </summary>
    public int Linjenummer { get; set; }

    /// <summary>
    /// FK til kontoen det posteres mot.
    /// Mapper til SAF-T Line/AccountID.
    /// </summary>
    public Guid KontoId { get; set; }
    public Konto Konto { get; set; } = default!;

    /// <summary>
    /// Kontonummer denormalisert for ytelse og sporbarhet.
    /// </summary>
    public string Kontonummer { get; set; } = default!;

    /// <summary>
    /// Debet eller kredit.
    /// </summary>
    public BokforingSide Side { get; set; }

    /// <summary>
    /// Belopet som posteres (alltid positivt).
    /// Mapper til SAF-T DebitAmount/Amount eller CreditAmount/Amount.
    /// </summary>
    public Belop Belop { get; set; }

    /// <summary>
    /// Beskrivelse av posteringen.
    /// Mapper til SAF-T Line/Description.
    /// </summary>
    public string Beskrivelse { get; set; } = default!;

    /// <summary>
    /// MVA-kode brukt for denne posteringen. Null hvis MVA-fri.
    /// </summary>
    public string? MvaKode { get; set; }

    /// <summary>
    /// MVA-belop beregnet for denne posteringen.
    /// </summary>
    public Belop? MvaBelop { get; set; }

    /// <summary>
    /// MVA-grunnlag (basbelop for MVA-beregning).
    /// </summary>
    public Belop? MvaGrunnlag { get; set; }

    /// <summary>
    /// MVA-sats brukt (snapshot ved bokforingstidspunkt).
    /// </summary>
    public decimal? MvaSats { get; set; }

    /// <summary>
    /// Avdelingskode/kostnadssted. Pakreves for kontoer med KreverAvdeling = true.
    /// </summary>
    public string? Avdelingskode { get; set; }

    /// <summary>
    /// Prosjektkode. Pakreves for kontoer med KreverProsjekt = true.
    /// </summary>
    public string? Prosjektkode { get; set; }

    /// <summary>
    /// Kunde-ID for kunde-relaterte posteringer.
    /// Mapper til SAF-T Line/CustomerID.
    /// </summary>
    public Guid? KundeId { get; set; }

    /// <summary>
    /// Leverandor-ID for leverandor-relaterte posteringer.
    /// Mapper til SAF-T Line/SupplierID.
    /// </summary>
    public Guid? LeverandorId { get; set; }

    /// <summary>
    /// Bilagsdato kopieres hit for enklere sporing og indeksering.
    /// </summary>
    public DateOnly Bilagsdato { get; set; }

    // --- Avledede egenskaper ---

    /// <summary>
    /// Fortegnsberegnet belop: positivt for debet, negativt for kredit.
    /// Nyttig for saldoberegninger.
    /// </summary>
    public Belop FortegnsbelOp => Side == BokforingSide.Debet ? Belop : -Belop;
}
```

**EF Core-konfigurasjon:**

```csharp
public class PosteringConfiguration : IEntityTypeConfiguration<Postering>
{
    public void Configure(EntityTypeBuilder<Postering> builder)
    {
        builder.ToTable("Posteringer");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Linjenummer).IsRequired();
        builder.Property(e => e.Kontonummer)
            .HasMaxLength(10)
            .IsRequired();
        builder.Property(e => e.Side)
            .HasConversion<string>()
            .HasMaxLength(6)
            .IsRequired();
        builder.Property(e => e.Beskrivelse)
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(e => e.MvaKode).HasMaxLength(10);
        builder.Property(e => e.Avdelingskode).HasMaxLength(20);
        builder.Property(e => e.Prosjektkode).HasMaxLength(20);

        // Belop som decimal(18,2)
        builder.Property(e => e.Belop)
            .HasConversion(v => v.Verdi, v => new Belop(v))
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        builder.Property(e => e.MvaBelop)
            .HasConversion(
                v => v.HasValue ? v.Value.Verdi : (decimal?)null,
                v => v.HasValue ? new Belop(v.Value) : null)
            .HasColumnType("decimal(18,2)");
        builder.Property(e => e.MvaGrunnlag)
            .HasConversion(
                v => v.HasValue ? v.Value.Verdi : (decimal?)null,
                v => v.HasValue ? new Belop(v.Value) : null)
            .HasColumnType("decimal(18,2)");
        builder.Property(e => e.MvaSats)
            .HasColumnType("decimal(5,2)");

        // FK til bilag
        builder.HasOne(e => e.Bilag)
            .WithMany(b => b.Posteringer)
            .HasForeignKey(e => e.BilagId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK til konto
        builder.HasOne(e => e.Konto)
            .WithMany()
            .HasForeignKey(e => e.KontoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unik linjenummer per bilag
        builder.HasIndex(e => new { e.BilagId, e.Linjenummer })
            .IsUnique()
            .HasFilter("IsDeleted = 0");

        // Ytelseindekser — kritiske for kontoutskrift og saldooppslag
        builder.HasIndex(e => e.KontoId);
        builder.HasIndex(e => e.Kontonummer);
        builder.HasIndex(e => e.Bilagsdato);
        builder.HasIndex(e => new { e.Kontonummer, e.Bilagsdato });
        builder.HasIndex(e => e.KundeId).HasFilter("KundeId IS NOT NULL");
        builder.HasIndex(e => e.LeverandorId).HasFilter("LeverandorId IS NOT NULL");
        builder.HasIndex(e => e.Avdelingskode).HasFilter("Avdelingskode IS NOT NULL");
        builder.HasIndex(e => e.Prosjektkode).HasFilter("Prosjektkode IS NOT NULL");
    }
}
```

### Entity: KontoSaldo (Account Balance per Period)

Materialisert saldo per konto per regnskapsperiode. Oppdateres ved bokforing.

```csharp
namespace Regnskap.Domain.Features.Hovedbok;

using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kontoplan;

/// <summary>
/// Saldo for en konto i en spesifikk regnskapsperiode.
/// Inneholder inngaende balanse, sum debet/kredit i perioden, og utgaende balanse.
///
/// Oppdateres inkrementelt ved hver bokforing — ALDRI beregnet pa nytt fra posteringer
/// med mindre det er en eksplisitt reberegning/avstemming.
/// </summary>
public class KontoSaldo : AuditableEntity
{
    /// <summary>
    /// FK til kontoen.
    /// </summary>
    public Guid KontoId { get; set; }
    public Konto Konto { get; set; } = default!;

    /// <summary>
    /// Kontonummer denormalisert for ytelse.
    /// </summary>
    public string Kontonummer { get; set; } = default!;

    /// <summary>
    /// FK til regnskapsperioden.
    /// </summary>
    public Guid RegnskapsperiodeId { get; set; }
    public Regnskapsperiode Regnskapsperiode { get; set; } = default!;

    /// <summary>
    /// Regnskapsaret.
    /// </summary>
    public int Ar { get; set; }

    /// <summary>
    /// Periodenummer (0-13).
    /// </summary>
    public int Periode { get; set; }

    /// <summary>
    /// Inngaende balanse (IB) ved periodens start.
    /// For periode 1: kommer fra forrige ars utgaende balanse (balansekontoer)
    /// eller er 0 (resultatkontoer, som nullstilles ved arsavslutning).
    /// </summary>
    public Belop InngaendeBalanse { get; set; } = Belop.Null;

    /// <summary>
    /// Sum av alle debetposteringer i perioden.
    /// </summary>
    public Belop SumDebet { get; set; } = Belop.Null;

    /// <summary>
    /// Sum av alle kreditposteringer i perioden.
    /// </summary>
    public Belop SumKredit { get; set; } = Belop.Null;

    /// <summary>
    /// Antall posteringer i perioden.
    /// </summary>
    public int AntallPosteringer { get; set; }

    // --- Avledede egenskaper ---

    /// <summary>
    /// Endring i perioden = SumDebet - SumKredit.
    /// Positivt = netto debet, negativt = netto kredit.
    /// </summary>
    public Belop Endring => SumDebet - SumKredit;

    /// <summary>
    /// Utgaende balanse (UB) = IB + SumDebet - SumKredit.
    /// </summary>
    public Belop UtgaendeBalanse => InngaendeBalanse + SumDebet - SumKredit;

    // --- Forretningslogikk ---

    /// <summary>
    /// Oppdater saldoen med en ny postering.
    /// </summary>
    public void LeggTilPostering(BokforingSide side, Belop belop)
    {
        if (side == BokforingSide.Debet)
            SumDebet += belop;
        else
            SumKredit += belop;

        AntallPosteringer++;
    }
}
```

**EF Core-konfigurasjon:**

```csharp
public class KontoSaldoConfiguration : IEntityTypeConfiguration<KontoSaldo>
{
    public void Configure(EntityTypeBuilder<KontoSaldo> builder)
    {
        builder.ToTable("KontoSaldoer");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Kontonummer)
            .HasMaxLength(10)
            .IsRequired();
        builder.Property(e => e.Ar).IsRequired();
        builder.Property(e => e.Periode).IsRequired();
        builder.Property(e => e.AntallPosteringer).IsRequired();

        // Belop-kolonner
        builder.Property(e => e.InngaendeBalanse)
            .HasConversion(v => v.Verdi, v => new Belop(v))
            .HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.SumDebet)
            .HasConversion(v => v.Verdi, v => new Belop(v))
            .HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.SumKredit)
            .HasConversion(v => v.Verdi, v => new Belop(v))
            .HasColumnType("decimal(18,2)").IsRequired();

        // Unik saldo per konto per periode
        builder.HasIndex(e => new { e.KontoId, e.RegnskapsperiodeId })
            .IsUnique()
            .HasFilter("IsDeleted = 0");

        // FK
        builder.HasOne(e => e.Konto)
            .WithMany()
            .HasForeignKey(e => e.KontoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Regnskapsperiode)
            .WithMany(p => p.KontoSaldoer)
            .HasForeignKey(e => e.RegnskapsperiodeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ytelseindekser
        builder.HasIndex(e => new { e.Kontonummer, e.Ar, e.Periode });
        builder.HasIndex(e => new { e.Ar, e.Periode });
    }
}
```

### Exceptions

```csharp
namespace Regnskap.Domain.Features.Hovedbok;

public class BilagValideringException : Exception
{
    public string BilagsId { get; }
    public BilagValideringException(string bilagsId, string melding)
        : base($"Bilag {bilagsId}: {melding}") => BilagsId = bilagsId;
}

public class PeriodeIkkeFunnetException : Exception
{
    public int Ar { get; }
    public int Periode { get; }
    public PeriodeIkkeFunnetException(int ar, int periode)
        : base($"Regnskapsperiode {ar}-{periode:D2} finnes ikke.")
    {
        Ar = ar;
        Periode = periode;
    }
}

public class PeriodeSperretException : Exception
{
    public int Ar { get; }
    public int Periode { get; }
    public PeriodeSperretException(int ar, int periode)
        : base($"Regnskapsperiode {ar}-{periode:D2} er sperret.")
    {
        Ar = ar;
        Periode = periode;
    }
}

public class BilagNummereringException : Exception
{
    public BilagNummereringException(string melding) : base(melding) { }
}

public class PeriodeLukkingException : Exception
{
    public int Ar { get; }
    public int Periode { get; }
    public PeriodeLukkingException(int ar, int periode, string grunn)
        : base($"Kan ikke lukke periode {ar}-{periode:D2}: {grunn}")
    {
        Ar = ar;
        Periode = periode;
    }
}

public class KontoIkkeBokforbarException : Exception
{
    public string Kontonummer { get; }
    public KontoIkkeBokforbarException(string kontonummer)
        : base($"Konto {kontonummer} er ikke bokforbar (summekonto/overskrift).")
        => Kontonummer = kontonummer;
}

public class AvdelingPakrevdException : Exception
{
    public string Kontonummer { get; }
    public AvdelingPakrevdException(string kontonummer)
        : base($"Konto {kontonummer} krever at avdelingskode angis.")
        => Kontonummer = kontonummer;
}

public class ProsjektPakrevdException : Exception
{
    public string Kontonummer { get; }
    public ProsjektPakrevdException(string kontonummer)
        : base($"Konto {kontonummer} krever at prosjektkode angis.")
        => Kontonummer = kontonummer;
}
```

### DbContext-utvidelse

```csharp
// Legg til i RegnskapDbContext:

// Hovedbok
public DbSet<Regnskapsperiode> Regnskapsperioder => Set<Regnskapsperiode>();
public DbSet<Bilag> Bilag => Set<Bilag>();
public DbSet<Postering> Posteringer => Set<Postering>();
public DbSet<KontoSaldo> KontoSaldoer => Set<KontoSaldo>();
```

---

## Repositories

### IHovedbokRepository

```csharp
namespace Regnskap.Domain.Features.Hovedbok;

public interface IHovedbokRepository
{
    // --- Regnskapsperioder ---
    Task<Regnskapsperiode?> HentPeriodeAsync(int ar, int periode, CancellationToken ct = default);
    Task<Regnskapsperiode?> HentPeriodeForDatoAsync(DateOnly dato, CancellationToken ct = default);
    Task<List<Regnskapsperiode>> HentPerioderForArAsync(int ar, CancellationToken ct = default);
    Task<List<Regnskapsperiode>> HentApnePerioderAsync(CancellationToken ct = default);
    Task LeggTilPeriodeAsync(Regnskapsperiode periode, CancellationToken ct = default);
    Task<bool> PeriodeFinnesAsync(int ar, int periode, CancellationToken ct = default);

    // --- Bilag ---
    Task<Bilag?> HentBilagAsync(Guid id, CancellationToken ct = default);
    Task<Bilag?> HentBilagMedPosteringerAsync(Guid id, CancellationToken ct = default);
    Task<Bilag?> HentBilagMedNummerAsync(int ar, int bilagsnummer, CancellationToken ct = default);
    Task<int> NestebilagsnummerAsync(int ar, CancellationToken ct = default);
    Task LeggTilBilagAsync(Bilag bilag, CancellationToken ct = default);
    Task<List<Bilag>> HentBilagForPeriodeAsync(
        int ar, int periode,
        BilagType? type = null,
        int side = 1, int antall = 50,
        CancellationToken ct = default);
    Task<int> TellBilagForPeriodeAsync(int ar, int periode, BilagType? type = null, CancellationToken ct = default);

    // --- Posteringer ---
    Task<List<Postering>> HentPosteringerForKontoAsync(
        string kontonummer,
        DateOnly? fraDato = null,
        DateOnly? tilDato = null,
        int side = 1,
        int antall = 100,
        CancellationToken ct = default);
    Task<int> TellPosteringerForKontoAsync(
        string kontonummer,
        DateOnly? fraDato = null,
        DateOnly? tilDato = null,
        CancellationToken ct = default);
    Task<bool> PeriodeHarPosteringerAsync(int ar, int periode, CancellationToken ct = default);

    // --- KontoSaldo ---
    Task<KontoSaldo?> HentKontoSaldoAsync(
        string kontonummer, int ar, int periode, CancellationToken ct = default);
    Task<List<KontoSaldo>> HentAlleSaldoerForPeriodeAsync(
        int ar, int periode, CancellationToken ct = default);
    Task<List<KontoSaldo>> HentSaldoHistorikkForKontoAsync(
        string kontonummer, int ar, CancellationToken ct = default);
    Task LeggTilKontoSaldoAsync(KontoSaldo saldo, CancellationToken ct = default);

    // --- Generelt ---
    Task LagreEndringerAsync(CancellationToken ct = default);
}
```

---

## API-kontrakt

### Regnskapsperioder

#### POST /api/perioder/opprett-ar

Oppretter alle 12 perioder for et regnskapsar (pluss periode 0 og 13).

**Request:**
```json
{
  "ar": 2026
}
```

**Response (201):**
```json
{
  "ar": 2026,
  "perioder": [
    { "ar": 2026, "periode": 0, "fraDato": "2026-01-01", "tilDato": "2026-01-01", "status": "Apen" },
    { "ar": 2026, "periode": 1, "fraDato": "2026-01-01", "tilDato": "2026-01-31", "status": "Apen" },
    ...
    { "ar": 2026, "periode": 12, "fraDato": "2026-12-01", "tilDato": "2026-12-31", "status": "Apen" },
    { "ar": 2026, "periode": 13, "fraDato": "2026-12-31", "tilDato": "2026-12-31", "status": "Apen" }
  ]
}
```

**Validering:**
- `ar`: Pakreves, 2000-2099
- Perioder for aret ma ikke finnes fra for

**Feilkoder:**
- `409 Conflict` — Perioder for dette aret finnes allerede

---

#### GET /api/perioder/{ar}

Henter alle perioder for et regnskapsar.

**Response (200):**
```json
{
  "ar": 2026,
  "perioder": [
    {
      "id": "guid",
      "ar": 2026,
      "periode": 1,
      "periodenavn": "2026-01",
      "fraDato": "2026-01-01",
      "tilDato": "2026-01-31",
      "status": "Apen",
      "lukketTidspunkt": null,
      "lukketAv": null,
      "antallBilag": 42,
      "sumDebet": 125000.00,
      "sumKredit": 125000.00
    }
  ]
}
```

---

#### PUT /api/perioder/{ar}/{periode}/status

Endre status for en periode (sperr eller lukk).

**Request:**
```json
{
  "nyStatus": "Lukket",
  "merknad": "Periodeavstemming fullfort"
}
```

**Validering:**
- Gyldige overganger: Apen -> Sperret, Sperret -> Apen, Sperret -> Lukket
- Man kan IKKE ga fra Apen direkte til Lukket (ma sperres forst)
- Man kan IKKE ga fra Lukket tilbake (permanent)
- Lukking krever at periodeavstemming er utfort (se forretningsregler)

**Feilkoder:**
- `400 Bad Request` — Ugyldig statusovergang
- `409 Conflict` — Periodeavstemming ikke fullfort (ved lukking)

---

### Bilag og posteringer

#### POST /api/bilag

Opprett et nytt bilag med posteringer. Bilaget bokfores atomisk.

**Request:**
```json
{
  "type": "Manuelt",
  "bilagsdato": "2026-03-15",
  "beskrivelse": "Kontorkjop Elkjop",
  "eksternReferanse": "F-2026-0042",
  "posteringer": [
    {
      "kontonummer": "6540",
      "side": "Debet",
      "belop": 8000.00,
      "beskrivelse": "Kontorutstyr",
      "mvaKode": "3",
      "avdelingskode": null,
      "prosjektkode": null
    },
    {
      "kontonummer": "2710",
      "side": "Debet",
      "belop": 2000.00,
      "beskrivelse": "Inngaende MVA 25%",
      "mvaKode": null,
      "avdelingskode": null,
      "prosjektkode": null
    },
    {
      "kontonummer": "2400",
      "side": "Kredit",
      "belop": 10000.00,
      "beskrivelse": "Leverandorgjeld Elkjop",
      "mvaKode": null,
      "avdelingskode": null,
      "prosjektkode": null
    }
  ]
}
```

**Response (201):**
```json
{
  "id": "guid",
  "bilagsId": "2026-00042",
  "bilagsnummer": 42,
  "ar": 2026,
  "type": "Manuelt",
  "bilagsdato": "2026-03-15",
  "registreringsdato": "2026-03-16T10:30:00Z",
  "beskrivelse": "Kontorkjop Elkjop",
  "eksternReferanse": "F-2026-0042",
  "periode": { "ar": 2026, "periode": 3 },
  "posteringer": [
    {
      "id": "guid",
      "linjenummer": 1,
      "kontonummer": "6540",
      "kontonavn": "Inventar",
      "side": "Debet",
      "belop": 8000.00,
      "beskrivelse": "Kontorutstyr",
      "mvaKode": "3",
      "mvaBelop": 2000.00,
      "mvaGrunnlag": 8000.00,
      "mvaSats": 25.00
    },
    ...
  ],
  "sumDebet": 10000.00,
  "sumKredit": 10000.00
}
```

**Validering:**
| Felt | Regel |
|------|-------|
| type | Pakreves, gyldig BilagType |
| bilagsdato | Pakreves, gyldig dato, kan ikke vaere i fremtiden |
| beskrivelse | Pakreves, maks 500 tegn |
| posteringer | Minimum 2 linjer |
| posteringer[].kontonummer | Ma eksistere, vaere aktiv, og vaere bokforbar |
| posteringer[].side | "Debet" eller "Kredit" |
| posteringer[].belop | Storre enn 0 |
| posteringer[].beskrivelse | Pakreves, maks 500 tegn |
| posteringer[].avdelingskode | Pakreves hvis kontoen krever avdeling |
| posteringer[].prosjektkode | Pakreves hvis kontoen krever prosjekt |
| Sum debet | Ma vaere lik sum kredit |

**Feilkoder:**
- `400 Bad Request` — Valideringsfeil (detaljer i response body)
- `404 Not Found` — Konto finnes ikke
- `409 Conflict` — Perioden er lukket/sperret
- `422 Unprocessable Entity` — Bilag ikke i balanse

---

#### GET /api/bilag/{id}

Hent et bilag med alle posteringer.

**Response (200):** Samme struktur som POST response.

---

#### GET /api/bilag?ar={ar}&periode={periode}&type={type}&side={side}&antall={antall}

Hent bilag med paginering og filtrering.

**Response (200):**
```json
{
  "data": [ /* bilag-objekter */ ],
  "totaltAntall": 142,
  "side": 1,
  "antall": 50
}
```

---

### Kontoutskrift (Account Statement)

#### GET /api/kontoutskrift/{kontonummer}?fraDato={fraDato}&tilDato={tilDato}&side={side}&antall={antall}

Henter alle posteringer for en konto innenfor et datointervall. Dette er kontospesifikasjonen som kreves av bokforingsforskriften 3-1.

**Response (200):**
```json
{
  "kontonummer": "6540",
  "kontonavn": "Inventar",
  "kontotype": "Kostnad",
  "normalbalanse": "Debet",
  "fraDato": "2026-01-01",
  "tilDato": "2026-03-31",
  "inngaendeBalanse": 0.00,
  "posteringer": [
    {
      "bilagsdato": "2026-01-15",
      "bilagsId": "2026-00005",
      "bilagBeskrivelse": "Kontorkjop IKEA",
      "linjenummer": 1,
      "beskrivelse": "Kontorutstyr",
      "side": "Debet",
      "belop": 5000.00,
      "lOpendeBalanse": 5000.00
    },
    {
      "bilagsdato": "2026-03-15",
      "bilagsId": "2026-00042",
      "bilagBeskrivelse": "Kontorkjop Elkjop",
      "linjenummer": 1,
      "beskrivelse": "Kontorutstyr",
      "side": "Debet",
      "belop": 8000.00,
      "lOpendeBalanse": 13000.00
    }
  ],
  "sumDebet": 13000.00,
  "sumKredit": 0.00,
  "utgaendeBalanse": 13000.00,
  "totaltAntall": 2,
  "side": 1,
  "antall": 100
}
```

---

### Saldobalanse (Trial Balance)

#### GET /api/saldobalanse/{ar}/{periode}

Henter saldobalanse for alle kontoer med aktivitet i en gitt periode.

**Response (200):**
```json
{
  "ar": 2026,
  "periode": 3,
  "periodenavn": "2026-03",
  "kontoer": [
    {
      "kontonummer": "1920",
      "kontonavn": "Bankinnskudd",
      "kontotype": "Eiendel",
      "inngaendeBalanse": 500000.00,
      "sumDebet": 150000.00,
      "sumKredit": 80000.00,
      "endring": 70000.00,
      "utgaendeBalanse": 570000.00
    },
    {
      "kontonummer": "2400",
      "kontonavn": "Leverandorgjeld",
      "kontotype": "Gjeld",
      "inngaendeBalanse": -120000.00,
      "sumDebet": 60000.00,
      "sumKredit": 95000.00,
      "endring": -35000.00,
      "utgaendeBalanse": -155000.00
    }
  ],
  "totalSumDebet": 750000.00,
  "totalSumKredit": 750000.00,
  "erIBalanse": true
}
```

**Parametre:**
- `inkluderNullsaldo` (bool, default false) — Inkluder kontoer uten bevegelse
- `kontoklasse` (int, valgfri) — Filtrer pa kontoklasse (1-8)

---

### Saldooppslag (Balance Query)

#### GET /api/saldo/{kontonummer}?ar={ar}&fraPeriode={fraPeriode}&tilPeriode={tilPeriode}

Hent saldoinformasjon for en konto, eventuelt over et periodespenn.

**Response (200):**
```json
{
  "kontonummer": "1920",
  "kontonavn": "Bankinnskudd",
  "ar": 2026,
  "perioder": [
    {
      "periode": 1,
      "inngaendeBalanse": 500000.00,
      "sumDebet": 100000.00,
      "sumKredit": 50000.00,
      "utgaendeBalanse": 550000.00,
      "antallPosteringer": 15
    },
    {
      "periode": 2,
      "inngaendeBalanse": 550000.00,
      "sumDebet": 80000.00,
      "sumKredit": 60000.00,
      "utgaendeBalanse": 570000.00,
      "antallPosteringer": 12
    }
  ],
  "totalInngaendeBalanse": 500000.00,
  "totalSumDebet": 180000.00,
  "totalSumKredit": 110000.00,
  "totalUtgaendeBalanse": 570000.00
}
```

---

### Periodeavstemming (Period Reconciliation)

#### GET /api/perioder/{ar}/{periode}/avstemming

Kontrollerer at perioden er klar for lukking.

**Response (200):**
```json
{
  "ar": 2026,
  "periode": 3,
  "erKlarForLukking": true,
  "kontroller": [
    {
      "navn": "DebetKredittBalanse",
      "beskrivelse": "Sum debet = sum kredit for alle bilag i perioden",
      "status": "OK",
      "detaljer": null
    },
    {
      "navn": "FortlopendeNummer",
      "beskrivelse": "Bilagsnumre er fortlopende uten hull",
      "status": "OK",
      "detaljer": null
    },
    {
      "navn": "SaldoKontroll",
      "beskrivelse": "Materialiserte saldoer stemmer med posteringer",
      "status": "OK",
      "detaljer": null
    },
    {
      "navn": "ForrigePeriodeLukket",
      "beskrivelse": "Forrige periode (2026-02) er lukket",
      "status": "OK",
      "detaljer": null
    },
    {
      "navn": "AlleKontoerHarSaldo",
      "beskrivelse": "Alle kontoer med posteringer har KontoSaldo-rad",
      "status": "OK",
      "detaljer": null
    }
  ]
}
```

**Avstemmingskontroller:**
1. **DebetKredittBalanse** — Sum debet = sum kredit for ALLE bilag i perioden
2. **FortlopendeNummer** — Ingen hull i bilagsnummereringen for aret
3. **SaldoKontroll** — Reberegnet saldo fra posteringer matcher materialisert KontoSaldo
4. **ForrigePeriodeLukket** — Forrige periode (i samme ar) er lukket (unntak: periode 1)
5. **AlleKontoerHarSaldo** — Alle kontoer med posteringer i perioden har en KontoSaldo-rad

---

## Forretningsregler

### FR-HOV-001: Dobbelt bokholderi — absolutt balanse

**Regel:** For hvert bilag MA sum debet vaere eksakt lik sum kredit. Ingen unntak.

**Implementasjon:** Valideres i `Bilag.ValiderBalanse()` for bilaget persisteres. Sjekkes igjen i service-laget.

**Eksempel:**
```
Bilag: Kjop av varer NOK 10.000 inkl. MVA 25%
  Debet  4000 Varekostnad          8.000,00
  Debet  2710 Inngaende MVA        2.000,00
  Kredit 2400 Leverandorgjeld     10.000,00
  ----------------------------------------
  Sum debet:  10.000,00
  Sum kredit: 10.000,00  ✓ Balanse
```

### FR-HOV-002: Posteringer kun mot apen periode

**Regel:** Bilag kan kun opprettes i en regnskapsperiode med status `Apen`. Sperrede og lukkede perioder avviser nye bilag.

**Implementasjon:** Service-laget henter perioden for bilagsdato og kaller `Regnskapsperiode.ValiderApen()`.

### FR-HOV-003: Fortlopende bilagsnummerering

**Regel:** Bilagsnumre tildeles sekvensielt per regnskapsar, startende fra 1. Ingen hull tillates (bokforingsloven 4 nr. 7).

**Implementasjon:** `NestebilagsnummerAsync()` henter MAX(bilagsnummer) + 1 for aret. Unikt indeks sikrer at samtidige innlegg ikke skaper duplikater.

### FR-HOV-004: Bilag ma tilhore korrekt periode

**Regel:** Bilagsdato bestemmer hvilken regnskapsperiode bilaget tilhorer. Bilagsdato MA falle innenfor periodens dato-spenn.

**Implementasjon:** Service-laget slaar opp perioden basert pa bilagsdato og validerer at datoen er innenfor `FraDato` og `TilDato`.

### FR-HOV-005: Kontoen ma vaere bokforbar

**Regel:** Posteringer kan kun gjores mot kontoer der `ErBokforbar = true` og `ErAktiv = true`.

**Implementasjon:** For hver posteringslinje, kall `IKontoService.HentKontoEllerKastAsync()` og sjekk `ErBokforbar` og `ErAktiv`.

### FR-HOV-006: Avdeling og prosjekt pakreves

**Regel:** Hvis en konto har `KreverAvdeling = true`, MA posteringen ha en `Avdelingskode`. Tilsvarende for `KreverProsjekt`.

**Implementasjon:** Valideres per posteringslinje mot kontoens innstillinger.

### FR-HOV-007: Belop ma vaere positivt

**Regel:** Belop i en postering er alltid positivt. Retning bestemmes av `Side` (Debet/Kredit), IKKE av fortegn.

**Implementasjon:** Valider at `Belop.Verdi > 0` for hver posteringslinje.

### FR-HOV-008: Saldo beregnes inkrementelt

**Regel:** `KontoSaldo` oppdateres inkrementelt ved hver bokforing. Utgaende balanse = Inngaende balanse + Sum debet - Sum kredit.

**Implementasjon:**
1. Hent eller opprett `KontoSaldo` for kontoen og perioden
2. Kall `KontoSaldo.LeggTilPostering(side, belop)`
3. Alt skjer i samme databasetransaksjon som bilagopprettelsen

### FR-HOV-009: Inngaende balanse — overforingsprinsipper

**Regel:**
- **Balansekontoer (klasse 1-2):** IB i periode N = UB i periode N-1. Ved arsskifte: IB ar N, periode 1 = UB ar N-1, periode 12.
- **Resultatkontoer (klasse 3-8):** IB nullstilles til 0 ved nytt regnskapsar (etter arsavslutning). Innenfor aret: IB i periode N = UB i periode N-1.

**Implementasjon:** Nar nye perioder opprettes, beregnes IB fra forrige periodes UB. Arsavslutning nullstiller resultatkontoer eksplisitt.

### FR-HOV-010: Periodeavstemming for lukking

**Regel:** En periode kan bare lukkes etter at alle avstemmingskontroller er bestatt:
1. Alle bilag i perioden er i balanse
2. Bilagsnumre er fortlopende
3. Materialiserte saldoer matcher summen av posteringer
4. Forrige periode er lukket (unntatt forste periode i aret)

**Implementasjon:** `GET /api/perioder/{ar}/{periode}/avstemming` kjorer alle kontroller. `PUT /api/perioder/{ar}/{periode}/status` kjorer samme kontroller for lukking tillates.

### FR-HOV-011: Periodelukking er permanent

**Regel:** Nar en periode er lukket (status `Lukket`), kan den IKKE gjenapnes. Korreksjoner bokfores i neste apne periode.

### FR-HOV-012: Sperring er reversibel

**Regel:** En periode kan sperres midlertidig (f.eks. under avstemming) og deretter gjenapnes (`Sperret -> Apen`). Sperring hindrer nye posteringer men er ikke permanent.

### FR-HOV-013: Statusoverganger

**Gyldige overganger:**
```
Apen -> Sperret     (midlertidig sperring)
Sperret -> Apen     (gjenapning)
Sperret -> Lukket   (endelig lukking, etter avstemming)
```

**Ugyldige overganger:**
```
Apen -> Lukket      (ma via Sperret forst)
Lukket -> Sperret   (permanent)
Lukket -> Apen      (permanent)
```

### FR-HOV-014: Atomisk bokforing

**Regel:** Et bilag med alle posteringer og alle saldooppdateringer bokfores i en enkelt databasetransaksjon. Enten alt lykkes eller ingenting endres.

### FR-HOV-015: MVA-sporbarhet

**Regel:** Nar en postering har MVA-kode, lagres MVA-sats, grunnlag og belop som snapshot ved bokforingstidspunkt. Dette sikrer at endringer i MVA-satser ikke pavirker historiske posteringer.

**Implementasjon:** Service-laget henter `MvaKode` fra Kontoplan-modulen, beregner MVA-belop, og lagrer snapshot-verdier pa posteringen.

### FR-HOV-016: Bilag kan ikke slettes

**Regel:** Bilag og posteringer kan ALDRI slettes (hard delete). Korreksjoner gjores ved a opprette et nytt korreksjonsbilag (BilagType.Korreksjon) som reverserer det opprinnelige.

**Implementasjon:** Soft delete er deaktivert for Bilag og Postering. Korreksjon-API-et oppretter et nytt bilag med motstatte posteringer.

### FR-HOV-017: Saldobalanse ma balansere

**Regel:** I saldobalansen MA total sum debet vaere lik total sum kredit. Hvis ikke, er det en feil i systemet.

---

## MVA-handling

### MVA i posteringer

MVA handteres pa posteringsniva. Nar en bruker bokforer med MVA-kode, beregner systemet MVA-belopet og oppretter tilhorende MVA-posteringer.

#### Automatisk MVA-beregning ved bokforing

Nar en postering har `MvaKode`:
1. Hent `MvaKode`-entiteten fra Kontoplan-modulen
2. Beregn MVA-belop: `grunnlag * sats / 100`
3. Lagre `MvaBelop`, `MvaGrunnlag`, og `MvaSats` pa posteringen
4. MVA-kontoposteringen (f.eks. debet 2710 for inngaende MVA) ma inkluderes eksplisitt i bilaget av klienten — systemet validerer kun at summen stemmer

#### SAF-T TaxInformation mapping

```
Postering.MvaKode      -> TaxInformation/TaxCode
Postering.MvaSats      -> TaxInformation/TaxPercentage
Postering.MvaGrunnlag  -> TaxInformation/TaxBase
Postering.MvaBelop     -> TaxInformation/TaxAmount
"MVA"                  -> TaxInformation/TaxType
```

#### Relevante MVA-koder

| Kode | Sats | Retning | Bruk i Hovedbok |
|------|------|---------|----------------|
| 0 | 0% | Ingen | Posteringer uten MVA |
| 1 | 25% | Utgaende | Salg med full MVA |
| 3 | 25% | Inngaende | Kjop med full fradrag — debet 2710 |
| 5 | 0% | Utgaende | Fritatt innenlands salg |
| 6 | 0% | Utgaende | Eksport |
| 11 | 15% | Utgaende | Naringsmiddel |
| 13 | 12% | Utgaende | Persontransport etc. |
| 31 | 15% | Inngaende | Kjop naringsmiddel, fradrag |
| 33 | 12% | Inngaende | Kjop lav sats, fradrag |

---

## SAF-T Mapping

Hovedboken mapper direkte til SAF-T `GeneralLedgerEntries`-seksjonen.

### Mapping-tabell

| SAF-T Element | Kilde |
|---|---|
| NumberOfEntries | COUNT(Bilag) for eksportperioden |
| TotalDebit | SUM(Postering.Belop) WHERE Side = Debet |
| TotalCredit | SUM(Postering.Belop) WHERE Side = Kredit |
| Journal/JournalID | BilagType (en journal per type) |
| Journal/Description | BilagType-navn |
| Journal/Type | BilagType-kode |
| Transaction/TransactionID | Bilag.BilagsId |
| Transaction/Period | Bilag.Bilagsdato.Month |
| Transaction/TransactionDate | Bilag.Bilagsdato |
| Transaction/Description | Bilag.Beskrivelse |
| Transaction/SystemEntryDate | Bilag.Registreringsdato |
| Transaction/GLPostingDate | Bilag.Bilagsdato |
| Line/RecordID | Postering.Id |
| Line/AccountID | Postering.Kontonummer |
| Line/DebitAmount/Amount | Postering.Belop (hvis Side = Debet) |
| Line/CreditAmount/Amount | Postering.Belop (hvis Side = Kredit) |
| Line/Description | Postering.Beskrivelse |
| Line/CustomerID | Postering.KundeId |
| Line/SupplierID | Postering.LeverandorId |
| Line/TaxInformation | Fra Postering.MvaKode/MvaSats/MvaGrunnlag/MvaBelop |

### Journal-gruppering for SAF-T

Bilag grupperes i journaler basert pa `BilagType`:

| BilagType | SAF-T JournalID | SAF-T Type |
|---|---|---|
| Manuelt | "GL" | "GL" |
| InngaendeFaktura | "AP" | "AP" |
| UtgaendeFaktura | "AR" | "AR" |
| Bank | "BK" | "BK" |
| Lonn | "PR" | "PR" |
| Avskrivning | "DA" | "DA" |
| MvaOppgjor | "VAT" | "VAT" |
| Arsavslutning | "CL" | "CL" |
| Apningsbalanse | "OB" | "OB" |
| Kreditnota | "CN" | "CN" |
| Korreksjon | "ADJ" | "ADJ" |

---

## Services

### IHovedbokService

```csharp
namespace Regnskap.Application.Features.Hovedbok;

/// <summary>
/// Service for hovedbok-operasjoner. Orkestrerer forretningslogikk
/// for bilag, posteringer, saldoer og perioder.
/// </summary>
public interface IHovedbokService
{
    // --- Perioder ---
    Task<List<RegnskapsperiodeDto>> OpprettPerioderForArAsync(int ar, CancellationToken ct = default);
    Task<List<RegnskapsperiodeDto>> HentPerioderAsync(int ar, CancellationToken ct = default);
    Task<RegnskapsperiodeDto> EndrePeriodeStatusAsync(
        int ar, int periode, PeriodeStatus nyStatus, string? merknad = null, CancellationToken ct = default);

    // --- Bilag ---
    Task<BilagDto> OpprettBilagAsync(OpprettBilagRequest request, CancellationToken ct = default);
    Task<BilagDto> HentBilagAsync(Guid id, CancellationToken ct = default);
    Task<BilagDto> HentBilagMedNummerAsync(int ar, int bilagsnummer, CancellationToken ct = default);
    Task<(List<BilagDto> Data, int TotaltAntall)> HentBilagListeAsync(
        int ar, int? periode = null, BilagType? type = null,
        int side = 1, int antall = 50, CancellationToken ct = default);

    // --- Kontoutskrift ---
    Task<KontoutskriftDto> HentKontoutskriftAsync(
        string kontonummer, DateOnly fraDato, DateOnly tilDato,
        int side = 1, int antall = 100, CancellationToken ct = default);

    // --- Saldobalanse ---
    Task<SaldobalanseDto> HentSaldobalanseAsync(
        int ar, int periode,
        bool inkluderNullsaldo = false,
        int? kontoklasse = null,
        CancellationToken ct = default);

    // --- Saldooppslag ---
    Task<KontoSaldoOppslagDto> HentKontoSaldoAsync(
        string kontonummer, int ar,
        int? fraPeriode = null, int? tilPeriode = null,
        CancellationToken ct = default);

    // --- Periodeavstemming ---
    Task<PeriodeavstemmingDto> KjorPeriodeavstemmingAsync(
        int ar, int periode, CancellationToken ct = default);
}
```

### DTO-er

```csharp
namespace Regnskap.Application.Features.Hovedbok;

public record RegnskapsperiodeDto(
    Guid Id,
    int Ar,
    int Periode,
    string Periodenavn,
    DateOnly FraDato,
    DateOnly TilDato,
    string Status,
    DateTime? LukketTidspunkt,
    string? LukketAv,
    string? Merknad);

public record BilagDto(
    Guid Id,
    string BilagsId,
    int Bilagsnummer,
    int Ar,
    string Type,
    DateOnly Bilagsdato,
    DateTime Registreringsdato,
    string Beskrivelse,
    string? EksternReferanse,
    RegnskapsperiodeDto Periode,
    List<PosteringDto> Posteringer,
    decimal SumDebet,
    decimal SumKredit);

public record PosteringDto(
    Guid Id,
    int Linjenummer,
    string Kontonummer,
    string Kontonavn,
    string Side,
    decimal Belop,
    string Beskrivelse,
    string? MvaKode,
    decimal? MvaBelop,
    decimal? MvaGrunnlag,
    decimal? MvaSats,
    string? Avdelingskode,
    string? Prosjektkode);

public record OpprettBilagRequest(
    BilagType Type,
    DateOnly Bilagsdato,
    string Beskrivelse,
    string? EksternReferanse,
    List<OpprettPosteringRequest> Posteringer);

public record OpprettPosteringRequest(
    string Kontonummer,
    BokforingSide Side,
    decimal Belop,
    string Beskrivelse,
    string? MvaKode,
    string? Avdelingskode,
    string? Prosjektkode);

public record KontoutskriftDto(
    string Kontonummer,
    string Kontonavn,
    string Kontotype,
    string Normalbalanse,
    DateOnly FraDato,
    DateOnly TilDato,
    decimal InngaendeBalanse,
    List<KontoutskriftLinjeDto> Posteringer,
    decimal SumDebet,
    decimal SumKredit,
    decimal UtgaendeBalanse,
    int TotaltAntall,
    int Side,
    int Antall);

public record KontoutskriftLinjeDto(
    DateOnly Bilagsdato,
    string BilagsId,
    string BilagBeskrivelse,
    int Linjenummer,
    string Beskrivelse,
    string Side,
    decimal Belop,
    decimal LopendeBalanse);

public record SaldobalanseDto(
    int Ar,
    int Periode,
    string Periodenavn,
    List<SaldobalanseLinjeDto> Kontoer,
    decimal TotalSumDebet,
    decimal TotalSumKredit,
    bool ErIBalanse);

public record SaldobalanseLinjeDto(
    string Kontonummer,
    string Kontonavn,
    string Kontotype,
    decimal InngaendeBalanse,
    decimal SumDebet,
    decimal SumKredit,
    decimal Endring,
    decimal UtgaendeBalanse);

public record KontoSaldoOppslagDto(
    string Kontonummer,
    string Kontonavn,
    int Ar,
    List<KontoSaldoPeriodeDto> Perioder,
    decimal TotalInngaendeBalanse,
    decimal TotalSumDebet,
    decimal TotalSumKredit,
    decimal TotalUtgaendeBalanse);

public record KontoSaldoPeriodeDto(
    int Periode,
    decimal InngaendeBalanse,
    decimal SumDebet,
    decimal SumKredit,
    decimal UtgaendeBalanse,
    int AntallPosteringer);

public record PeriodeavstemmingDto(
    int Ar,
    int Periode,
    bool ErKlarForLukking,
    List<AvstemmingKontrollDto> Kontroller);

public record AvstemmingKontrollDto(
    string Navn,
    string Beskrivelse,
    string Status,
    string? Detaljer);
```

---

## Avhengigheter

### Bruker fra Kontoplan (implementert)

| Interface/Service | Bruk |
|---|---|
| `IKontoService.HentKontoEllerKastAsync()` | Validere at konto eksisterer og er aktiv |
| `IKontoService.KontoFinnesOgErAktivAsync()` | Rask eksistenssjekk |
| `IKontoService.HentKontotypeAsync()` | Bestemme kontotype for rapporter |
| `IKontoService.HentNormalbalanseAsync()` | Bestemme normalbalanse for rapporter |
| `Konto.ErBokforbar` | Sjekke om konto kan bokfores mot |
| `Konto.KreverAvdeling` | Sjekke om avdeling pakreves |
| `Konto.KreverProsjekt` | Sjekke om prosjekt pakreves |
| `MvaKode` | Hente MVA-sats for beregning |

### Eksponerer til fremtidige moduler

| Interface/Entitet | Konsument |
|---|---|
| `IHovedbokService.OpprettBilagAsync()` | Bilag-modul, Leverandorreskontro, Kundereskontro |
| `IHovedbokService.HentKontoSaldoAsync()` | Rapportering, Arsavslutning |
| `IHovedbokService.HentSaldobalanseAsync()` | MVA-oppgjor, Arsregnskap |
| `Regnskapsperiode` | Alle moduler som trenger periodevalidering |
| `Bilag` / `Postering` | SAF-T eksport |

### TODO: Implementeres i fremtidige moduler

- **Bilag-modul:** Utvidet bilagshanding med vedlegg, workflow, og OCR
- **Kundereskontro:** `KundeId` pa posteringer, koblinger mot kundebilag
- **Leverandorreskontro:** `LeverandorId` pa posteringer, koblinger mot leverandorbilag
- **Avdeling/Prosjekt:** Stamdata for avdelings- og prosjektkoder
- **SAF-T eksport:** Generering av SAF-T XML fra Hovedbok-data
- **Arsavslutning:** Nullstilling av resultatkontoer, overfolring til EK

---

## Bokforingsloven-samsvar

| Krav (Bokforingsloven) | Hvordan oppfylt |
|---|---|
| 4 nr. 2 Fullstendighet | Alle transaksjoner ma bokfores via bilag |
| 4 nr. 4 Noyaktighet | decimal(18,2) presisjon, Belop value object |
| 4 nr. 5 Ajourhold | Perioder med statustyring, service advarer om ajourhold |
| 4 nr. 6 Dokumentasjon | Bilag med beskrivelse, ekstern referanse, revisjonsspor |
| 4 nr. 7 Sporbarhet | Fra rapport -> saldo -> postering -> bilag og tilbake |
| 5 nr. 2 Kontospesifikasjon | GET /api/kontoutskrift/{kontonummer} |
| 10 Fortlopende nummerering | Bilagsnummer sekvensielt per ar, unik indeks |
| 13 Oppbevaring | Soft delete, aldri hard delete, AuditableEntity |

---

## Filstruktur

```
src/
  Regnskap.Domain/
    Features/
      Hovedbok/
        Enums.cs                    # PeriodeStatus, BilagType, BokforingSide
        Regnskapsperiode.cs         # Entity
        Bilag.cs                    # Entity
        Postering.cs                # Entity
        KontoSaldo.cs               # Entity
        Exceptions.cs               # Domene-exceptions
        IHovedbokRepository.cs      # Repository-interface

  Regnskap.Application/
    Features/
      Hovedbok/
        IHovedbokService.cs         # Service-interface
        Dtos.cs                     # Alle DTO-er og request-typer
        HovedbokService.cs          # Implementasjon

  Regnskap.Infrastructure/
    Persistence/
      Configurations/
        RegnskapsperiodeConfiguration.cs
        BilagConfiguration.cs
        PosteringConfiguration.cs
        KontoSaldoConfiguration.cs
      Repositories/
        HovedbokRepository.cs       # Repository-implementasjon

  Regnskap.Api/
    Controllers/
      PerioderController.cs         # Perioder-endepunkter
      BilagController.cs            # Bilag-endepunkter
      KontoutskriftController.cs    # Kontoutskrift-endepunkt
      SaldobalanseController.cs     # Saldobalanse-endepunkt
      SaldoController.cs            # Saldooppslag-endepunkt
```
