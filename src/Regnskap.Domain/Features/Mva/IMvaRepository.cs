namespace Regnskap.Domain.Features.Mva;

using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Domain.Features.Kontoplan;

public interface IMvaRepository
{
    // --- Terminer ---
    Task<List<MvaTermin>> HentTerminerForArAsync(int ar, CancellationToken ct = default);
    Task<MvaTermin?> HentTerminAsync(Guid id, CancellationToken ct = default);
    Task<MvaTermin?> HentTerminForDatoAsync(DateOnly dato, CancellationToken ct = default);
    Task<MvaTermin?> HentTerminAsync(int ar, int termin, CancellationToken ct = default);
    Task<bool> TerminerFinnesForArAsync(int ar, CancellationToken ct = default);
    Task LeggTilTerminAsync(MvaTermin termin, CancellationToken ct = default);
    Task LeggTilTerminerAsync(IEnumerable<MvaTermin> terminer, CancellationToken ct = default);

    // --- Oppgjor ---
    Task<MvaOppgjor?> HentOppgjorForTerminAsync(Guid terminId, CancellationToken ct = default);
    Task<MvaOppgjor?> HentOppgjorMedLinjerAsync(Guid oppgjorId, CancellationToken ct = default);
    Task LeggTilOppgjorAsync(MvaOppgjor oppgjor, CancellationToken ct = default);

    // --- Avstemming ---
    Task<MvaAvstemming?> HentSisteAvstemmingForTerminAsync(Guid terminId, CancellationToken ct = default);
    Task<List<MvaAvstemming>> HentAvstemmingshistorikkAsync(Guid terminId, CancellationToken ct = default);
    Task LeggTilAvstemmingAsync(MvaAvstemming avstemming, CancellationToken ct = default);

    // --- Posteringsaggregering (leser fra Hovedbok) ---
    /// <summary>
    /// Henter aggregerte MVA-data fra posteringer i en gitt periode, gruppert per MvaKode.
    /// Kun bokforte posteringer med MvaKode != null inkluderes.
    /// </summary>
    Task<List<MvaAggregeringDto>> HentMvaAggregertForPeriodeAsync(
        DateOnly fraDato, DateOnly tilDato, CancellationToken ct = default);

    /// <summary>
    /// Henter alle MVA-posteringer (detaljert) for en periode, gruppert per MvaKode.
    /// </summary>
    Task<List<MvaPosteringDetalj>> HentMvaPosteringerForPeriodeAsync(
        DateOnly fraDato, DateOnly tilDato, string? mvaKode = null, CancellationToken ct = default);

    /// <summary>
    /// Henter saldo for MVA-relaterte kontoer (2600-2699 serien) for gitte perioder.
    /// </summary>
    Task<List<MvaKontoSaldoDto>> HentMvaKontoSaldoerAsync(
        int ar, int fraPeriode, int tilPeriode, CancellationToken ct = default);

    /// <summary>
    /// Henter sum av auto-genererte MVA-posteringer gruppert per kontonummer for en periode.
    /// Brukes til avstemming: sammenligner med KontoSaldo for MVA-kontoer.
    /// </summary>
    Task<List<MvaKontoBeregnetDto>> HentBeregnetMvaPerKontoAsync(
        DateOnly fraDato, DateOnly tilDato, CancellationToken ct = default);

    Task LagreEndringerAsync(CancellationToken ct = default);
}

/// <summary>
/// Aggregert MVA per kode for en periode. Produseres av repository-query.
/// </summary>
public record MvaAggregeringDto(
    string MvaKode,
    string StandardTaxCode,
    decimal Sats,
    MvaRetning Retning,
    decimal SumGrunnlag,
    decimal SumMvaBelop,
    int AntallPosteringer
);

/// <summary>
/// Detaljert MVA-postering for sammenstilling.
/// </summary>
public record MvaPosteringDetalj(
    Guid PosteringId,
    Guid BilagId,
    int Bilagsnummer,
    DateOnly Bilagsdato,
    string Kontonummer,
    string Beskrivelse,
    BokforingSide Side,
    decimal Belop,
    string MvaKode,
    decimal MvaGrunnlag,
    decimal MvaBelop,
    decimal MvaSats,
    bool ErAutoGenerertMva
);

/// <summary>
/// Saldo for en MVA-konto, fra KontoSaldo.
/// </summary>
public record MvaKontoSaldoDto(
    string Kontonummer,
    string Kontonavn,
    decimal InngaendeBalanse,
    decimal DebetIPerioden,
    decimal KreditIPerioden,
    decimal UtgaendeBalanse
);

/// <summary>
/// Beregnet MVA-belop per kontonummer fra auto-genererte posteringer.
/// Brukes til avstemming mot KontoSaldo.
/// </summary>
public record MvaKontoBeregnetDto(
    string Kontonummer,
    decimal SumDebet,
    decimal SumKredit,
    decimal NettoEndring
);
