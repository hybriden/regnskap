// TypeScript-typer for Periodeavslutning-modulen
// Matcher API DTOs fra spec-periodeavslutning.md
// TODO: Synk med backend

// --- Enums (som const objekter for erasableSyntaxOnly) ---

export const PeriodiseringsType = {
  ForskuddsbetaltKostnad: 'ForskuddsbetaltKostnad',
  PaloptKostnad: 'PaloptKostnad',
  ForskuddsbetaltInntekt: 'ForskuddsbetaltInntekt',
  OpptjentInntekt: 'OpptjentInntekt',
} as const;
export type PeriodiseringsType = (typeof PeriodiseringsType)[keyof typeof PeriodiseringsType];

export const PeriodiseringsTypeNavn: Record<PeriodiseringsType, string> = {
  [PeriodiseringsType.ForskuddsbetaltKostnad]: 'Forskuddsbetalt kostnad',
  [PeriodiseringsType.PaloptKostnad]: 'Påløpt kostnad',
  [PeriodiseringsType.ForskuddsbetaltInntekt]: 'Forskuddsbetalt inntekt',
  [PeriodiseringsType.OpptjentInntekt]: 'Opptjent inntekt',
};

export const PeriodeLukkingSteg = {
  AvskrivningerBeregnet: 'AvskrivningerBeregnet',
  PeriodiseringerBokfort: 'PeriodiseringerBokfort',
  AvstemmingKjort: 'AvstemmingKjort',
  SaldokontrollBestatt: 'SaldokontrollBestatt',
  BilagsnummerKontrollert: 'BilagsnummerKontrollert',
  MvaAvstemt: 'MvaAvstemt',
  PeriodeLukket: 'PeriodeLukket',
} as const;
export type PeriodeLukkingSteg =
  (typeof PeriodeLukkingSteg)[keyof typeof PeriodeLukkingSteg];

export const ArsavslutningFase = {
  IkkeStartet: 'IkkeStartet',
  AllePerioderLukket: 'AllePerioderLukket',
  ArsoppgjorBilagOpprettet: 'ArsoppgjorBilagOpprettet',
  ResultatDisponert: 'ResultatDisponert',
  ResultatkontoerNullstilt: 'ResultatkontoerNullstilt',
  ApningsbalanseOpprettet: 'ApningsbalanseOpprettet',
  Fullfort: 'Fullfort',
} as const;
export type ArsavslutningFase =
  (typeof ArsavslutningFase)[keyof typeof ArsavslutningFase];

export const ArsavslutningFaseNavn: Record<ArsavslutningFase, string> = {
  [ArsavslutningFase.IkkeStartet]: 'Ikke startet',
  [ArsavslutningFase.AllePerioderLukket]: 'Alle perioder lukket',
  [ArsavslutningFase.ArsoppgjorBilagOpprettet]: 'Årsoppgjørsbilag opprettet',
  [ArsavslutningFase.ResultatDisponert]: 'Resultat disponert',
  [ArsavslutningFase.ResultatkontoerNullstilt]: 'Resultatkontoer nullstilt',
  [ArsavslutningFase.ApningsbalanseOpprettet]: 'Åpningsbalanse opprettet',
  [ArsavslutningFase.Fullfort]: 'Fullført',
};

// --- Månedlig avstemming DTOs ---

export interface AvstemmingResultatDto {
  ar: number;
  periode: number;
  erKlarForLukking: boolean;
  kontroller: AvstemmingKontrollDto[];
  advarsler: AvstemmingAdvarselDto[];
}

export interface AvstemmingKontrollDto {
  kode: string;
  beskrivelse: string;
  status: string; // "OK", "ADVARSEL", "FEIL"
  detaljer: string | null;
}

export interface AvstemmingAdvarselDto {
  kode: string;
  melding: string;
  alvorlighet: string; // "INFO", "ADVARSEL", "KRITISK"
}

// --- Månedlig lukking DTOs ---

export interface LukkPeriodeRequest {
  merknad?: string;
  tvingLukking?: boolean;
}

export interface PeriodeLukkingDto {
  ar: number;
  periode: number;
  nyStatus: string;
  lukketTidspunkt: string;
  lukketAv: string;
  avstemming: AvstemmingResultatDto;
  logg: PeriodeLukkingLoggDto[];
}

export interface PeriodeLukkingLoggDto {
  steg: string;
  beskrivelse: string;
  status: string;
  detaljer: string | null;
  tidspunkt: string;
}

export interface GjenapnePeriodeRequest {
  begrunnelse: string;
}

// --- Årsavslutning DTOs ---

export interface ArsavslutningRequest {
  disponeringKontonummer?: string;
  utbytte?: number;
  utbytteKontonummer?: string;
}

export interface ArsavslutningDto {
  ar: number;
  fase: ArsavslutningFase;
  arsresultat: number;
  utbytte: number | null;
  disponertTilEgenkapital: number;
  arsavslutningBilagId: string;
  apningsbalanseBilagId: string;
  steg: ArsavslutningStegDto[];
  fullfortTidspunkt: string;
  fullfortAv: string;
}

export interface ArsavslutningStegDto {
  steg: string;
  beskrivelse: string;
  status: string;
  detaljer: string | null;
}

