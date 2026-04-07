namespace Regnskap.Application.Features.Bank;

using Regnskap.Domain.Features.Bankavstemming;

/// <summary>
/// Service for bankavstemming.
/// </summary>
public interface IBankavstemmingService
{
    /// <summary>
    /// Hent eller opprett avstemming for en bankkonto og periode.
    /// </summary>
    Task<Bankavstemming> HentEllerOpprett(Guid bankkontoId, int aar, int periode);

    /// <summary>
    /// Oppdater tidsavgrensninger og forklaringer.
    /// </summary>
    Task<Bankavstemming> Oppdater(Guid avstemmingId, OppdaterAvstemmingRequest request);

    /// <summary>
    /// Godkjenn avstemming (validerer at differanse er forklart).
    /// </summary>
    Task<Bankavstemming> Godkjenn(Guid avstemmingId, string godkjentAv);

    /// <summary>
    /// Generer avstemmingsrapport.
    /// </summary>
    Task<AvstemmingsrapportResponse> GenererRapport(Guid bankkontoId, DateOnly dato);
}

public record OppdaterAvstemmingRequest(
    decimal UtestaaendeBetalinger,
    decimal InnbetalingerITransitt,
    decimal AndreDifferanser,
    string? DifferanseForklaring
);

public record AvstemmingsrapportResponse(
    string Bankkontonummer,
    string Banknavn,
    string Hovedbokkontonummer,
    DateOnly Avstemmingsdato,
    decimal SaldoHovedbok,
    decimal SaldoBank,
    decimal Differanse,
    List<AvstemmingspostResponse> UtestaaendeBetalinger,
    List<AvstemmingspostResponse> InnbetalingerITransitt,
    List<AvstemmingspostResponse> AndrePoster,
    decimal SumTidsavgrensninger,
    decimal UforklartDifferanse,
    AvstemmingStatus Status
);

public record AvstemmingspostResponse(
    DateOnly Dato,
    string Beskrivelse,
    decimal Belop,
    string? Referanse
);
