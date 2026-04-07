using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Fakturering;

namespace Regnskap.Infrastructure.Features.Fakturering;

public class FakturaConfiguration : IEntityTypeConfiguration<Faktura>
{
    public void Configure(EntityTypeBuilder<Faktura> builder)
    {
        builder.HasKey(f => f.Id);

        builder.HasIndex(f => new { f.FakturanummerAr, f.Fakturanummer })
            .IsUnique()
            .HasFilter("\"Fakturanummer\" IS NOT NULL");

        builder.HasIndex(f => f.KundeId);
        builder.HasIndex(f => f.Status);
        builder.HasIndex(f => f.Fakturadato);
        builder.HasIndex(f => f.KidNummer).HasFilter("\"KidNummer\" IS NOT NULL");
        builder.HasIndex(f => f.KreditertFakturaId).HasFilter("\"KreditertFakturaId\" IS NOT NULL");

        builder.HasOne(f => f.Kunde).WithMany().HasForeignKey(f => f.KundeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.Bilag).WithMany().HasForeignKey(f => f.BilagId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.KundeFaktura).WithMany().HasForeignKey(f => f.KundeFakturaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(f => f.KreditertFaktura)
            .WithMany(f => f.Kreditnotaer)
            .HasForeignKey(f => f.KreditertFakturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(f => f.BelopEksMva).HasConversion(
            v => v.Verdi, v => new Belop(v));
        builder.Property(f => f.MvaBelop).HasConversion(
            v => v.Verdi, v => new Belop(v));
        builder.Property(f => f.BelopInklMva).HasConversion(
            v => v.Verdi, v => new Belop(v));

        builder.Ignore(f => f.FakturaId);
    }
}

public class FakturaLinjeConfiguration : IEntityTypeConfiguration<FakturaLinje>
{
    public void Configure(EntityTypeBuilder<FakturaLinje> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasIndex(l => new { l.FakturaId, l.Linjenummer }).IsUnique();

        builder.HasOne(l => l.Faktura).WithMany(f => f.Linjer)
            .HasForeignKey(l => l.FakturaId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(l => l.Enhetspris).HasConversion(
            v => v.Verdi, v => new Belop(v));
        builder.Property(l => l.Nettobelop).HasConversion(
            v => v.Verdi, v => new Belop(v));
        builder.Property(l => l.MvaBelop).HasConversion(
            v => v.Verdi, v => new Belop(v));
        builder.Property(l => l.Bruttobelop).HasConversion(
            v => v.Verdi, v => new Belop(v));
        builder.Property(l => l.RabattBelop).HasConversion(
            v => v.HasValue ? v.Value.Verdi : (decimal?)null,
            v => v.HasValue ? new Belop(v.Value) : null);
    }
}

public class FakturaMvaLinjeConfiguration : IEntityTypeConfiguration<FakturaMvaLinje>
{
    public void Configure(EntityTypeBuilder<FakturaMvaLinje> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasIndex(m => new { m.FakturaId, m.MvaKode }).IsUnique();

        builder.HasOne(m => m.Faktura).WithMany(f => f.MvaLinjer)
            .HasForeignKey(m => m.FakturaId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(m => m.Grunnlag).HasConversion(
            v => v.Verdi, v => new Belop(v));
        builder.Property(m => m.MvaBelop).HasConversion(
            v => v.Verdi, v => new Belop(v));
    }
}

public class FakturaNummerserieConfiguration : IEntityTypeConfiguration<FakturaNummerserie>
{
    public void Configure(EntityTypeBuilder<FakturaNummerserie> builder)
    {
        builder.HasKey(n => n.Id);
        builder.HasIndex(n => new { n.Ar, n.Dokumenttype }).IsUnique();
    }
}

public class SelskapsinfoConfiguration : IEntityTypeConfiguration<Selskapsinfo>
{
    public void Configure(EntityTypeBuilder<Selskapsinfo> builder)
    {
        builder.HasKey(s => s.Id);
    }
}
