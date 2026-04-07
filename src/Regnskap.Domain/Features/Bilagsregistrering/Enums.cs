namespace Regnskap.Domain.Features.Bilagsregistrering;

/// <summary>
/// Status for et bilag i registreringsflyten.
/// </summary>
public enum BilagStatus
{
    /// <summary>Bilaget er under arbeid, ikke validert.</summary>
    Kladd,

    /// <summary>Bilaget er validert og klart for bokforing.</summary>
    Validert,

    /// <summary>Bilaget er bokfort mot hovedbok.</summary>
    Bokfort,

    /// <summary>Bilaget er tilbakfort (reversert).</summary>
    Tilbakfort
}
