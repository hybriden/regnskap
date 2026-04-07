using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Features.Bilagsregistrering;

namespace Regnskap.Infrastructure.Features.Bilagsregistrering;

public class BilagSerieConfiguration : IEntityTypeConfiguration<BilagSerie>
{
    public void Configure(EntityTypeBuilder<BilagSerie> builder)
    {
        builder.ToTable("BilagSerier");
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => b.Kode).IsUnique();
        builder.Property(b => b.Kode).HasMaxLength(10).IsRequired();
        builder.Property(b => b.Navn).HasMaxLength(200).IsRequired();
        builder.Property(b => b.NavnEn).HasMaxLength(200);
        builder.Property(b => b.StandardType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(b => b.SaftJournalId).HasMaxLength(20).IsRequired();
    }
}

public class BilagSerieNummerConfiguration : IEntityTypeConfiguration<BilagSerieNummer>
{
    public void Configure(EntityTypeBuilder<BilagSerieNummer> builder)
    {
        builder.ToTable("BilagSerieNummer");
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.SerieKode, b.Ar }).IsUnique();
        builder.Property(b => b.SerieKode).HasMaxLength(10).IsRequired();
        builder.Property(b => b.RowVersion).IsRowVersion();
        builder.HasOne(b => b.BilagSerie)
            .WithMany()
            .HasForeignKey(b => b.BilagSerieId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
