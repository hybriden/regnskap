namespace Regnskap.Application.Features.Mva;

public class MvaTerminIkkeFunnetException : Exception
{
    public MvaTerminIkkeFunnetException(Guid terminId)
        : base($"MVA-termin {terminId} finnes ikke.") { }
}

public class MvaTerminerFinnesException : Exception
{
    public MvaTerminerFinnesException(int ar)
        : base($"Terminer for {ar} finnes allerede.") { }
}

public class MvaTerminIkkeApenException : Exception
{
    public MvaTerminIkkeApenException(Guid terminId)
        : base($"Termin {terminId} er ikke apen for beregning.") { }
}

public class MvaOppgjorAlleredeLastException : Exception
{
    public MvaOppgjorAlleredeLastException(Guid terminId)
        : base($"Oppgjoret for termin {terminId} er last og kan ikke endres.") { }
}

public class MvaOppgjorManglerException : Exception
{
    public MvaOppgjorManglerException(Guid terminId)
        : base($"Oppgjor for termin {terminId} er ikke beregnet.") { }
}

public class MvaAvstemmingIkkeFunnetException : Exception
{
    public MvaAvstemmingIkkeFunnetException(Guid terminId)
        : base($"Ingen avstemming funnet for termin {terminId}.") { }
}

public class MvaAvstemmingIkkeGodkjentException : Exception
{
    public MvaAvstemmingIkkeGodkjentException(Guid terminId)
        : base($"Avstemming for termin {terminId} er ikke godkjent.") { }
}

public class MvaOppgjorAlleredeBokfortException : Exception
{
    public MvaOppgjorAlleredeBokfortException(Guid terminId)
        : base($"Oppgjorsbilag for termin {terminId} er allerede bokfort.") { }
}