export interface ArsavslutningStatusDto {
  ar: number;
  fase: ArsavslutningFase;
  arsresultat: number | null;
  disponeringKontonummer: string | null;
  arsavslutningBilagId: string | null;
  apningsbalanseBilagId: string | null;
  fullfortTidspunkt: string | null;
  fullfortAv: string | null;
}

// --- Anleggsmiddel DTOs ---

export interface AnleggsmiddelDto {
  id: string;
  navn: string;
  beskrivelse: string | null;
  anskaffelsesdato: string;
  anskaffelseskostnad: number;
  restverdi: number;
  levetidManeder: number;
  balanseKontonummer: string;
  avskrivningsKontonummer: string;
  akkumulertAvskrivningKontonummer: string;
  avdelingskode: string | null;
  prosjektkode: string | null;
  erAktivt: boolean;
  utrangeringsDato: string | null;
  avskrivningsgrunnlag: number;
  manedligAvskrivning: number;
  arligAvskrivning: number;
  akkumulertAvskrivning: number;
  bokfortVerdi: number;
  gjenvaerendeAvskrivning: number;
  erFulltAvskrevet: boolean;
  avskrivninger: AvskrivningHistorikkDto[];
}

export interface AvskrivningHistorikkDto {
  id: string;
  anleggsmiddelId: string;
  ar: number;
  periode: number;
  belop: number;
  akkumulertEtter: number;
  bokfortVerdiEtter: number;
  bilagId: string;
}

export interface OpprettAnleggsmiddelRequest {
  navn: string;
  beskrivelse?: string;
  anskaffelsesdato: string;
  anskaffelseskostnad: number;
  restverdi: number;
  levetidManeder: number;
  balanseKontonummer: string;
  avskrivningsKontonummer: string;
  akkumulertAvskrivningKontonummer: string;
  avdelingskode?: string;
  prosjektkode?: string;
}

// --- Avskrivning beregning DTOs ---

export interface BeregnAvskrivningerRequest {
  ar: number;
  periode: number;
}

export interface AvskrivningBeregningDto {
  ar: number;
  periode: number;
  linjer: AvskrivningLinjeDto[];
  totalAvskrivning: number;
  antallAnleggsmidler: number;
}

export interface AvskrivningLinjeDto {
  anleggsmiddelId: string;
  navn: string;
  balanseKontonummer: string;
  avskrivningsKontonummer: string;
  akkumulertAvskrivningKontonummer: string;
  belop: number;
  akkumulertFor: number;
  akkumulertEtter: number;
  bokfortVerdiEtter: number;
  erSisteAvskrivning: boolean;
}

export interface BokforAvskrivningerRequest {
  ar: number;
  periode: number;
}

// --- Periodisering DTOs ---

export interface PeriodiseringDto {
  id: string;
  beskrivelse: string;
  type: PeriodiseringsType;
  totalBelop: number;
  valuta: string;
  fraAr: number;
  fraPeriode: number;
  tilAr: number;
  tilPeriode: number;
  balanseKontonummer: string;
  resultatKontonummer: string;
  avdelingskode: string | null;
  prosjektkode: string | null;
  erAktiv: boolean;
  opprinneligBilagId: string | null;
  antallPerioder: number;
  belopPerPeriode: number;
  sumPeriodisert: number;
  gjenstaaendeBelop: number;
  posteringer: PeriodiseringsHistorikkDto[];
}

export interface PeriodiseringsHistorikkDto {
  id: string;
  periodiseringId: string;
  ar: number;
  periode: number;
  belop: number;
  akkumulertEtter: number;
  gjenstaarEtter: number;
  bilagId: string;
}

export interface OpprettPeriodiseringRequest {
  beskrivelse: string;
  type: PeriodiseringsType;
  totalBelop: number;
  fraAr: number;
  fraPeriode: number;
  tilAr: number;
  tilPeriode: number;
  balanseKontonummer: string;
  resultatKontonummer: string;
  avdelingskode?: string;
  prosjektkode?: string;
  opprinneligBilagId?: string;
}

export interface BokforPeriodiseringerRequest {
  ar: number;
  periode: number;
}

export interface PeriodiseringBokforingDto {
  ar: number;
  periode: number;
  linjer: PeriodiseringLinjeDto[];
  totalBelop: number;
  bilagId: string;
}

export interface PeriodiseringLinjeDto {
  periodiseringId: string;
  beskrivelse: string;
  type: PeriodiseringsType;
  belop: number;
  gjenstaarEtter: number;
}

// --- Årsregnskapsklargjøring DTOs ---

export interface ArsregnskapsklarDto {
  ar: number;
  erKlar: boolean;
  kontroller: KlargjoringKontrollDto[];
  frister: FilingDeadlineDto;
}

export interface KlargjoringKontrollDto {
  kode: string;
  beskrivelse: string;
  status: string; // "OK", "FEIL", "ADVARSEL"
  detaljer: string | null;
}

export interface FilingDeadlineDto {
  godkjenningsfrist: string;
  innsendingsfrist: string;
  erFristUtlopt: boolean;
}

// --- Periodeoversikt (for hovedsiden) ---

export interface PeriodeStatusDto {
  ar: number;
  periode: number;
  periodenavn: string;
  status: string; // "Apen", "Sperret", "Lukket"
  lukketTidspunkt: string | null;
  lukketAv: string | null;
  antallBilag: number;
  sumDebet: number;
  sumKredit: number;
}
