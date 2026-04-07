namespace Regnskap.Application.Features.Bilagsregistrering;

using Regnskap.Domain.Features.Hovedbok;
using Regnskap.Application.Features.Hovedbok;

// --- Request DTOer ---

public record OpprettBilagRequest(
    BilagType Type,
    DateOnly Bilagsdato,
    string Beskrivelse,
    string? EksternReferanse,
    string? SerieKode,
    List<OpprettPosteringRequest> Posteringer,
    bool BokforDirekte = true);

public record OpprettPosteringRequest(
    string Kontonummer,
    BokforingSide Side,
    decimal Belop,
    string Beskrivelse,
    string? MvaKode,
    string? Avdelingskode,
    string? Prosjektkode,
    Guid? KundeId,
    Guid? LeverandorId);

public record TilbakeforBilagRequest(
    Guid OriginalBilagId,
    DateOnly Tilbakeforingsdato,
    string Beskrivelse);

public record LeggTilVedleggRequest(
    Guid BilagId,
    string Filnavn,
    string MimeType,
    long Storrelse,
    string LagringSti,
    string HashSha256,
    string? Beskrivelse);

public record BilagSokRequest(
    int? Ar,
    int? Periode,
    BilagType? Type,
    string? SerieKode,
    DateOnly? FraDato,
    DateOnly? TilDato,
    string? Kontonummer,
    decimal? MinBelop,
    decimal? MaxBelop,
    string? Beskrivelse,
    string? EksternReferanse,
    int? Bilagsnummer,
    bool? ErBokfort,
    bool? ErTilbakfort,
    int Side = 1,
    int Antall = 50);

public record ValiderBilagRequest(
    BilagType Type,
    DateOnly Bilagsdato,
    string Beskrivelse,
    string? SerieKode,
    List<OpprettPosteringRequest> Posteringer);

public record OpprettBilagSerieRequest(
    string Kode,
    string Navn,
    string? NavnEn,
    BilagType StandardType,
    string SaftJournalId);

public record OppdaterBilagSerieRequest(
    string Navn,
    string? NavnEn,
    BilagType StandardType,
    bool ErAktiv,
    string SaftJournalId);

// --- Response DTOer ---

public record BilagDto(
    Guid Id,
    string BilagsId,
    string? SerieBilagsId,
    int Bilagsnummer,
    int? SerieNummer,
    string? SerieKode,
    int Ar,
    string Type,
    DateOnly Bilagsdato,
    DateTime Registreringsdato,
    string Beskrivelse,
    string? EksternReferanse,
    RegnskapsperiodeDto Periode,
    List<PosteringDto> Posteringer,
    List<VedleggDto> Vedlegg,
    decimal SumDebet,
    decimal SumKredit,
    bool ErBokfort,
    DateTime? BokfortTidspunkt,
    bool ErTilbakfort,
    Guid? TilbakefortFraBilagId,
    Guid? TilbakefortAvBilagId);

public record PosteringDto(
    Guid Id,
    int Linjenummer,
    string Kontonummer,
    string Kontonavn,
    string Side,
    decimal Belop,
    string Beskrivelse,
    string? MvaKode,
    decimal? MvaBelop,
    decimal? MvaGrunnlag,
    decimal? MvaSats,
    string? Avdelingskode,
    string? Prosjektkode,
    Guid? KundeId,
    Guid? LeverandorId,
    bool ErAutoGenerertMva);

public record VedleggDto(
    Guid Id,
    string Filnavn,
    string MimeType,
    long Storrelse,
    string LagringSti,
    string? Beskrivelse,
    int Rekkefolge,
    DateTime OpplastetTidspunkt);

public record BilagSerieDto(
    Guid Id,
    string Kode,
    string Navn,
    string? NavnEn,
    string StandardType,
    bool ErAktiv,
    bool ErSystemserie,
    string SaftJournalId);

public record BilagValideringResultatDto(
    bool ErGyldig,
    List<BilagValideringFeilDto> Feil,
    List<BilagValideringAdvarselDto> Advarsler,
    List<PosteringDto>? GenererteMvaPosteringer);

public record BilagValideringFeilDto(
    string Kode,
    string Melding,
    int? Linjenummer);

public record BilagValideringAdvarselDto(
    string Kode,
    string Melding,
    int? Linjenummer);

public record BilagSokResultatDto(
    List<BilagDto> Data,
    int TotaltAntall,
    int Side,
    int Antall);
