// TypeScript-typer for Bilag-modulen
// Matcher API DTOs fra spec-bilag.md

import type { BilagType, BokforingSide } from './hovedbok';

// --- Enums (som const objekter for erasableSyntaxOnly) ---

export const BilagStatus = {
  Kladd: 'Kladd',
  Validert: 'Validert',
  Bokfort: 'Bokfort',
  Tilbakfort: 'Tilbakfort',
} as const;
export type BilagStatus = (typeof BilagStatus)[keyof typeof BilagStatus];

export const BilagStatusNavn: Record<BilagStatus, string> = {
  [BilagStatus.Kladd]: 'Kladd',
  [BilagStatus.Validert]: 'Validert',
  [BilagStatus.Bokfort]: 'Bokført',
  [BilagStatus.Tilbakfort]: 'Tilbakeført',
};

// --- Bilagserie DTOs ---

export interface BilagSerieDto {
  id: string;
  kode: string;
  navn: string;
  navnEn: string | null;
  standardType: string;
  erAktiv: boolean;
  erSystemserie: boolean;
  saftJournalId: string;
}

export interface OpprettBilagSerieRequest {
  kode: string;
  navn: string;
  navnEn?: string | null;
  standardType: string;
  saftJournalId: string;
}

export interface OppdaterBilagSerieRequest {
  navn: string;
  navnEn?: string | null;
  erAktiv: boolean;
}

// --- Vedlegg DTOs ---

export interface VedleggDto {
  id: string;
  filnavn: string;
  mimeType: string;
  storrelse: number;
  lagringSti: string;
  beskrivelse: string | null;
  rekkefolge: number;
  opplastetTidspunkt: string;
}

export interface LeggTilVedleggRequest {
  bilagId: string;
  filnavn: string;
  mimeType: string;
  storrelse: number;
  lagringSti: string;
  hashSha256: string;
  beskrivelse?: string | null;
}

// --- Postering DTOs (utvidet for bilagsmodul) ---

export interface PosteringLinjeDto {
  id: string;
  linjenummer: number;
  kontonummer: string;
  kontonavn: string;
  side: BokforingSide;
  belop: number;
  beskrivelse: string;
  mvaKode: string | null;
  mvaBelop: number | null;
  mvaGrunnlag: number | null;
  mvaSats: number | null;
  avdelingskode: string | null;
  prosjektkode: string | null;
  kundeId: string | null;
  leverandorId: string | null;
  erAutoGenerertMva: boolean;
}

export interface PosteringLinjeRequest {
  kontonummer: string;
  side: BokforingSide;
  belop: number;
  beskrivelse: string;
  mvaKode?: string | null;
  avdelingskode?: string | null;
  prosjektkode?: string | null;
  kundeId?: string | null;
  leverandorId?: string | null;
}

// --- Bilag DTOs ---

export interface BilagDto {
  id: string;
  bilagsId: string;
  serieBilagsId: string | null;
  bilagsnummer: number;
  serieNummer: number | null;
  serieKode: string | null;
  ar: number;
  type: string;
  bilagsdato: string;
  registreringsdato: string;
  beskrivelse: string;
  eksternReferanse: string | null;
  periode: { ar: number; periode: number };
  posteringer: PosteringLinjeDto[];
  vedlegg: VedleggDto[];
  sumDebet: number;
  sumKredit: number;
  erBokfort: boolean;
  bokfortTidspunkt: string | null;
  erTilbakfort: boolean;
  tilbakefortFraBilagId: string | null;
  tilbakefortAvBilagId: string | null;
}

// --- Request DTOs ---

export interface OpprettBilagRequest {
  type: BilagType;
  bilagsdato: string;
  beskrivelse: string;
  eksternReferanse?: string | null;
  serieKode?: string | null;
  posteringer: PosteringLinjeRequest[];
  bokforDirekte?: boolean;
}

export interface TilbakeforBilagRequest {
  originalBilagId: string;
  tilbakeforingsdato: string;
  beskrivelse: string;
}

export interface ValiderBilagRequest {
  type: BilagType;
  bilagsdato: string;
  beskrivelse: string;
  serieKode?: string | null;
  posteringer: PosteringLinjeRequest[];
}

// --- Sok ---

export interface BilagSokParams {
  ar?: number;
  periode?: number;
  type?: BilagType;
  serieKode?: string;
  fraDato?: string;
  tilDato?: string;
  kontonummer?: string;
  minBelop?: number;
  maxBelop?: number;
  beskrivelse?: string;
  eksternReferanse?: string;
  bilagsnummer?: number;
  erBokfort?: boolean;
  erTilbakfort?: boolean;
  side?: number;
  antall?: number;
}

export interface BilagSokResultat {
  data: BilagDto[];
  totaltAntall: number;
  side: number;
  antall: number;
}

// --- Validering ---

export interface BilagValideringResultat {
  erGyldig: boolean;
  feil: BilagValideringFeil[];
  advarsler: BilagValideringAdvarsel[];
  genererteeMvaPosteringer: PosteringLinjeDto[] | null;
}

export interface BilagValideringFeil {
  kode: string;
  melding: string;
  linjenummer: number | null;
}

export interface BilagValideringAdvarsel {
  kode: string;
  melding: string;
  linjenummer: number | null;
}
