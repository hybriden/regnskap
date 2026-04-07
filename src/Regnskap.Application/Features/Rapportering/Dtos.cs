namespace Regnskap.Application.Features.Rapportering;

// --- Resultatregnskap ---

public record ResultatregnskapDto(
    int Ar,
    int FraPeriode,
    int TilPeriode,
    string Format,
    List<ResultatregnskapSeksjonDto> Seksjoner,
    decimal Driftsresultat,
    decimal FinansresultatNetto,
    decimal OrdnaertResultatForSkatt,
    decimal Skattekostnad,
    decimal Arsresultat,
    decimal? ForrigeArDriftsresultat,
    decimal? ForrigeArArsresultat
);

public record ResultatregnskapSeksjonDto(
    string Kode,
    string Navn,
    List<ResultatregnskapLinjeDto> Linjer,
    decimal Sum,
    decimal? ForrigeArSum
);

public record ResultatregnskapLinjeDto(
    string Kontonummer,
    string Kontonavn,
    decimal Belop,
    decimal? ForrigeArBelop,
    bool ErSummeringslinje
);

// --- Balanse ---

public record BalanseDto(
    int Ar,
    int Periode,
    BalanseSideDto Eiendeler,
    BalanseSideDto EgenkapitalOgGjeld,
    decimal SumEiendeler,
    decimal SumEgenkapitalOgGjeld,
    bool ErIBalanse,
    decimal? ForrigeArSumEiendeler,
    decimal? ForrigeArSumEgenkapitalOgGjeld
);

public record BalanseSideDto(
    List<BalanseSeksjonDto> Seksjoner,
    decimal Sum,
    decimal? ForrigeArSum
);

public record BalanseSeksjonDto(
    string Kode,
    string Navn,
    List<BalanseLinjeDto> Linjer,
    decimal Sum,
    decimal? ForrigeArSum
);

public record BalanseLinjeDto(
    string Kontonummer,
    string Kontonavn,
    decimal Belop,
    decimal? ForrigeArBelop,
    bool ErSummeringslinje
);

// --- Kontantstrom ---

public record KontantstromDto(
    int Ar,
    KontantstromSeksjonDto Drift,
    KontantstromSeksjonDto Investering,
    KontantstromSeksjonDto Finansiering,
    decimal NettoEndringLikvider,
    decimal LikviderIB,
    decimal LikviderUB,
    decimal? ForrigeArNettoEndring
);

public record KontantstromSeksjonDto(
    string Navn,
    List<KontantstromLinjeDto> Linjer,
    decimal Sum,
    decimal? ForrigeArSum
);

public record KontantstromLinjeDto(
    string Beskrivelse,
    decimal Belop,
    decimal? ForrigeArBelop
);

// --- Saldobalanse ---

public record SaldobalanseRapportDto(
    int Ar,
    int FraPeriode,
    int TilPeriode,
    List<SaldobalanseGruppeDto> Grupper,
    SaldobalanseTotalerDto Totaler,
    bool DebetKredittStemmer
);

public record SaldobalanseGruppeDto(
    int Gruppekode,
    string Gruppenavn,
    List<SaldobalanseRapportLinjeDto> Linjer,
    decimal GruppeIB,
    decimal GruppeSumDebet,
    decimal GruppeSumKredit,
    decimal GruppeUB
);

public record SaldobalanseRapportLinjeDto(
    string Kontonummer,
    string Kontonavn,
    string Kontotype,
    decimal InngaendeBalanse,
    decimal SumDebet,
    decimal SumKredit,
    decimal Endring,
    decimal UtgaendeBalanse
);

public record SaldobalanseTotalerDto(
    decimal TotalIB,
    decimal TotalDebet,
    decimal TotalKredit,
    decimal TotalUB,
    decimal DebetSaldo,
    decimal KreditSaldo
);

// --- Hovedboksutskrift ---

public record HovedboksutskriftDto(
    int Ar,
    int FraPeriode,
    int TilPeriode,
    List<KontoUtskriftDto> Kontoer
);

