using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Infrastructure.Features.Hovedbok;

public class KontoSaldoConfiguration : IEntityTypeConfiguration<KontoSaldo>
{
    public void Configure(EntityTypeBuilder<KontoSaldo> builder)
    {
        builder.ToTable("KontoSaldoer");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Kontonummer)
            .HasMaxLength(10)
            .IsRequired();
        builder.Property(e => e.Ar).IsRequired();
        builder.Property(e => e.Periode).IsRequired();
        builder.Property(e => e.AntallPosteringer).IsRequired();

        // Belop-kolonner
        builder.Property(e => e.InngaendeBalanse)
            .HasConversion(v => v.Verdi, v => new Belop(v))
            .HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.SumDebet)
            .HasConversion(v => v.Verdi, v => new Belop(v))
            .HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(e => e.SumKredit)
            .HasConversion(v => v.Verdi, v => new Belop(v))
            .HasColumnType("decimal(18,2)").IsRequired();

        // Unik saldo per konto per periode
        builder.HasIndex(e => new { e.KontoId, e.RegnskapsperiodeId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        // FK
        builder.HasOne(e => e.Konto)
            .WithMany()
            .HasForeignKey(e => e.KontoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Regnskapsperiode)
            .WithMany(p => p.KontoSaldoer)
            .HasForeignKey(e => e.RegnskapsperiodeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ytelseindekser
        builder.HasIndex(e => new { e.Kontonummer, e.Ar, e.Periode });
        builder.HasIndex(e => new { e.Ar, e.Periode });
    }
}
