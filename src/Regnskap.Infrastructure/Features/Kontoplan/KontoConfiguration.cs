using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Features.Kontoplan;

namespace Regnskap.Infrastructure.Features.Kontoplan;

public class KontoConfiguration : IEntityTypeConfiguration<Konto>
{
    public void Configure(EntityTypeBuilder<Konto> builder)
    {
        builder.ToTable("Kontoer");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Kontonummer)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(k => k.Kontonummer)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.Property(k => k.Navn)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(k => k.NavnEn)
            .HasMaxLength(200);

        builder.Property(k => k.StandardAccountId)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(k => k.GrupperingsKode)
            .HasMaxLength(50);

        builder.Property(k => k.StandardMvaKode)
            .HasMaxLength(10);

        builder.Property(k => k.Beskrivelse)
            .HasMaxLength(1000);

        builder.HasOne(k => k.OverordnetKonto)
            .WithMany(k => k.Underkontoer)
            .HasForeignKey(k => k.OverordnetKontoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(k => k.Kontogruppe)
            .WithMany(k => k.Kontoer)
            .HasForeignKey(k => k.KontogruppeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(k => k.Kontoklasse);
        builder.Ignore(k => k.ErBalansekonto);
        builder.Ignore(k => k.ErUnderkonto);
    }
}
