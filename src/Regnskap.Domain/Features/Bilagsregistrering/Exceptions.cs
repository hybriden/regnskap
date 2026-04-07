namespace Regnskap.Domain.Features.Bilagsregistrering;

public class BilagIkkeFunnetException : Exception
{
    public Guid BilagId { get; }
    public BilagIkkeFunnetException(Guid id)
        : base($"Bilag {id} finnes ikke.") => BilagId = id;
}

public class BilagAlleredeBokfortException : Exception
{
    public Guid BilagId { get; }
    public BilagAlleredeBokfortException(Guid id)
        : base($"Bilaget er allerede bokfort.") => BilagId = id;
}

public class BilagAlleredeTilbakfortException : Exception
{
    public Guid BilagId { get; }
    public BilagAlleredeTilbakfortException(Guid id)
        : base($"Bilaget er allerede tilbakfort.") => BilagId = id;
}

public class BilagIkkeBokfortForTilbakeforingException : Exception
{
    public Guid BilagId { get; }
    public BilagIkkeBokfortForTilbakeforingException(Guid id)
        : base($"Kun bokforte bilag kan tilbakefores.") => BilagId = id;
}

public class SerieIkkeFunnetException : Exception
{
    public string SerieKode { get; }
    public SerieIkkeFunnetException(string kode)
        : base($"Bilagserie {kode} finnes ikke.") => SerieKode = kode;
}

public class SerieInaktivException : Exception
{
    public string SerieKode { get; }
    public SerieInaktivException(string kode)
        : base($"Bilagserie {kode} er deaktivert.") => SerieKode = kode;
}

public class VedleggIkkeFunnetException : Exception
{
    public Guid VedleggId { get; }
    public VedleggIkkeFunnetException(Guid id)
        : base($"Vedlegg {id} finnes ikke.") => VedleggId = id;
}

public class VedleggPaBokfortBilagException : Exception
{
    public VedleggPaBokfortBilagException()
        : base("Vedlegg pa bokforte bilag kan ikke slettes (oppbevaringsplikt).") { }
}

public class UgyldigMimeTypeException : Exception
{
    public string MimeType { get; }
    public UgyldigMimeTypeException(string mimeType)
        : base($"MIME-type {mimeType} er ikke tillatt. Tillatte typer: application/pdf, image/jpeg, image/png, image/tiff, application/xml")
        => MimeType = mimeType;
}

public class VedleggForStortException : Exception
{
    public long Storrelse { get; }
    public VedleggForStortException(long storrelse)
        : base($"Vedlegg er for stort ({storrelse} bytes). Maks er {Vedlegg.MaksStorrelse} bytes.")
        => Storrelse = storrelse;
}

public class NummereringKonfliktException : Exception
{
    public NummereringKonfliktException()
        : base("Samtidig nummerering - provs igjen.") { }
}

public class SystemserieKanIkkeDeaktiveresException : Exception
{
    public string SerieKode { get; }
    public SystemserieKanIkkeDeaktiveresException(string kode)
        : base($"Systemserien {kode} kan ikke deaktiveres.") => SerieKode = kode;
}
