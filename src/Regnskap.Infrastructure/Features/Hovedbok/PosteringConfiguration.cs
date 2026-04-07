using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Infrastructure.Features.Hovedbok;

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
            .HasFilter("\"IsDeleted\" = false");

        // Ytelseindekser
        builder.HasIndex(e => e.KontoId);
        builder.HasIndex(e => e.Kontonummer);
        builder.HasIndex(e => e.Bilagsdato);
        builder.HasIndex(e => new { e.Kontonummer, e.Bilagsdato });
        builder.HasIndex(e => e.KundeId).HasFilter("\"KundeId\" IS NOT NULL");
        builder.HasIndex(e => e.LeverandorId).HasFilter("\"LeverandorId\" IS NOT NULL");
        builder.HasIndex(e => e.Avdelingskode).HasFilter("\"Avdelingskode\" IS NOT NULL");
        builder.HasIndex(e => e.Prosjektkode).HasFilter("\"Prosjektkode\" IS NOT NULL");
    }
}
