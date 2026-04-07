namespace Regnskap.Application.Features.Bank;

/// <summary>
/// Service for import og parsing av CAMT.053.
/// </summary>
public interface ICamt053ImportService
{
    /// <summary>
    /// Importer en CAMT.053-fil for en bankkonto.
    /// Returnerer importresultat med antall bevegelser og auto-matchinger.
    /// </summary>
    Task<ImportKontoutskriftResultat> Importer(Guid bankkontoId, Stream fil, string filnavn);
}

public record ImportKontoutskriftResultat(
    Guid KontoutskriftId,
    string MeldingsId,
    DateOnly PeriodeFra,
    DateOnly PeriodeTil,
    decimal InngaendeSaldo,
    decimal UtgaendeSaldo,
    int AntallBevegelser,
    int AntallAutoMatchet,
    int AntallIkkeMatchet
);
