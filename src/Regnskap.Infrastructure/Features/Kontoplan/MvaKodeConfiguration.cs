using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Infrastructure.Features.Kontoplan;

public class MvaKodeConfiguration : IEntityTypeConfiguration<MvaKode>
{
    public void Configure(EntityTypeBuilder<MvaKode> builder)
    {
        builder.ToTable("MvaKoder");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Kode)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(k => k.Kode)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(k => k.Beskrivelse)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(k => k.BeskrivelseEn)
            .HasMaxLength(300);

        builder.Property(k => k.StandardTaxCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(k => k.Sats)
            .HasPrecision(5, 2);

        builder.HasOne(k => k.UtgaendeKonto)
            .WithMany()
            .HasForeignKey(k => k.UtgaendeKontoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(k => k.InngaendeKonto)
            .WithMany()
            .HasForeignKey(k => k.InngaendeKontoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
