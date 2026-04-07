using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Features.Bilagsregistrering;

namespace Regnskap.Infrastructure.Features.Bilagsregistrering;

public class VedleggConfiguration : IEntityTypeConfiguration<Vedlegg>
{
    public void Configure(EntityTypeBuilder<Vedlegg> builder)
    {
        builder.ToTable("Vedlegg");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Filnavn).HasMaxLength(500).IsRequired();
        builder.Property(v => v.MimeType).HasMaxLength(100).IsRequired();
        builder.Property(v => v.LagringSti).HasMaxLength(1000).IsRequired();
        builder.Property(v => v.HashSha256).HasMaxLength(64).IsRequired();
        builder.Property(v => v.Beskrivelse).HasMaxLength(500);
        builder.HasIndex(v => v.BilagId);
        builder.HasOne(v => v.Bilag)
            .WithMany(b => b.Vedlegg)
            .HasForeignKey(v => v.BilagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
