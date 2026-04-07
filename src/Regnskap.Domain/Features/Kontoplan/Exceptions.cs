namespace Regnskap.Domain.Features.Kontoplan;

public class KontoIkkeFunnetException : Exception
{
    public string Kontonummer { get; }
    public KontoIkkeFunnetException(string kontonummer)
        : base($"Konto {kontonummer} finnes ikke.") => Kontonummer = kontonummer;
}

public class KontoInaktivException : Exception
{
    public string Kontonummer { get; }
    public KontoInaktivException(string kontonummer)
        : base($"Konto {kontonummer} er deaktivert og kan ikke brukes til bokforing.") => Kontonummer = kontonummer;
}

public class SystemkontoSlettingException : Exception
{
    public string Kontonummer { get; }
    public SystemkontoSlettingException(string kontonummer)
        : base($"Systemkonto {kontonummer} kan ikke slettes.") => Kontonummer = kontonummer;
}

public class KontoHarPosteringerException : Exception
{
    public string Kontonummer { get; }
    public KontoHarPosteringerException(string kontonummer)
        : base($"Konto {kontonummer} har posteringer og kan ikke slettes.") => Kontonummer = kontonummer;
}

public class KontoHarUnderkontoerException : Exception
{
    public string Kontonummer { get; }
    public KontoHarUnderkontoerException(string kontonummer)
        : base($"Konto {kontonummer} har aktive underkontoer og kan ikke slettes.") => Kontonummer = kontonummer;
}

public class SystemkontoFeltEndringException : Exception
{
    public string Kontonummer { get; }
    public string Felt { get; }
    public SystemkontoFeltEndringException(string kontonummer, string felt)
        : base($"Beskyttet felt '{felt}' pa systemkonto {kontonummer} kan ikke endres.")
    {
        Kontonummer = kontonummer;
        Felt = felt;
    }
}

public class MvaKodeIkkeFunnetException : Exception
{
    public string Kode { get; }
    public MvaKodeIkkeFunnetException(string kode)
        : base($"MVA-kode {kode} finnes ikke.") => Kode = kode;
}
