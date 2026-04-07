using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Regnskap.Domain.Features.Rapportering;

namespace Regnskap.Infrastructure.Features.Rapportering;

public class RapportKonfigurasjonConfiguration : IEntityTypeConfiguration<RapportKonfigurasjon>
{
    public void Configure(EntityTypeBuilder<RapportKonfigurasjon> builder)
    {
        builder.ToTable("RapportKonfigurasjon");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Firmanavn).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Organisasjonsnummer).IsRequired().HasMaxLength(9);
        builder.Property(e => e.Adresse).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Postnummer).IsRequired().HasMaxLength(4);
        builder.Property(e => e.Poststed).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Landskode).IsRequired().HasMaxLength(2).HasDefaultValue("NO");
        builder.Property(e => e.Valuta).IsRequired().HasMaxLength(3).HasDefaultValue("NOK");
        builder.Property(e => e.Kontaktperson).HasMaxLength(200);
        builder.Property(e => e.Telefon).HasMaxLength(20);
        builder.Property(e => e.Epost).HasMaxLength(200);
        builder.Property(e => e.Bankkontonummer).HasMaxLength(20);
        builder.Property(e => e.Iban).HasMaxLength(34);
    }
}

public class BudsjettConfiguration : IEntityTypeConfiguration<Budsjett>
{
    public void Configure(EntityTypeBuilder<Budsjett> builder)
    {
        builder.ToTable("Budsjett");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Kontonummer).IsRequired().HasMaxLength(6);
        builder.Property(e => e.Belop).HasPrecision(18, 2);
        builder.Property(e => e.Versjon).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Merknad).HasMaxLength(500);
        builder.HasIndex(e => new { e.Kontonummer, e.Ar, e.Periode, e.Versjon }).IsUnique();
    }
}

public class RapportLoggConfiguration : IEntityTypeConfiguration<RapportLogg>
{
    public void Configure(EntityTypeBuilder<RapportLogg> builder)
    {
        builder.ToTable("RapportLogg");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.GenererAv).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Kontrollsum).HasMaxLength(64);
        builder.Property(e => e.Parametre).HasMaxLength(2000);
        builder.HasIndex(e => new { e.Type, e.Ar });
    }
}
