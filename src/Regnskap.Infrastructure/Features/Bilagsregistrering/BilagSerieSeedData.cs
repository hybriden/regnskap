using Microsoft.EntityFrameworkCore;
using Regnskap.Domain.Features.Bilagsregistrering;
using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Infrastructure.Persistence;
using BilagType = Regnskap.Domain.Features.Hovedbok.BilagType;

namespace Regnskap.Infrastructure.Features.Bilagsregistrering;

/// <summary>
/// Seed-data for standard bilagserier.
/// </summary>
public static class BilagSerieSeedData
{
    public static readonly BilagSerie[] StandardSerier =
    {
        new()
        {
            Id = Guid.Parse("a0000001-0000-0000-0000-000000000001"),
            Kode = "IB",
            Navn = "Apningsbalanse",
            NavnEn = "Opening Balance",
            StandardType = BilagType.Apningsbalanse,
            ErAktiv = true,
            ErSystemserie = true,
            SaftJournalId = "IB"
        },
        new()
        {
            Id = Guid.Parse("a0000001-0000-0000-0000-000000000002"),
            Kode = "MAN",
            Navn = "Manuelt bilag",
            NavnEn = "Manual Journal Entry",
            StandardType = BilagType.Manuelt,
            ErAktiv = true,
            ErSystemserie = true,
            SaftJournalId = "MAN"
        },
        new()
        {
            Id = Guid.Parse("a0000001-0000-0000-0000-000000000003"),
            Kode = "AUTO",
            Navn = "Automatisk bilag",
            NavnEn = "Automatic Entry",
            StandardType = BilagType.Manuelt,
            ErAktiv = true,
            ErSystemserie = true,
            SaftJournalId = "AUTO"
        },
        new()
        {
            Id = Guid.Parse("a0000001-0000-0000-0000-000000000004"),
            Kode = "IF",
            Navn = "Inngaende faktura",
            NavnEn = "Accounts Payable Invoice",
            StandardType = BilagType.InngaendeFaktura,
            ErAktiv = true,
            ErSystemserie = true,
            SaftJournalId = "IF"
        },
        new()
        {
            Id = Guid.Parse("a0000001-0000-0000-0000-000000000005"),
            Kode = "UF",
            Navn = "Utgaende faktura",
            NavnEn = "Accounts Receivable Invoice",
            StandardType = BilagType.UtgaendeFaktura,
            ErAktiv = true,
            ErSystemserie = true,
            SaftJournalId = "UF"
        },
        new()
        {
            Id = Guid.Parse("a0000001-0000-0000-0000-000000000006"),
            Kode = "BANK",
            Navn = "Bankbilag",
            NavnEn = "Bank Entry",
            StandardType = BilagType.Bank,
            ErAktiv = true,
            ErSystemserie = true,
            SaftJournalId = "BANK"
        },
        new()
        {
            Id = Guid.Parse("a0000001-0000-0000-0000-000000000007"),
            Kode = "LON",
            Navn = "Lonsbilag",
            NavnEn = "Payroll Entry",
            StandardType = BilagType.Lonn,
            ErAktiv = true,
            ErSystemserie = true,
            SaftJournalId = "LON"
        },
        new()
        {
            Id = Guid.Parse("a0000001-0000-0000-0000-000000000008"),
            Kode = "KOR",
            Navn = "Korreksjon",
            NavnEn = "Correction/Reversal",
            StandardType = BilagType.Korreksjon,
            ErAktiv = true,
            ErSystemserie = true,
            SaftJournalId = "KOR"
        }
    };

    /// <summary>
    /// Seed default bilagserier if they do not exist.
    /// Call this from application startup.
    /// </summary>
    public static async Task SeedAsync(RegnskapDbContext db, CancellationToken ct = default)
    {
        foreach (var serie in StandardSerier)
        {
            if (!await db.BilagSerier.AnyAsync(s => s.Kode == serie.Kode, ct))
            {
                await db.BilagSerier.AddAsync(serie, ct);
            }
        }
        await db.SaveChangesAsync(ct);
    }
}
