namespace Regnskap.Domain.Features.Leverandorreskontro;

public class LeverandorIkkeFunnetException : Exception
{
    public LeverandorIkkeFunnetException(Guid id)
        : base($"Leverandor med id {id} ble ikke funnet.") { }

    public LeverandorIkkeFunnetException(string leverandornummer)
        : base($"Leverandor med nummer '{leverandornummer}' ble ikke funnet.") { }
}

public class LeverandorDuplikatException : Exception
{
    public LeverandorDuplikatException(string felt, string verdi)
        : base($"Det finnes allerede en leverandor med {felt} '{verdi}'.") { }
}

public class LeverandorFakturaIkkeFunnetException : Exception
{
    public LeverandorFakturaIkkeFunnetException(Guid id)
        : base($"Leverandorfaktura med id {id} ble ikke funnet.") { }
}

public class LeverandorFakturaDuplikatException : Exception
{
    public LeverandorFakturaDuplikatException(Guid leverandorId, string eksternNummer)
        : base($"Faktura med eksternt nummer '{eksternNummer}' finnes allerede for denne leverandoren.") { }
}

public class LeverandorSperretException : Exception
{
    public LeverandorSperretException(string leverandornummer)
        : base($"Leverandor '{leverandornummer}' er sperret.") { }
}

public class FakturaStatusException : Exception
{
    public FakturaStatusException(string melding) : base(melding) { }
}

public class BetalingsforslagStatusException : Exception
{
    public BetalingsforslagStatusException(string melding) : base(melding) { }
}

public class BetalingsforslagIkkeFunnetException : Exception
{
    public BetalingsforslagIkkeFunnetException(Guid id)
        : base($"Betalingsforslag med id {id} ble ikke funnet.") { }
}
