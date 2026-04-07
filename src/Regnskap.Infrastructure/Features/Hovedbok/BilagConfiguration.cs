using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Infrastructure.Features.Hovedbok;

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
            .HasFilter("\"IsDeleted\" = false");

        // FK til regnskapsperiode
        builder.HasOne(e => e.Regnskapsperiode)
            .WithMany()
            .HasForeignKey(e => e.RegnskapsperiodeId)
            .OnDelete(DeleteBehavior.Restrict);

        // --- Bilagserie-utvidelser ---
        builder.Property(e => e.SerieKode).HasMaxLength(10);
        builder.Property(e => e.BokfortAv).HasMaxLength(200);

        builder.HasOne(e => e.BilagSerie)
            .WithMany()
            .HasForeignKey(e => e.BilagSerieId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.SerieKode, e.Ar, e.SerieNummer })
            .IsUnique()
            .HasFilter("\"SerieKode\" IS NOT NULL AND \"IsDeleted\" = false");

        // Tilbakeforing - self-referencing
        builder.HasOne(e => e.TilbakefortFraBilag)
            .WithOne()
            .HasForeignKey<Bilag>(e => e.TilbakefortFraBilagId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore computed navigation (TilbakefortAvBilag is set manually, not via FK config)
        builder.Ignore(e => e.TilbakefortAvBilag);

        // Ytelseindekser
        builder.HasIndex(e => e.Bilagsdato);
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.RegnskapsperiodeId);
        builder.HasIndex(e => e.ErBokfort);
        builder.HasIndex(e => e.ErTilbakfort);
    }
}
