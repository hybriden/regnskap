namespace Regnskap.Domain.Common;

public class AccountingBalanceException : Exception
{
    public decimal SumDebet { get; }
    public decimal SumKredit { get; }

    public AccountingBalanceException(decimal sumDebet, decimal sumKredit)
        : base($"Bilag er ikke i balanse. Debet: {sumDebet:N2}, Kredit: {sumKredit:N2}, Differanse: {sumDebet - sumKredit:N2}")
    {
        SumDebet = sumDebet;
        SumKredit = sumKredit;
    }
}

public class PeriodeLukketException : Exception
{
    public PeriodeLukketException(int ar, int maned)
        : base($"Perioden {ar}-{maned:D2} er lukket for bokføring.") { }
}

public class BilagNummerException : Exception
{
    public BilagNummerException(string melding) : base(melding) { }
}

/// <summary>
/// Kastes av infrastruktur-laget nar en concurrency-konflikt oppstar (wrapper rundt DbUpdateConcurrencyException).
/// Muliggjor retry-logikk i Application-laget uten EF Core-avhengighet.
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string melding, Exception innerException)
        : base(melding, innerException) { }
}
