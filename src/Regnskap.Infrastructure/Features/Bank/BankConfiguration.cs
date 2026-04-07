using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Features.Bankavstemming;

namespace Regnskap.Infrastructure.Features.Bank;

public class BankkontoConfiguration : IEntityTypeConfiguration<Bankkonto>
{
    public void Configure(EntityTypeBuilder<Bankkonto> builder)
    {
        builder.ToTable("Bankkontoer");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Kontonummer).HasMaxLength(11).IsRequired();
        builder.Property(b => b.Iban).HasMaxLength(34);
        builder.Property(b => b.Bic).HasMaxLength(11);
        builder.Property(b => b.Banknavn).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Beskrivelse).HasMaxLength(500).IsRequired();
        builder.Property(b => b.Valutakode).HasMaxLength(3).IsRequired();
        builder.Property(b => b.Hovedbokkontonummer).HasMaxLength(10).IsRequired();

        builder.HasIndex(b => b.Kontonummer)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasIndex(b => b.Iban)
            .IsUnique()
            .HasFilter("\"Iban\" IS NOT NULL AND \"IsDeleted\" = false");

        builder.HasOne(b => b.Hovedbokkonto)
            .WithMany()
            .HasForeignKey(b => b.HovedbokkkontoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class KontoutskriftConfiguration : IEntityTypeConfiguration<Kontoutskrift>
{
    public void Configure(EntityTypeBuilder<Kontoutskrift> builder)
    {
        builder.ToTable("Kontoutskrifter");
        builder.HasKey(k => k.Id);

        builder.Property(k => k.MeldingsId).HasMaxLength(100).IsRequired();
        builder.Property(k => k.UtskriftId).HasMaxLength(100).IsRequired();
        builder.Property(k => k.Sekvensnummer).HasMaxLength(20);
        builder.Property(k => k.OriginalFilsti).HasMaxLength(500);
        builder.Property(k => k.FilHash).HasMaxLength(64);

        builder.Property(k => k.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(k => k.InngaendeSaldo)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.Property(k => k.UtgaendeSaldo)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.Property(k => k.SumInn)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.Property(k => k.SumUt)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(k => new { k.BankkontoId, k.MeldingsId }).IsUnique();
        builder.HasIndex(k => new { k.BankkontoId, k.PeriodeFra, k.PeriodeTil });

        builder.HasOne(k => k.Bankkonto)
            .WithMany(b => b.Kontoutskrifter)
            .HasForeignKey(k => k.BankkontoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BankbevegelseConfiguration : IEntityTypeConfiguration<Bankbevegelse>
{
    public void Configure(EntityTypeBuilder<Bankbevegelse> builder)
    {
        builder.ToTable("Bankbevegelser");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Valutakode).HasMaxLength(3).IsRequired();
        builder.Property(b => b.KidNummer).HasMaxLength(25);
        builder.Property(b => b.EndToEndId).HasMaxLength(100);
        builder.Property(b => b.Motpart).HasMaxLength(200);
        builder.Property(b => b.MotpartKonto).HasMaxLength(34);
        builder.Property(b => b.Transaksjonskode).HasMaxLength(50);
        builder.Property(b => b.Beskrivelse).HasMaxLength(500);
        builder.Property(b => b.BankReferanse).HasMaxLength(100);

        builder.Property(b => b.Retning)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(b => b.MatcheType)
            .HasConversion<string?>()
            .HasMaxLength(30);

        builder.Property(b => b.MatcheKonfidens)
            .HasColumnType("decimal(3,2)");

        builder.Property(b => b.Belop)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(b => b.KidNummer).HasFilter("\"KidNummer\" IS NOT NULL");
        builder.HasIndex(b => b.Status);
        builder.HasIndex(b => new { b.BankkontoId, b.Bokforingsdato });

        builder.HasOne(b => b.Kontoutskrift)
            .WithMany(k => k.Bevegelser)
            .HasForeignKey(b => b.KontoutskriftId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Bankkonto)
            .WithMany()
            .HasForeignKey(b => b.BankkontoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Bilag)
            .WithMany()
            .HasForeignKey(b => b.BilagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BankbevegelseMatchConfiguration : IEntityTypeConfiguration<BankbevegelseMatch>
{
    public void Configure(EntityTypeBuilder<BankbevegelseMatch> builder)
    {
        builder.ToTable("BankbevegelseMatchinger");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Beskrivelse).HasMaxLength(500);

        builder.Property(m => m.MatcheType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(m => m.Belop)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(m => m.BankbevegelseId);

        builder.HasOne(m => m.Bankbevegelse)
            .WithMany(b => b.Matchinger)
            .HasForeignKey(m => m.BankbevegelseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.KundeFaktura)
            .WithMany()
            .HasForeignKey(m => m.KundeFakturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.LeverandorFaktura)
            .WithMany()
            .HasForeignKey(m => m.LeverandorFakturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Bilag)
            .WithMany()
            .HasForeignKey(m => m.BilagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BankavstemmingConfiguration : IEntityTypeConfiguration<Bankavstemming>
{
    public void Configure(EntityTypeBuilder<Bankavstemming> builder)
    {
        builder.ToTable("Bankavstemminger");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.DifferanseForklaring).HasMaxLength(2000);
        builder.Property(a => a.GodkjentAv).HasMaxLength(200);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.SaldoHovedbok)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.Property(a => a.SaldoBank)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.Property(a => a.Differanse)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.Property(a => a.UtestaaendeBetalinger)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.Property(a => a.InnbetalingerITransitt)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        builder.Property(a => a.AndreDifferanser)
            .HasConversion(v => v.Verdi, v => new Domain.Common.Belop(v))
            .HasColumnType("decimal(18,2)");

        // Ignorer computed properties
        builder.Ignore(a => a.ForklartDifferanse);
        builder.Ignore(a => a.UforklartDifferanse);

        builder.HasIndex(a => new { a.BankkontoId, a.Ar, a.Periode }).IsUnique();

        builder.HasOne(a => a.Bankkonto)
            .WithMany(b => b.Avstemminger)
            .HasForeignKey(a => a.BankkontoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
