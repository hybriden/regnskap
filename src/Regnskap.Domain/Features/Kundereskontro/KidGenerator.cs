namespace Regnskap.Domain.Features.Kundereskontro;

/// <summary>
/// KID-nummer generator og validator.
/// Stotter bade MOD10 (Luhn) og MOD11 algoritmer.
/// KID: 2-25 siffer, siste siffer er kontrollsiffer.
/// </summary>
public static class KidGenerator
{
    /// <summary>
    /// Generer KID-nummer fra kundenummer og fakturanummer.
    /// Format: [kundenummer (6 siffer)][fakturanummer (6 siffer)][kontrollsiffer].
    /// </summary>
    public static string Generer(string kundenummer, int fakturanummer, KidAlgoritme algoritme)
    {
        var payload = $"{kundenummer.PadLeft(6, '0')}{fakturanummer.ToString().PadLeft(6, '0')}";
        var kontrollsiffer = algoritme switch
        {
            KidAlgoritme.MOD10 => BeregnMod10(payload),
            KidAlgoritme.MOD11 => BeregnMod11(payload),
            _ => throw new ArgumentOutOfRangeException(nameof(algoritme))
        };

        if (kontrollsiffer < 0)
            throw new InvalidOperationException(
                $"MOD11 gir ugyldig kontrollsiffer for payload '{payload}'. Bruk neste fakturanummer.");

        return payload + kontrollsiffer;
    }

    /// <summary>
    /// Valider et KID-nummer.
    /// </summary>
    public static bool Valider(string kid, KidAlgoritme algoritme)
    {
        if (string.IsNullOrWhiteSpace(kid) || kid.Length < 2 || kid.Length > 25)
            return false;

        if (!kid.All(char.IsDigit))
            return false;

        var payload = kid[..^1];
        var oppgittKontroll = int.Parse(kid[^1..]);

        var beregnetKontroll = algoritme switch
        {
            KidAlgoritme.MOD10 => BeregnMod10(payload),
            KidAlgoritme.MOD11 => BeregnMod11(payload),
            _ => -1
        };

        return beregnetKontroll == oppgittKontroll;
    }

    /// <summary>
    /// MOD10 (Luhn) kontrollsiffer.
    /// </summary>
    public static int BeregnMod10(string payload)
    {
        var sum = 0;
        for (int i = payload.Length - 1, vekt = 2; i >= 0; i--, vekt = vekt == 2 ? 1 : 2)
        {
            var produkt = (payload[i] - '0') * vekt;
            sum += produkt > 9 ? produkt - 9 : produkt;
        }
        return (10 - (sum % 10)) % 10;
    }

    /// <summary>
    /// MOD11 kontrollsiffer. Returnerer -1 hvis rest = 1 (ugyldig, 11-1=10).
    /// </summary>
    public static int BeregnMod11(string payload)
    {
        var vekter = new[] { 2, 3, 4, 5, 6, 7 };
        var sum = 0;
        for (int i = payload.Length - 1, v = 0; i >= 0; i--, v++)
        {
            sum += (payload[i] - '0') * vekter[v % vekter.Length];
        }
        var rest = sum % 11;
        if (rest == 0) return 0;
        if (rest == 1) return -1; // 11 - 1 = 10, ugyldig
        return 11 - rest;
    }
}
