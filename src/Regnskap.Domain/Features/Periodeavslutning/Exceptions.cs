namespace Regnskap.Domain.Features.Periodeavslutning;

public class ArsavslutningException : Exception
{
    public int Ar { get; }
    public ArsavslutningException(int ar, string melding)
        : base($"Arsavslutning {ar}: {melding}") => Ar = ar;
}

public class AvskrivningException : Exception
{
    public AvskrivningException(string melding) : base(melding) { }
}

public class PeriodiseringsException : Exception
{
    public PeriodiseringsException(string melding) : base(melding) { }
}

public class AnleggsmiddelIkkeFunnetException : Exception
{
    public Guid Id { get; }
    public AnleggsmiddelIkkeFunnetException(Guid id)
        : base($"Anleggsmiddel med id {id} finnes ikke.") => Id = id;
}

public class PeriodiseringIkkeFunnetException : Exception
{
    public Guid Id { get; }
    public PeriodiseringIkkeFunnetException(Guid id)
        : base($"Periodisering med id {id} finnes ikke.") => Id = id;
}

public class DuplikatAvskrivningException : Exception
{
    public DuplikatAvskrivningException(Guid anleggsmiddelId, int ar, int periode)
        : base($"Avskrivning for anleggsmiddel {anleggsmiddelId} er allerede bokfort for {ar}-{periode:D2}.") { }
}

public class DuplikatPeriodiseringException : Exception
{
    public DuplikatPeriodiseringException(Guid periodiseringId, int ar, int periode)
        : base($"Periodisering {periodiseringId} er allerede bokfort for {ar}-{periode:D2}.") { }
}
