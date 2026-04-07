using Regnskap.Domain.Features.Mva;

namespace Regnskap.Infrastructure.Features.Mva;

/// <summary>
/// Seed-data for MVA-terminer. Brukes for a generere standard tomandersperioder.
/// Selve genereringen skjer via MvaTerminService.GenererTerminerAsync().
/// Denne klassen inneholder hjelpemetoder for testing og seeding.
/// </summary>
public static class MvaTerminSeedData
{
    /// <summary>
    /// Genererer standard 6 tomandersperioder for et ar.
    /// </summary>
    public static List<MvaTermin> GenererTomaanedersTerminer(int ar)
    {
        var erSkuddar = DateTime.IsLeapYear(ar);
        var febSiste = erSkuddar ? 29 : 28;

        return new List<MvaTermin>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Ar = ar, Termin = 1,
                Type = MvaTerminType.Tomaaneders,
                FraDato = new DateOnly(ar, 1, 1),
                TilDato = new DateOnly(ar, 2, febSiste),
                Frist = new DateOnly(ar, 4, 10),
                Status = MvaTerminStatus.Apen
            },
            new()
            {
                Id = Guid.NewGuid(),
                Ar = ar, Termin = 2,
                Type = MvaTerminType.Tomaaneders,
                FraDato = new DateOnly(ar, 3, 1),
                TilDato = new DateOnly(ar, 4, 30),
                Frist = new DateOnly(ar, 6, 10),
                Status = MvaTerminStatus.Apen
            },
            new()
            {
                Id = Guid.NewGuid(),
                Ar = ar, Termin = 3,
                Type = MvaTerminType.Tomaaneders,
                FraDato = new DateOnly(ar, 5, 1),
                TilDato = new DateOnly(ar, 6, 30),
                Frist = new DateOnly(ar, 8, 31),
                Status = MvaTerminStatus.Apen
            },
            new()
            {
                Id = Guid.NewGuid(),
                Ar = ar, Termin = 4,
                Type = MvaTerminType.Tomaaneders,
                FraDato = new DateOnly(ar, 7, 1),
                TilDato = new DateOnly(ar, 8, 31),
                Frist = new DateOnly(ar, 10, 10),
                Status = MvaTerminStatus.Apen
            },
            new()
            {
                Id = Guid.NewGuid(),
                Ar = ar, Termin = 5,
                Type = MvaTerminType.Tomaaneders,
                FraDato = new DateOnly(ar, 9, 1),
                TilDato = new DateOnly(ar, 10, 31),
                Frist = new DateOnly(ar, 12, 10),
                Status = MvaTerminStatus.Apen
            },
            new()
            {
                Id = Guid.NewGuid(),
                Ar = ar, Termin = 6,
                Type = MvaTerminType.Tomaaneders,
                FraDato = new DateOnly(ar, 11, 1),
                TilDato = new DateOnly(ar, 12, 31),
                Frist = new DateOnly(ar + 1, 2, 10),
                Status = MvaTerminStatus.Apen
            }
        };
    }

    /// <summary>
    /// Genererer arstermin for et ar.
    /// </summary>
    public static List<MvaTermin> GenererArstermin(int ar)
    {
        return new List<MvaTermin>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Ar = ar, Termin = 1,
                Type = MvaTerminType.Arlig,
                FraDato = new DateOnly(ar, 1, 1),
                TilDato = new DateOnly(ar, 12, 31),
                Frist = new DateOnly(ar + 1, 3, 10),
                Status = MvaTerminStatus.Apen
            }
        };
    }
}
