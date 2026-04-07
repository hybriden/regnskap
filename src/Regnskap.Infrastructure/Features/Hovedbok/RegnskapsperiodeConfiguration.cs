using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Infrastructure.Features.Hovedbok;

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
            .HasFilter("\"IsDeleted\" = false");

        // Ytelseindeks
        builder.HasIndex(e => e.Status);
    }
}
