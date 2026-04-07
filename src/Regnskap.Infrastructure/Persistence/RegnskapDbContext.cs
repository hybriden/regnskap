using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Regnskap.Domain.Common;
using Regnskap.Domain.Features.Kontoplan;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Mva;
using Regnskap.Domain.Features.Kundereskontro;
using Regnskap.Domain.Features.Leverandorreskontro;
using Regnskap.Domain.Features.Bankavstemming;
using Regnskap.Domain.Features.Fakturering;

namespace Regnskap.Infrastructure.Persistence;

public class RegnskapDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public RegnskapDbContext(DbContextOptions<RegnskapDbContext> options) : base(options) { }

    public RegnskapDbContext(DbContextOptions<RegnskapDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Kontoplan
    public DbSet<Kontogruppe> Kontogrupper => Set<Kontogruppe>();
    public DbSet<Konto> Kontoer => Set<Konto>();
    public DbSet<MvaKode> MvaKoder => Set<MvaKode>();

    // Hovedbok
    public DbSet<Regnskapsperiode> Regnskapsperioder => Set<Regnskapsperiode>();
    public DbSet<Bilag> Bilag => Set<Bilag>();
    public DbSet<Postering> Posteringer => Set<Postering>();
    public DbSet<KontoSaldo> KontoSaldoer => Set<KontoSaldo>();

    // Bilagsregistrering
    public DbSet<BilagSerie> BilagSerier => Set<BilagSerie>();
    public DbSet<BilagSerieNummer> BilagSerieNummer => Set<BilagSerieNummer>();
    public DbSet<Vedlegg> Vedlegg => Set<Vedlegg>();

    // MVA
    public DbSet<MvaTermin> MvaTerminer => Set<MvaTermin>();
    public DbSet<MvaOppgjor> MvaOppgjorSet => Set<MvaOppgjor>();
    public DbSet<MvaOppgjorLinje> MvaOppgjorLinjer => Set<MvaOppgjorLinje>();
    public DbSet<MvaAvstemming> MvaAvstemminger => Set<MvaAvstemming>();
    public DbSet<MvaAvstemmingLinje> MvaAvstemmingLinjer => Set<MvaAvstemmingLinje>();

    // Leverandorreskontro
    public DbSet<Leverandor> Leverandorer => Set<Leverandor>();
    public DbSet<LeverandorFaktura> LeverandorFakturaer => Set<LeverandorFaktura>();
    public DbSet<LeverandorFakturaLinje> LeverandorFakturaLinjer => Set<LeverandorFakturaLinje>();
    public DbSet<LeverandorBetaling> LeverandorBetalinger => Set<LeverandorBetaling>();
    public DbSet<Betalingsforslag> Betalingsforslag => Set<Betalingsforslag>();
    public DbSet<BetalingsforslagLinje> BetalingsforslagLinjer => Set<BetalingsforslagLinje>();

    // Bankavstemming
    public DbSet<Bankkonto> Bankkontoer => Set<Bankkonto>();
    public DbSet<Kontoutskrift> Kontoutskrifter => Set<Kontoutskrift>();
    public DbSet<Bankbevegelse> Bankbevegelser => Set<Bankbevegelse>();
    public DbSet<BankbevegelseMatch> BankbevegelseMatchinger => Set<BankbevegelseMatch>();
    public DbSet<Bankavstemming> Bankavstemminger => Set<Bankavstemming>();

    // Fakturering
    public DbSet<Faktura> Fakturaer => Set<Faktura>();
    public DbSet<FakturaLinje> FakturaLinjer => Set<FakturaLinje>();
    public DbSet<FakturaMvaLinje> FakturaMvaLinjer => Set<FakturaMvaLinje>();
    public DbSet<FakturaNummerserie> FakturaNummerserie => Set<FakturaNummerserie>();
    public DbSet<Selskapsinfo> Selskapsinfo => Set<Selskapsinfo>();

    // Kundereskontro
    public DbSet<Kunde> Kunder => Set<Kunde>();
    public DbSet<KundeFaktura> KundeFakturaer => Set<KundeFaktura>();
    public DbSet<KundeFakturaLinje> KundeFakturaLinjer => Set<KundeFakturaLinje>();
    public DbSet<KundeInnbetaling> KundeInnbetalinger => Set<KundeInnbetaling>();
    public DbSet<Purring> Purringer => Set<Purring>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RegnskapDbContext).Assembly);

        // Global query filter for soft delete
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));
                var condition = Expression.Equal(property, Expression.Constant(false));
                var lambda = Expression.Lambda(condition, parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override int SaveChanges()
    {
        SetAuditFields();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditFields()
    {
        var now = DateTime.UtcNow;
        var user = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "system";

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = user;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = user;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.ModifiedAt = now;
                    entry.Entity.ModifiedBy = user;
                    break;
            }
        }
    }
}
