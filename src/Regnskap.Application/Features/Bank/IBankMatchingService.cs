namespace Regnskap.Application.Features.Bank;

using Regnskap.Domain.Features.Bankavstemming;

/// <summary>
/// Service for automatisk og manuell matching av bankbevegelser.
/// </summary>
public interface IBankMatchingService
{
    /// <summary>
    /// Kjoer automatisk matching paa alle umatchede bevegelser for en bankkonto.
    /// </summary>
    Task<int> AutoMatch(Guid bankkontoId);

    /// <summary>
    /// Hent matchforslag for en enkelt bevegelse.
    /// </summary>
    Task<IReadOnlyList<MatcheForslagResponse>> HentForslag(Guid bankbevegelseId);

    /// <summary>
    /// Manuell matching av en bevegelse.
    /// </summary>
    Task Match(Guid bankbevegelseId, ManuellMatchRequest request);

    /// <summary>
    /// Splitt-matching.
    /// </summary>
    Task Splitt(Guid bankbevegelseId, SplittMatchRequest request);

    /// <summary>
    /// Fjern matching og tilbakestill bevegelse til IkkeMatchet.
    /// </summary>
    Task FjernMatch(Guid bankbevegelseId);

    /// <summary>
    /// FR-B05: Direkte bokforing av umatched bankbevegelse.
    /// Oppretter bilag med bank-konto (1920) og motkonto.
    /// </summary>
    Task<Guid> BokforDirekte(Guid bankbevegelseId, BokforDirekteRequest request);
}

public record BokforDirekteRequest(
    Guid MotkontoId,
    string Motkontonummer,
    string? MvaKode,
    string Beskrivelse,
    string? Avdelingskode,
    string? Prosjektkode
);

public record ManuellMatchRequest(
    Guid? KundeFakturaId,
    Guid? LeverandorFakturaId,
    Guid? BilagId,
    string? Beskrivelse
);

public record SplittMatchRequest(
    List<SplittLinjeRequest> Linjer
);

public record SplittLinjeRequest(
    decimal Belop,
    Guid? KundeFakturaId,
    Guid? LeverandorFakturaId,
    Guid? BilagId,
    string? Beskrivelse
);

public record MatcheForslagResponse(
    MatcheType MatcheType,
    decimal Konfidens,
    string Beskrivelse,
    Guid? KundeFakturaId,
    string? KundeFakturaNummer,
    decimal? KundeFakturaGjenstaende,
    Guid? LeverandorFakturaId,
    string? LeverandorFakturaNummer,
    decimal? LeverandorFakturaGjenstaende,
    Guid? BilagId,
    string? BilagNummer
);
