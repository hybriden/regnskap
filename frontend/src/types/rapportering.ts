// TypeScript-typer for Rapportering-modulen
// Matcher API DTOs fra spec-rapportering.md
// TODO: Synk med backend

// --- Enums (som const objekter for erasableSyntaxOnly) ---

export const RapportType = {
  Resultatregnskap: 'Resultatregnskap',
  Balanse: 'Balanse',
  Kontantstromoppstilling: 'Kontantstromoppstilling',
  Saldobalanse: 'Saldobalanse',
  Hovedboksutskrift: 'Hovedboksutskrift',
  SaftEksport: 'SaftEksport',
  Dimensjonsrapport: 'Dimensjonsrapport',
  Sammenligning: 'Sammenligning',
  Nokkeltall: 'Nokkeltall',
} as const;
export type RapportType = (typeof RapportType)[keyof typeof RapportType];

export const ResultatregnskapFormat = {
  Artsinndelt: 'artsinndelt',
  Funksjonsinndelt: 'funksjonsinndelt',
} as const;
export type ResultatregnskapFormat = (typeof ResultatregnskapFormat)[keyof typeof ResultatregnskapFormat];

// --- Resultatregnskap ---

export interface ResultatregnskapLinjeDto {
  kontonummer: string;
  kontonavn: string;
  belop: number;
  forrigeArBelop: number | null;
  erSummeringslinje: boolean;
}

export interface ResultatregnskapSeksjonDto {
  kode: string;
  navn: string;
  linjer: ResultatregnskapLinjeDto[];
  sum: number;
  forrigeArSum: number | null;
}

export interface ResultatregnskapDto {
  ar: number;
  fraPeriode: number;
  tilPeriode: number;
  format: string;
  seksjoner: ResultatregnskapSeksjonDto[];
  driftsresultat: number;
  finansresultatNetto: number;
  ordnaertResultatForSkatt: number;
  skattekostnad: number;
  arsresultat: number;
  forrigeArDriftsresultat: number | null;
  forrigeArArsresultat: number | null;
}

export interface ResultatregnskapParams {
  ar: number;
  fraPeriode?: number;
  tilPeriode?: number;
  format?: ResultatregnskapFormat;
  inkluderForrigeAr?: boolean;
}

// --- Balanse ---

export interface BalanseLinjeDto {
  kontonummer: string;
  kontonavn: string;
  belop: number;
  forrigeArBelop: number | null;
  erSummeringslinje: boolean;
}

export interface BalanseSeksjonDto {
  kode: string;
  navn: string;
  linjer: BalanseLinjeDto[];
  sum: number;
  forrigeArSum: number | null;
}

export interface BalanseSideDto {
  seksjoner: BalanseSeksjonDto[];
  sum: number;
  forrigeArSum: number | null;
}

export interface BalanseDto {
  ar: number;
  periode: number;
  eiendeler: BalanseSideDto;
  egenkapitalOgGjeld: BalanseSideDto;
  sumEiendeler: number;
  sumEgenkapitalOgGjeld: number;
  erIBalanse: boolean;
  forrigeArSumEiendeler: number | null;
  forrigeArSumEgenkapitalOgGjeld: number | null;
}

export interface BalanseParams {
  ar: number;
  periode?: number;
  inkluderForrigeAr?: boolean;
}

// --- Kontantstrom ---

export interface KontantstromLinjeDto {
  beskrivelse: string;
  belop: number;
  forrigeArBelop: number | null;
}

export interface KontantstromSeksjonDto {
  navn: string;
  linjer: KontantstromLinjeDto[];
  sum: number;
  forrigeArSum: number | null;
}

export interface KontantstromDto {
  ar: number;
  drift: KontantstromSeksjonDto;
  investering: KontantstromSeksjonDto;
  finansiering: KontantstromSeksjonDto;
  nettoEndringLikvider: number;
  likviderIB: number;
  likviderUB: number;
  forrigeArNettoEndring: number | null;
}

export interface KontantstromParams {
  ar: number;
  inkluderForrigeAr?: boolean;
}

// --- Saldobalanse (utvidet rapport) ---

export interface SaldobalanseRapportLinjeDto {
  kontonummer: string;
  kontonavn: string;
  kontotype: string;
  inngaendeBalanse: number;
  sumDebet: number;
  sumKredit: number;
  endring: number;
  utgaendeBalanse: number;
}

export interface SaldobalanseGruppeDto {
  gruppekode: number;
  gruppenavn: string;
  linjer: SaldobalanseRapportLinjeDto[];
  gruppeIB: number;
  gruppeSumDebet: number;
  gruppeSumKredit: number;
  gruppeUB: number;
}

export interface SaldobalanseTotalerDto {
  totalIB: number;
  totalDebet: number;
  totalKredit: number;
  totalUB: number;
  debetSaldo: number;
  kreditSaldo: number;
}

export interface SaldobalanseRapportDto {
  ar: number;
  fraPeriode: number;
  tilPeriode: number;
  grupper: SaldobalanseGruppeDto[];
  totaler: SaldobalanseTotalerDto;
  debetKredittStemmer: boolean;
}

export interface SaldobalanseRapportParams {
  ar: number;
  fraPeriode?: number;
  tilPeriode?: number;
  inkluderNullsaldo?: boolean;
  kontoklasse?: number;
  gruppert?: boolean;
}

