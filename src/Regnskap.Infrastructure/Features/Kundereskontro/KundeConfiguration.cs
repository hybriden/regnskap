using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Features.Kundereskontro;

namespace Regnskap.Infrastructure.Features.Kundereskontro;

public class KundeConfiguration : IEntityTypeConfiguration<Kunde>
{
    public void Configure(EntityTypeBuilder<Kunde> builder)
    {
        builder.ToTable("Kunder");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Kundenummer).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Navn).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Organisasjonsnummer).HasMaxLength(9);
        builder.Property(e => e.Fodselsnummer).HasMaxLength(11);
        builder.Property(e => e.Adresse1).HasMaxLength(200);
        builder.Property(e => e.Adresse2).HasMaxLength(200);
        builder.Property(e => e.Postnummer).HasMaxLength(10);
        builder.Property(e => e.Poststed).HasMaxLength(100);
        builder.Property(e => e.Landkode).HasMaxLength(2).IsRequired();
        builder.Property(e => e.Kontaktperson).HasMaxLength(200);
        builder.Property(e => e.Telefon).HasMaxLength(30);
        builder.Property(e => e.Epost).HasMaxLength(200);
        builder.Property(e => e.Notat).HasMaxLength(2000);
        builder.Property(e => e.PeppolId).HasMaxLength(50);
        builder.Property(e => e.StandardMvaKode).HasMaxLength(10);
        builder.Property(e => e.Valutakode).HasMaxLength(3).IsRequired();

        builder.Property(e => e.Betalingsbetingelse)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.KidAlgoritme)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(e => e.Kredittgrense)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        // Indekser
        builder.HasIndex(e => e.Kundenummer)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(e => e.Organisasjonsnummer)
            .IsUnique()
            .HasFilter("\"Organisasjonsnummer\" IS NOT NULL AND \"IsDeleted\" = false");

        builder.HasIndex(e => e.Navn);

        builder.HasIndex(e => e.Fodselsnummer)
            .HasFilter("\"Fodselsnummer\" IS NOT NULL");

        // Ignorer computed property
        builder.Ignore(e => e.SaftCustomerId);
    }
}

public class KundeFakturaConfiguration : IEntityTypeConfiguration<KundeFaktura>
{
    public void Configure(EntityTypeBuilder<KundeFaktura> builder)
    {
        builder.ToTable("KundeFakturaer");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Fakturanummer).IsRequired();
        builder.Property(e => e.Beskrivelse).HasMaxLength(500).IsRequired();
        builder.Property(e => e.KidNummer).HasMaxLength(25);
        builder.Property(e => e.Valutakode).HasMaxLength(3).IsRequired();
        builder.Property(e => e.EksternReferanse).HasMaxLength(100);
        builder.Property(e => e.Bestillingsnummer).HasMaxLength(50);

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.BelopEksMva)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.MvaBelop)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.BelopInklMva)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.GjenstaendeBelop)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.PurregebyrTotalt)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        // FK
        builder.HasOne(e => e.Kunde)
            .WithMany(k => k.Fakturaer)
            .HasForeignKey(e => e.KundeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Bilag)
            .WithMany()
            .HasForeignKey(e => e.BilagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.KreditnotaForFaktura)
            .WithMany()
            .HasForeignKey(e => e.KreditnotaForFakturaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indekser
        builder.HasIndex(e => e.Fakturanummer)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(e => new { e.KundeId, e.Fakturadato });
        builder.HasIndex(e => e.Forfallsdato);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.KidNummer);
        builder.HasIndex(e => e.BilagId);
    }
}

public class KundeFakturaLinjeConfiguration : IEntityTypeConfiguration<KundeFakturaLinje>
{
    public void Configure(EntityTypeBuilder<KundeFakturaLinje> builder)
    {
        builder.ToTable("KundeFakturaLinjer");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Kontonummer).HasMaxLength(10).IsRequired();
        builder.Property(e => e.Beskrivelse).HasMaxLength(500).IsRequired();
        builder.Property(e => e.MvaKode).HasMaxLength(10);
        builder.Property(e => e.Avdelingskode).HasMaxLength(20);
        builder.Property(e => e.Prosjektkode).HasMaxLength(20);
        builder.Property(e => e.Antall).HasColumnType("decimal(18,4)");
        builder.Property(e => e.Rabatt).HasColumnType("decimal(5,2)");
        builder.Property(e => e.MvaSats).HasColumnType("decimal(5,2)");

        builder.Property(e => e.Enhetspris)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.Belop)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.MvaBelop)
            .HasConversion(v => v!.Value.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.HasOne(e => e.KundeFaktura)
            .WithMany(f => f.Linjer)
            .HasForeignKey(e => e.KundeFakturaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.KundeFakturaId, e.Linjenummer }).IsUnique();
    }
}

public class KundeInnbetalingConfiguration : IEntityTypeConfiguration<KundeInnbetaling>
{
    public void Configure(EntityTypeBuilder<KundeInnbetaling> builder)
    {
        builder.ToTable("KundeInnbetalinger");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Bankreferanse).HasMaxLength(100);
        builder.Property(e => e.KidNummer).HasMaxLength(25);
        builder.Property(e => e.Betalingsmetode).HasMaxLength(30).IsRequired();

        builder.Property(e => e.Belop)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.HasOne(e => e.KundeFaktura)
            .WithMany(f => f.Innbetalinger)
            .HasForeignKey(e => e.KundeFakturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Bilag)
            .WithMany()
            .HasForeignKey(e => e.BilagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.Innbetalingsdato);
        builder.HasIndex(e => e.KidNummer);
    }
}

public class PurringConfiguration : IEntityTypeConfiguration<Purring>
{
    public void Configure(EntityTypeBuilder<Purring> builder)
    {
        builder.ToTable("Purringer");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.Sendemetode).HasMaxLength(30);

        builder.Property(e => e.Gebyr)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.Forsinkelsesrente)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.HasOne(e => e.KundeFaktura)
            .WithMany(f => f.Purringer)
            .HasForeignKey(e => e.KundeFakturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.GebyrBilag)
            .WithMany()
            .HasForeignKey(e => e.GebyrBilagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.KundeFakturaId);
        builder.HasIndex(e => e.Purringsdato);
    }
}
