// TypeScript-typer for Hovedbok-modulen
// Matcher API DTOs fra spec-hovedbok.md

import type { Kontotype, Normalbalanse } from './kontoplan';

// --- Enums (som const objekter for erasableSyntaxOnly) ---

export const PeriodeStatus = {
  Apen: 'Apen',
  Sperret: 'Sperret',
  Lukket: 'Lukket',
} as const;
export type PeriodeStatus = (typeof PeriodeStatus)[keyof typeof PeriodeStatus];

export const PeriodeStatusNavn: Record<PeriodeStatus, string> = {
  [PeriodeStatus.Apen]: 'Åpen',
  [PeriodeStatus.Sperret]: 'Sperret',
  [PeriodeStatus.Lukket]: 'Lukket',
};

export const BilagType = {
  Manuelt: 'Manuelt',
  InngaendeFaktura: 'InngaendeFaktura',
  UtgaendeFaktura: 'UtgaendeFaktura',
  Bank: 'Bank',
  Lonn: 'Lonn',
  Avskrivning: 'Avskrivning',
  MvaOppgjor: 'MvaOppgjor',
  Arsavslutning: 'Arsavslutning',
  Apningsbalanse: 'Apningsbalanse',
  Kreditnota: 'Kreditnota',
  Korreksjon: 'Korreksjon',
} as const;
export type BilagType = (typeof BilagType)[keyof typeof BilagType];

export const BokforingSide = {
  Debet: 'Debet',
  Kredit: 'Kredit',
} as const;
export type BokforingSide = (typeof BokforingSide)[keyof typeof BokforingSide];

// --- Regnskapsperiode DTOs ---

export interface PeriodeDto {
  id: string;
  ar: number;
  periode: number;
  periodenavn: string;
  fraDato: string;
  tilDato: string;
  status: PeriodeStatus;
  lukketTidspunkt: string | null;
  lukketAv: string | null;
  antallBilag: number;
  sumDebet: number;
  sumKredit: number;
}

export interface PerioderResponse {
  ar: number;
  perioder: PeriodeDto[];
}

export interface OpprettArRequest {
  ar: number;
}

export interface EndreStatusRequest {
  nyStatus: PeriodeStatus;
  merknad?: string;
}

// --- Bilag DTOs ---

export interface PosteringLinjeRequest {
  kontonummer: string;
  side: BokforingSide;
  belop: number;
  beskrivelse: string;
  mvaKode?: string | null;
  avdelingskode?: string | null;
  prosjektkode?: string | null;
}

export interface OpprettBilagRequest {
  type: BilagType;
  bilagsdato: string;
  beskrivelse: string;
  eksternReferanse?: string | null;
  posteringer: PosteringLinjeRequest[];
}

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
}

export interface BilagDto {
  id: string;
  bilagsId: string;
  bilagsnummer: number;
  ar: number;
  type: BilagType;
  bilagsdato: string;
  registreringsdato: string;
  beskrivelse: string;
  eksternReferanse: string | null;
  periode: { ar: number; periode: number };
  posteringer: PosteringLinjeDto[];
  sumDebet: number;
  sumKredit: number;
}

// --- Kontoutskrift DTOs ---

export interface KontoutskriftPosteringDto {
  bilagsdato: string;
  bilagsId: string;
  bilagBeskrivelse: string;
  linjenummer: number;
  beskrivelse: string;
  side: BokforingSide;
  belop: number;
  lOpendeBalanse: number;
}

export interface KontoutskriftResponse {
  kontonummer: string;
  kontonavn: string;
  kontotype: Kontotype;
  normalbalanse: Normalbalanse;
  fraDato: string;
  tilDato: string;
  inngaendeBalanse: number;
  posteringer: KontoutskriftPosteringDto[];
  sumDebet: number;
  sumKredit: number;
  utgaendeBalanse: number;
  totaltAntall: number;
  side: number;
  antall: number;
}

// --- Saldobalanse DTOs ---

export interface SaldobalanseKontoDto {
  kontonummer: string;
  kontonavn: string;
  kontotype: Kontotype;
  inngaendeBalanse: number;
  sumDebet: number;
  sumKredit: number;
  endring: number;
  utgaendeBalanse: number;
}

export interface SaldobalanseResponse {
  ar: number;
  periode: number;
  periodenavn: string;
  kontoer: SaldobalanseKontoDto[];
  totalSumDebet: number;
  totalSumKredit: number;
  erIBalanse: boolean;
}

// --- Saldooppslag DTOs ---

export interface KontoSaldoPeriodeDto {
  periode: number;
  inngaendeBalanse: number;
  sumDebet: number;
  sumKredit: number;
  utgaendeBalanse: number;
  antallPosteringer: number;
}

export interface KontoSaldoResponse {
  kontonummer: string;
  kontonavn: string;
  ar: number;
  perioder: KontoSaldoPeriodeDto[];
  totalInngaendeBalanse: number;
  totalSumDebet: number;
  totalSumKredit: number;
  totalUtgaendeBalanse: number;
}

// --- Soekeparametre ---

export interface BilagSokParams {
  ar?: number;
  periode?: number;
  type?: BilagType;
  side?: number;
  antall?: number;
}

export interface KontoutskriftParams {
  fraDato?: string;
  tilDato?: string;
  side?: number;
  antall?: number;
}

export interface SaldobalanseParams {
  inkluderNullsaldo?: boolean;
  kontoklasse?: number;
}
