using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Features.Periodeavslutning;

namespace Regnskap.Infrastructure.Features.Periodeavslutning;

public class AnleggsmiddelConfiguration : IEntityTypeConfiguration<Anleggsmiddel>
{
    public void Configure(EntityTypeBuilder<Anleggsmiddel> builder)
    {
        builder.ToTable("Anleggsmiddel");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Navn).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Beskrivelse).HasMaxLength(500);
        builder.Property(e => e.Anskaffelseskostnad).HasPrecision(18, 2);
        builder.Property(e => e.Restverdi).HasPrecision(18, 2);
        builder.Property(e => e.BalanseKontonummer).IsRequired().HasMaxLength(6);
        builder.Property(e => e.AvskrivningsKontonummer).IsRequired().HasMaxLength(6);
        builder.Property(e => e.AkkumulertAvskrivningKontonummer).IsRequired().HasMaxLength(6);
        builder.Property(e => e.Avdelingskode).HasMaxLength(20);
        builder.Property(e => e.Prosjektkode).HasMaxLength(20);
        builder.HasMany(e => e.Avskrivninger)
            .WithOne(a => a.Anleggsmiddel)
            .HasForeignKey(a => a.AnleggsmiddelId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.BalanseKontonummer);

        // Ignorer computed properties
        builder.Ignore(e => e.Avskrivningsgrunnlag);
        builder.Ignore(e => e.ManedligAvskrivning);
        builder.Ignore(e => e.ArligAvskrivning);
        builder.Ignore(e => e.AkkumulertAvskrivning);
        builder.Ignore(e => e.BokfortVerdi);
        builder.Ignore(e => e.GjenvaerendeAvskrivning);
        builder.Ignore(e => e.ErFulltAvskrevet);
    }
}

public class AvskrivningHistorikkConfiguration : IEntityTypeConfiguration<AvskrivningHistorikk>
{
    public void Configure(EntityTypeBuilder<AvskrivningHistorikk> builder)
    {
        builder.ToTable("AvskrivningHistorikk");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Belop).HasPrecision(18, 2);
        builder.Property(e => e.AkkumulertEtter).HasPrecision(18, 2);
        builder.Property(e => e.BokfortVerdiEtter).HasPrecision(18, 2);
        builder.HasIndex(e => new { e.AnleggsmiddelId, e.Ar, e.Periode })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class PeriodiseringConfiguration : IEntityTypeConfiguration<Periodisering>
{
    public void Configure(EntityTypeBuilder<Periodisering> builder)
    {
        builder.ToTable("Periodisering");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Beskrivelse).IsRequired().HasMaxLength(500);
        builder.Property(e => e.TotalBelop).HasPrecision(18, 2);
        builder.Property(e => e.Valuta).HasMaxLength(3);
        builder.Property(e => e.BalanseKontonummer).IsRequired().HasMaxLength(6);
        builder.Property(e => e.ResultatKontonummer).IsRequired().HasMaxLength(6);
        builder.Property(e => e.Avdelingskode).HasMaxLength(20);
        builder.Property(e => e.Prosjektkode).HasMaxLength(20);
        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(40);
        builder.HasMany(e => e.Posteringer)
            .WithOne(p => p.Periodisering)
            .HasForeignKey(p => p.PeriodiseringId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignorer computed properties
        builder.Ignore(e => e.AntallPerioder);
        builder.Ignore(e => e.BelopPerPeriode);
        builder.Ignore(e => e.SumPeriodisert);
        builder.Ignore(e => e.GjenstaendeBelop);
    }
}

public class PeriodiseringsHistorikkConfiguration : IEntityTypeConfiguration<PeriodiseringsHistorikk>
{
    public void Configure(EntityTypeBuilder<PeriodiseringsHistorikk> builder)
    {
        builder.ToTable("PeriodiseringsHistorikk");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Belop).HasPrecision(18, 2);
        builder.Property(e => e.AkkumulertEtter).HasPrecision(18, 2);
        builder.Property(e => e.GjenstaarEtter).HasPrecision(18, 2);
        builder.HasIndex(e => new { e.PeriodiseringId, e.Ar, e.Periode })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}

public class PeriodeLukkingLoggConfiguration : IEntityTypeConfiguration<PeriodeLukkingLogg>
{
    public void Configure(EntityTypeBuilder<PeriodeLukkingLogg> builder)
    {
        builder.ToTable("PeriodeLukkingLogg");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Beskrivelse).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Detaljer).HasMaxLength(2000);
        builder.Property(e => e.UtfortAv).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Steg)
            .HasConversion<string>()
            .HasMaxLength(40);
        builder.HasIndex(e => new { e.Ar, e.Periode, e.Steg });
    }
}

public class ArsavslutningStatusConfiguration : IEntityTypeConfiguration<ArsavslutningStatus>
{
    public void Configure(EntityTypeBuilder<ArsavslutningStatus> builder)
    {
        builder.ToTable("ArsavslutningStatus");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Arsresultat).HasPrecision(18, 2);
        builder.Property(e => e.DisponeringKontonummer).HasMaxLength(6);
        builder.Property(e => e.FullfortAv).HasMaxLength(100);
        builder.Property(e => e.Fase)
            .HasConversion<string>()
            .HasMaxLength(40);
        builder.HasIndex(e => e.Ar)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
    }
}
