using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Leverandorreskontro;

namespace Regnskap.Infrastructure.Features.Leverandorreskontro;

public class LeverandorConfiguration : IEntityTypeConfiguration<Leverandor>
{
    public void Configure(EntityTypeBuilder<Leverandor> builder)
    {
        builder.ToTable("Leverandorer");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Leverandornummer)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Navn)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Organisasjonsnummer).HasMaxLength(9);
        builder.Property(e => e.Adresse1).HasMaxLength(200);
        builder.Property(e => e.Adresse2).HasMaxLength(200);
        builder.Property(e => e.Postnummer).HasMaxLength(4);
        builder.Property(e => e.Poststed).HasMaxLength(100);
        builder.Property(e => e.Landkode).HasMaxLength(2).IsRequired();
        builder.Property(e => e.Kontaktperson).HasMaxLength(200);
        builder.Property(e => e.Telefon).HasMaxLength(20);
        builder.Property(e => e.Epost).HasMaxLength(200);
        builder.Property(e => e.Bankkontonummer).HasMaxLength(11);
        builder.Property(e => e.Iban).HasMaxLength(34);
        builder.Property(e => e.Bic).HasMaxLength(11);
        builder.Property(e => e.Banknavn).HasMaxLength(200);
        builder.Property(e => e.StandardMvaKode).HasMaxLength(10);
        builder.Property(e => e.Valutakode).HasMaxLength(3).IsRequired();
        builder.Property(e => e.Notat).HasMaxLength(2000);

        builder.Property(e => e.Betalingsbetingelse)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(e => e.Leverandornummer)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(e => e.Organisasjonsnummer)
            .IsUnique()
            .HasFilter("\"Organisasjonsnummer\" IS NOT NULL AND \"IsDeleted\" = false");

        builder.HasIndex(e => e.Navn);
        builder.HasIndex(e => e.ErAktiv);

        // Ignore computed property
        builder.Ignore(e => e.SaftSupplierId);

        builder.HasMany(e => e.Fakturaer)
            .WithOne(f => f.Leverandor)
            .HasForeignKey(f => f.LeverandorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeverandorFakturaConfiguration : IEntityTypeConfiguration<LeverandorFaktura>
{
    public void Configure(EntityTypeBuilder<LeverandorFaktura> builder)
    {
        builder.ToTable("LeverandorFakturaer");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EksternFakturanummer)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Beskrivelse)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.KidNummer).HasMaxLength(25);
        builder.Property(e => e.Valutakode).HasMaxLength(3).IsRequired();
        builder.Property(e => e.SperreArsak).HasMaxLength(500);

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Valutakurs).HasColumnType("decimal(10,6)");

        // Belop-felter
        builder.Property(e => e.BelopEksMva)
            .HasConversion(v => v.Verdi, v => new Belop(v))
            .HasColumnType("decimal(18,2)");
        builder.Property(e => e.MvaBelop)
            .HasConversion(v => v.Verdi, v => new Belop(v))
            .HasColumnType("decimal(18,2)");
        builder.Property(e => e.BelopInklMva)
            .HasConversion(v => v.Verdi, v => new Belop(v))
            .HasColumnType("decimal(18,2)");
        builder.Property(e => e.GjenstaendeBelop)
            .HasConversion(v => v.Verdi, v => new Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(e => new { e.LeverandorId, e.EksternFakturanummer })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(e => e.InternNummer)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(e => e.Forfallsdato);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.BilagId);

        builder.HasOne(e => e.Bilag)
            .WithMany()
            .HasForeignKey(e => e.BilagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.KreditnotaForFaktura)
            .WithMany()
            .HasForeignKey(e => e.KreditnotaForFakturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Linjer)
            .WithOne(l => l.LeverandorFaktura)
            .HasForeignKey(l => l.LeverandorFakturaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Betalinger)
            .WithOne(b => b.LeverandorFaktura)
            .HasForeignKey(b => b.LeverandorFakturaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeverandorFakturaLinjeConfiguration : IEntityTypeConfiguration<LeverandorFakturaLinje>
{
    public void Configure(EntityTypeBuilder<LeverandorFakturaLinje> builder)
    {
        builder.ToTable("LeverandorFakturaLinjer");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Kontonummer)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.Beskrivelse)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.MvaKode).HasMaxLength(10);
        builder.Property(e => e.Avdelingskode).HasMaxLength(20);
        builder.Property(e => e.Prosjektkode).HasMaxLength(20);

        builder.Property(e => e.Belop)
            .HasConversion(v => v.Verdi, v => new Belop(v))
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.MvaBelop)
            .HasConversion(
                v => v.HasValue ? v.Value.Verdi : (decimal?)null,
                v => v.HasValue ? new Belop(v.Value) : null)
            .HasColumnType("decimal(18,2)");

        builder.Property(e => e.MvaSats).HasColumnType("decimal(5,2)");

        builder.HasIndex(e => new { e.LeverandorFakturaId, e.Linjenummer })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class LeverandorBetalingConfiguration : IEntityTypeConfiguration<LeverandorBetaling>
{
    public void Configure(EntityTypeBuilder<LeverandorBetaling> builder)
    {
        builder.ToTable("LeverandorBetalinger");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Bankreferanse).HasMaxLength(100);
        builder.Property(e => e.Betalingsmetode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Belop)
            .HasConversion(v => v.Verdi, v => new Belop(v))
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne(e => e.Bilag)
            .WithMany()
            .HasForeignKey(e => e.BilagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Betalingsforslag)
            .WithMany()
            .HasForeignKey(e => e.BetalingsforslagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.LeverandorFakturaId);
        builder.HasIndex(e => e.Betalingsdato);
    }
}

public class BetalingsforslagConfiguration : IEntityTypeConfiguration<Betalingsforslag>
{
    public void Configure(EntityTypeBuilder<Betalingsforslag> builder)
    {
        builder.ToTable("Betalingsforslag");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Beskrivelse)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.FraKontonummer).HasMaxLength(34);
        builder.Property(e => e.FraBic).HasMaxLength(11);
        builder.Property(e => e.BetalingsfilReferanse).HasMaxLength(200);
        builder.Property(e => e.GodkjentAv).HasMaxLength(200);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.TotalBelop)
            .HasConversion(v => v.Verdi, v => new Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(e => e.Forslagsnummer)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(e => e.Status);

        builder.HasMany(e => e.Linjer)
            .WithOne(l => l.Betalingsforslag)
            .HasForeignKey(l => l.BetalingsforslagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class BetalingsforslagLinjeConfiguration : IEntityTypeConfiguration<BetalingsforslagLinje>
{
    public void Configure(EntityTypeBuilder<BetalingsforslagLinje> builder)
    {
        builder.ToTable("BetalingsforslagLinjer");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.MottakerKontonummer).HasMaxLength(34);
        builder.Property(e => e.MottakerIban).HasMaxLength(34);
        builder.Property(e => e.MottakerBic).HasMaxLength(11);
        builder.Property(e => e.KidNummer).HasMaxLength(25);
        builder.Property(e => e.Melding).HasMaxLength(140);
        builder.Property(e => e.EndToEndId).HasMaxLength(35);
        builder.Property(e => e.Feilmelding).HasMaxLength(500);

        builder.Property(e => e.Belop)
            .HasConversion(v => v.Verdi, v => new Belop(v))
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne(e => e.LeverandorFaktura)
            .WithMany()
            .HasForeignKey(e => e.LeverandorFakturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Leverandor)
            .WithMany()
            .HasForeignKey(e => e.LeverandorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.BetalingsforslagId);
        builder.HasIndex(e => e.LeverandorFakturaId);
    }
}
