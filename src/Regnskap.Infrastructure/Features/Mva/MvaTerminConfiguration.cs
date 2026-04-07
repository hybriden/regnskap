using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Features.Mva;

namespace Regnskap.Infrastructure.Features.Mva;

public class MvaTerminConfiguration : IEntityTypeConfiguration<MvaTermin>
{
    public void Configure(EntityTypeBuilder<MvaTermin> builder)
    {
        builder.ToTable("MvaTerminer");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.Ar, e.Termin })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(e => e.Status);

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.AvsluttetAv)
            .HasMaxLength(200);
    }
}

public class MvaOppgjorConfiguration : IEntityTypeConfiguration<MvaOppgjor>
{
    public void Configure(EntityTypeBuilder<MvaOppgjor> builder)
    {
        builder.ToTable("MvaOppgjor");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.MvaTerminId)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(e => e.MvaTermin)
            .WithOne()
            .HasForeignKey<MvaOppgjor>(e => e.MvaTerminId);

        builder.Property(e => e.BeregnetAv)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.SumUtgaendeMva).HasPrecision(18, 2);
        builder.Property(e => e.SumInngaendeMva).HasPrecision(18, 2);
        builder.Property(e => e.SumSnuddAvregningUtgaende).HasPrecision(18, 2);
        builder.Property(e => e.SumSnuddAvregningInngaende).HasPrecision(18, 2);
        builder.Property(e => e.MvaTilBetaling).HasPrecision(18, 2);
    }
}

public class MvaOppgjorLinjeConfiguration : IEntityTypeConfiguration<MvaOppgjorLinje>
{
    public void Configure(EntityTypeBuilder<MvaOppgjorLinje> builder)
    {
        builder.ToTable("MvaOppgjorLinjer");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.MvaOppgjorId, e.MvaKode })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(e => e.MvaOppgjor)
            .WithMany(o => o.Linjer)
            .HasForeignKey(e => e.MvaOppgjorId);

        builder.Property(e => e.MvaKode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.StandardTaxCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.Retning)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.SumGrunnlag).HasPrecision(18, 2);
        builder.Property(e => e.SumMvaBelop).HasPrecision(18, 2);
    }
}

public class MvaAvstemmingConfiguration : IEntityTypeConfiguration<MvaAvstemming>
{
    public void Configure(EntityTypeBuilder<MvaAvstemming> builder)
    {
        builder.ToTable("MvaAvstemming");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.MvaTerminId);

        builder.HasOne(e => e.MvaTermin)
            .WithMany()
            .HasForeignKey(e => e.MvaTerminId);

        builder.Property(e => e.AvstemmingAv)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Merknad)
            .HasMaxLength(2000);
    }
}

public class MvaAvstemmingLinjeConfiguration : IEntityTypeConfiguration<MvaAvstemmingLinje>
{
    public void Configure(EntityTypeBuilder<MvaAvstemmingLinje> builder)
    {
        builder.ToTable("MvaAvstemmingLinjer");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.MvaAvstemmingId, e.Kontonummer })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(e => e.MvaAvstemming)
            .WithMany(a => a.Linjer)
            .HasForeignKey(e => e.MvaAvstemmingId);

        builder.Property(e => e.Kontonummer)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.Kontonavn)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.SaldoIflgHovedbok).HasPrecision(18, 2);
        builder.Property(e => e.BeregnetFraPosteringer).HasPrecision(18, 2);
        builder.Property(e => e.Avvik).HasPrecision(18, 2);
    }
}
