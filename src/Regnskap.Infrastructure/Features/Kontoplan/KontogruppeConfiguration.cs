using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Infrastructure.Features.Kontoplan;

public class KontogruppeConfiguration : IEntityTypeConfiguration<Kontogruppe>
{
    public void Configure(EntityTypeBuilder<Kontogruppe> builder)
    {
        builder.ToTable("Kontogrupper");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Gruppekode)
            .IsRequired();

        builder.HasIndex(k => k.Gruppekode)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(k => k.Navn)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(k => k.NavnEn)
            .HasMaxLength(200);

        builder.Ignore(k => k.Kontoklasse);

        builder.HasMany(k => k.Kontoer)
            .WithOne(k => k.Kontogruppe)
            .HasForeignKey(k => k.KontogruppeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
