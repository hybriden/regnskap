namespace Regnskap.Domain.Features.Kundereskontro;

public class KundeIkkeFunnetException : Exception
{
    public KundeIkkeFunnetException(Guid id) : base($"Kunde med id {id} ble ikke funnet.") { }
    public KundeIkkeFunnetException(string kundenummer) : base($"Kunde med nummer {kundenummer} ble ikke funnet.") { }
}

public class KundeFakturaIkkeFunnetException : Exception
{
    public KundeFakturaIkkeFunnetException(Guid id) : base($"Kundefaktura med id {id} ble ikke funnet.") { }
}

public class KundenummerEksistererException : Exception
{
    public KundenummerEksistererException(string kundenummer) : base($"Kundenummer {kundenummer} er allerede i bruk.") { }
}

public class KundeSperretException : Exception
{
    public KundeSperretException(string kundenummer) : base($"Kunde {kundenummer} er sperret for nye fakturaer.") { }
}

public class KredittgrenseOverskredetException : Exception
{
    public decimal UtstaaendeSaldo { get; }
    public decimal NyFakturaBelop { get; }
    public decimal Kredittgrense { get; }

    public KredittgrenseOverskredetException(decimal utstaaende, decimal nyFaktura, decimal grense)
        : base($"Kredittgrense overskredet. Utstaaende: {utstaaende:N2}, ny faktura: {nyFaktura:N2}, grense: {grense:N2}.")
    {
        UtstaaendeSaldo = utstaaende;
        NyFakturaBelop = nyFaktura;
        Kredittgrense = grense;
    }
}

public class PurringValideringException : Exception
{
    public PurringValideringException(string melding) : base(melding) { }
}

public class TapAvskrivningException : Exception
{
    public TapAvskrivningException(string melding) : base(melding) { }
}
