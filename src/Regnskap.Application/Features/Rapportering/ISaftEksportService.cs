namespace Regnskap.Application.Features.Rapportering;

public interface ISaftEksportService
{
    /// <summary>
    /// Genererer komplett SAF-T Financial XML v1.30.
    /// </summary>
    Task<Stream> GenererSaftXmlAsync(
        int ar, int fraPeriode = 1, int tilPeriode = 12,
        string taxAccountingBasis = "A",
        CancellationToken ct = default);

    /// <summary>
    /// Validerer generert SAF-T mot XSD-skjema.
    /// Returnerer liste med valideringsfeil (tom = gyldig).
    /// </summary>
    Task<List<string>> ValiderSaftXmlAsync(
        Stream xmlStream,
        CancellationToken ct = default);
}