public record KontoUtskriftDto(
    string Kontonummer,
    string Kontonavn,
    string Kontotype,
    string Normalbalanse,
    decimal InngaendeBalanse,
    List<PosteringUtskriftDto> Posteringer,
    decimal SumDebet,
    decimal SumKredit,
    decimal UtgaendeBalanse,
    int AntallPosteringer
);

public record PosteringUtskriftDto(
    DateOnly Dato,
    string BilagsId,
    string Beskrivelse,
    int Linjenummer,
    string Side,
    decimal Belop,
    decimal LopendeSaldo,
    string? MvaKode,
    string? Avdelingskode,
    string? Prosjektkode,
    string? Motkontonummer
);

// --- Dimensjonsrapport ---

public record DimensjonsrapportDto(
    int Ar,
    int FraPeriode,
    int TilPeriode,
    string Dimensjon,
    List<DimensjonsGruppeDto> Grupper,
    DimensjonsTotalerDto Totaler
);

public record DimensjonsGruppeDto(
    string Kode,
    string Navn,
    List<DimensjonsLinjeDto> Linjer,
    decimal SumDebet,
    decimal SumKredit,
    decimal Netto
);

public record DimensjonsLinjeDto(
    string Kontonummer,
    string Kontonavn,
    decimal SumDebet,
    decimal SumKredit,
    decimal Netto
);

public record DimensjonsTotalerDto(
    decimal TotalDebet,
    decimal TotalKredit,
    decimal TotalNetto,
    int AntallPosteringer
);

// --- Sammenligning ---

public record SammenligningDto(
    int Ar,
    int FraPeriode,
    int TilPeriode,
    string Type,
    List<SammenligningLinjeDto> Linjer,
    SammenligningTotalerDto Totaler
);

public record SammenligningLinjeDto(
    string Kontonummer,
    string Kontonavn,
    string Kontotype,
    decimal Faktisk,
    decimal Sammenligning,
    decimal AvvikBelop,
    decimal AvvikProsent
);

public record SammenligningTotalerDto(
    decimal TotalFaktisk,
    decimal TotalSammenligning,
    decimal TotalAvvik,
    decimal TotalAvvikProsent
);

// --- Nokkeltall ---

public record NokkeltallRapportDto(
    int Ar,
    int Periode,
    LikviditetDto Likviditet,
    SoliditetDto Soliditet,
    LonnsomhetDto Lonnsomhet,
    NokkeltallRapportDto? ForrigeAr
);

public record LikviditetDto(
    decimal Likviditetsgrad1,
    decimal Likviditetsgrad2,
    decimal Arbeidskapital
);

public record SoliditetDto(
    decimal Egenkapitalandel,
    decimal Gjeldsgrad,
    decimal Rentedekningsgrad
);

public record LonnsomhetDto(
    decimal Totalkapitalrentabilitet,
    decimal Egenkapitalrentabilitet,
    decimal Resultatmargin,
    decimal Driftsmargin
);

// --- Budsjett ---

public record BudsjettDto(
    Guid Id,
    string Kontonummer,
    int Ar,
    int Periode,
    decimal Belop,
    string Versjon,
    string? Merknad
);

public record OpprettBudsjettRequest(
    string Kontonummer,
    int Ar,
    int Periode,
    decimal Belop,
    string Versjon = "Opprinnelig",
    string? Merknad = null
);

public record BudsjettBulkRequest(
    int Ar,
    string Versjon,
    List<BudsjettLinjeRequest> Linjer
);

public record BudsjettLinjeRequest(
    string Kontonummer,
    int Periode,
    decimal Belop
);

// --- SAF-T ---

public record SaftEksportRequest(
    int Ar,
    int FraPeriode = 1,
    int TilPeriode = 12,
    string TaxAccountingBasis = "A"
);

// --- Hjelpetyper for aggregering ---

public record KontoSaldoAggregat(
    string Kontonummer,
    string Kontonavn,
    string Kontotype,
    string Normalbalanse,
    int Gruppekode,
    string Gruppenavn,
    decimal InngaendeBalanse,
    decimal SumDebet,
    decimal SumKredit,
    decimal UtgaendeBalanse
);

public record DimensjonsSaldoAggregat(
    string DimensjonsKode,
    string Kontonummer,
    string Kontonavn,
    decimal SumDebet,
    decimal SumKredit,
    decimal Netto
);