// --- Hovedboksutskrift ---

export interface PosteringUtskriftDto {
  dato: string;
  bilagsId: string;
  beskrivelse: string;
  linjenummer: number;
  side: string;
  belop: number;
  lopendeSaldo: number;
  mvaKode: string | null;
  avdelingskode: string | null;
  prosjektkode: string | null;
  motkontonummer: string | null;
}

export interface KontoUtskriftDto {
  kontonummer: string;
  kontonavn: string;
  kontotype: string;
  normalbalanse: string;
  inngaendeBalanse: number;
  posteringer: PosteringUtskriftDto[];
  sumDebet: number;
  sumKredit: number;
  utgaendeBalanse: number;
  antallPosteringer: number;
}

export interface HovedboksutskriftDto {
  ar: number;
  fraPeriode: number;
  tilPeriode: number;
  kontoer: KontoUtskriftDto[];
}

export interface HovedboksutskriftParams {
  ar: number;
  fraPeriode?: number;
  tilPeriode?: number;
  fraKonto?: string;
  tilKonto?: string;
}

// --- SAF-T Eksport ---

export interface SaftEksportRequest {
  ar: number;
  fraPeriode?: number;
  tilPeriode?: number;
  taxAccountingBasis?: string;
}

// --- Dimensjonsrapport ---

export interface DimensjonsLinjeDto {
  kontonummer: string;
  kontonavn: string;
  sumDebet: number;
  sumKredit: number;
  netto: number;
}

export interface DimensjonsGruppeDto {
  kode: string;
  navn: string;
  linjer: DimensjonsLinjeDto[];
  sumDebet: number;
  sumKredit: number;
  netto: number;
}

export interface DimensjonsTotalerDto {
  totalDebet: number;
  totalKredit: number;
  totalNetto: number;
  antallPosteringer: number;
}

export interface DimensjonsrapportDto {
  ar: number;
  fraPeriode: number;
  tilPeriode: number;
  dimensjon: string;
  grupper: DimensjonsGruppeDto[];
  totaler: DimensjonsTotalerDto;
}

// --- Sammenligning ---

export interface SammenligningLinjeDto {
  kontonummer: string;
  kontonavn: string;
  kontotype: string;
  faktisk: number;
  sammenligning: number;
  avvikBelop: number;
  avvikProsent: number;
}

export interface SammenligningTotalerDto {
  totalFaktisk: number;
  totalSammenligning: number;
  totalAvvik: number;
  totalAvvikProsent: number;
}

export interface SammenligningDto {
  ar: number;
  fraPeriode: number;
  tilPeriode: number;
  type: string;
  linjer: SammenligningLinjeDto[];
  totaler: SammenligningTotalerDto;
}

export interface SammenligningParams {
  ar: number;
  fraPeriode?: number;
  tilPeriode?: number;
  type: 'forrige_ar' | 'budsjett';
  budsjettVersjon?: string;
  kontoklasse?: number;
}

// --- Nokkeltall ---

export interface LikviditetDto {
  likviditetsgrad1: number;
  likviditetsgrad2: number;
  arbeidskapital: number;
}

export interface SoliditetDto {
  egenkapitalandel: number;
  gjeldsgrad: number;
  rentedekningsgrad: number;
}

export interface LonnsomhetDto {
  totalkapitalrentabilitet: number;
  egenkapitalrentabilitet: number;
  resultatmargin: number;
  driftsmargin: number;
}

export interface NokkeltallDto {
  ar: number;
  periode: number;
  likviditet: LikviditetDto;
  soliditet: SoliditetDto;
  lonnsomhet: LonnsomhetDto;
  forrigeAr: NokkeltallDto | null;
}

export interface NokkeltallParams {
  ar: number;
  periode?: number;
  inkluderForrigeAr?: boolean;
}

// --- Budsjett ---

export interface BudsjettDto {
  id: string;
  kontonummer: string;
  ar: number;
  periode: number;
  belop: number;
  versjon: string;
  merknad: string | null;
}

export interface OpprettBudsjettRequest {
  kontonummer: string;
  ar: number;
  periode: number;
  belop: number;
  versjon?: string;
  merknad?: string | null;
}

export interface BudsjettLinjeRequest {
  kontonummer: string;
  periode: number;
  belop: number;
}

export interface BudsjettBulkRequest {
  ar: number;
  versjon: string;
  linjer: BudsjettLinjeRequest[];
}

// --- Rapport Konfigurasjon ---

export interface RapportKonfigurasjonDto {
  firmanavn: string;
  organisasjonsnummer: string;
  adresse: string;
  postnummer: string;
  poststed: string;
  landskode: string;
  erMvaRegistrert: boolean;
  kontaktperson: string | null;
  telefon: string | null;
  epost: string | null;
  bankkontonummer: string | null;
  iban: string | null;
  erSmaForetak: boolean;
  valuta: string;
}

// --- Rapport Logg ---

export interface RapportLoggDto {
  id: string;
  type: RapportType;
  ar: number;
  fraPeriode: number | null;
  tilPeriode: number | null;
  genererTidspunkt: string;
  genererAv: string;
  parametre: string | null;
  kontrollsum: string | null;
}
