using Regnskap.Domain.Features.Hovedbok;

namespace Regnskap.Infrastructure.Features.Hovedbok;

/// <summary>
/// Seed-data for regnskapsperioder. Oppretter perioder for innevaerende ar.
/// </summary>
public static class PeriodeSeedData
{
    public static List<Regnskapsperiode> GenererPerioderForAr(int ar)
    {
        var perioder = new List<Regnskapsperiode>();

        // Periode 0: Apningsbalanse
        perioder.Add(new Regnskapsperiode
        {
            Id = Guid.NewGuid(),
            Ar = ar,
            Periode = 0,
            FraDato = new DateOnly(ar, 1, 1),
            TilDato = new DateOnly(ar, 1, 1),
            Status = PeriodeStatus.Apen
        });

        // Periode 1-12: Maneder
        for (int m = 1; m <= 12; m++)
        {
            var fraDato = new DateOnly(ar, m, 1);
            var tilDato = fraDato.AddMonths(1).AddDays(-1);
            perioder.Add(new Regnskapsperiode
            {
                Id = Guid.NewGuid(),
                Ar = ar,
                Periode = m,
                FraDato = fraDato,
                TilDato = tilDato,
                Status = PeriodeStatus.Apen
            });
        }

        // Periode 13: Arsavslutning
        perioder.Add(new Regnskapsperiode
        {
            Id = Guid.NewGuid(),
            Ar = ar,
            Periode = 13,
            FraDato = new DateOnly(ar, 12, 31),
            TilDato = new DateOnly(ar, 12, 31),
            Status = PeriodeStatus.Apen
        });

        return perioder;
    }
}
