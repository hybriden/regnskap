namespace Regnskap.Domain.Features.Hovedbok;

public class BilagValideringException : Exception
{
    public string BilagsId { get; }
    public BilagValideringException(string bilagsId, string melding)
        : base($"Bilag {bilagsId}: {melding}") => BilagsId = bilagsId;
}

public class PeriodeIkkeFunnetException : Exception
{
    public int Ar { get; }
    public int Periode { get; }
    public PeriodeIkkeFunnetException(int ar, int periode)
        : base($"Regnskapsperiode {ar}-{periode:D2} finnes ikke.")
    {
        Ar = ar;
        Periode = periode;
    }
}

public class PeriodeSperretException : Exception
{
    public int Ar { get; }
    public int Periode { get; }
    public PeriodeSperretException(int ar, int periode)
        : base($"Regnskapsperiode {ar}-{periode:D2} er sperret.")
    {
        Ar = ar;
        Periode = periode;
    }
}

public class BilagNummereringException : Exception
{
    public BilagNummereringException(string melding) : base(melding) { }
}

public class PeriodeLukkingException : Exception
{
    public int Ar { get; }
    public int Periode { get; }
    public PeriodeLukkingException(int ar, int periode, string grunn)
        : base($"Kan ikke lukke periode {ar}-{periode:D2}: {grunn}")
    {
        Ar = ar;
        Periode = periode;
    }
}

public class KontoIkkeBokforbarException : Exception
{
    public string Kontonummer { get; }
    public KontoIkkeBokforbarException(string kontonummer)
        : base($"Konto {kontonummer} er ikke bokforbar (summekonto/overskrift).")
        => Kontonummer = kontonummer;
}

public class AvdelingPakrevdException : Exception
{
    public string Kontonummer { get; }
    public AvdelingPakrevdException(string kontonummer)
        : base($"Konto {kontonummer} krever at avdelingskode angis.")
        => Kontonummer = kontonummer;
}

public class ProsjektPakrevdException : Exception
{
    public string Kontonummer { get; }
    public ProsjektPakrevdException(string kontonummer)
        : base($"Konto {kontonummer} krever at prosjektkode angis.")
        => Kontonummer = kontonummer;
}

public class UgyldigStatusOvergangException : Exception
{
    public PeriodeStatus FraStatus { get; }
    public PeriodeStatus TilStatus { get; }
    public UgyldigStatusOvergangException(PeriodeStatus fra, PeriodeStatus til)
        : base($"Ugyldig statusovergang fra {fra} til {til}.")
    {
        FraStatus = fra;
        TilStatus = til;
    }
}

public class PerioderFinnesAlleredeException : Exception
{
    public int Ar { get; }
    public PerioderFinnesAlleredeException(int ar)
        : base($"Perioder for regnskapsar {ar} finnes allerede.") => Ar = ar;
}
