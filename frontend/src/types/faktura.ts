// TypeScript-typer for Fakturering-modulen
// Matcher API DTOs fra spec-faktura.md
// TODO: Synk med backend

import type { PagedResult as _PagedResult } from './kunde';
export type { PagedResult } from './kunde';

// --- Enums (som const objekter for erasableSyntaxOnly) ---

export const FakturaStatus = {
  Utkast: 'Utkast',
  Godkjent: 'Godkjent',
  Utstedt: 'Utstedt',
  Kreditert: 'Kreditert',
  Kansellert: 'Kansellert',
} as const;
export type FakturaStatus = (typeof FakturaStatus)[keyof typeof FakturaStatus];

export const FakturaStatusNavn: Record<FakturaStatus, string> = {
  [FakturaStatus.Utkast]: 'Utkast',
  [FakturaStatus.Godkjent]: 'Godkjent',
  [FakturaStatus.Utstedt]: 'Utstedt',
  [FakturaStatus.Kreditert]: 'Kreditert',
  [FakturaStatus.Kansellert]: 'Kansellert',
};

export const FakturaStatusFarge: Record<FakturaStatus, { bg: string; color: string }> = {
  [FakturaStatus.Utkast]: { bg: '#fff3e0', color: '#e65100' },
  [FakturaStatus.Godkjent]: { bg: '#e3f2fd', color: '#1565c0' },
  [FakturaStatus.Utstedt]: { bg: '#e8f5e9', color: '#2e7d32' },
  [FakturaStatus.Kreditert]: { bg: '#fce4ec', color: '#c62828' },
  [FakturaStatus.Kansellert]: { bg: '#f5f5f5', color: '#616161' },
};

export const FakturaDokumenttype = {
  Faktura: 'Faktura',
  Kreditnota: 'Kreditnota',
} as const;
export type FakturaDokumenttype =
  (typeof FakturaDokumenttype)[keyof typeof FakturaDokumenttype];

export const FakturaLeveringsformat = {
  Epost: 'Epost',
  Ehf: 'Ehf',
  Papir: 'Papir',
  Intern: 'Intern',
} as const;
export type FakturaLeveringsformat =
  (typeof FakturaLeveringsformat)[keyof typeof FakturaLeveringsformat];

export const FakturaLeveringsformatNavn: Record<FakturaLeveringsformat, string> = {
  [FakturaLeveringsformat.Epost]: 'E-post (PDF)',
  [FakturaLeveringsformat.Ehf]: 'EHF / PEPPOL',
  [FakturaLeveringsformat.Papir]: 'Papir (utskrift)',
  [FakturaLeveringsformat.Intern]: 'Intern',
};

export const RabattType = {
  Prosent: 'Prosent',
  Belop: 'Belop',
} as const;
export type RabattType = (typeof RabattType)[keyof typeof RabattType];

export const Enhet = {
  Stykk: 'Stykk',
  Timer: 'Timer',
  Kilogram: 'Kilogram',
  Liter: 'Liter',
  Meter: 'Meter',
  Kvadratmeter: 'Kvadratmeter',
  Pakke: 'Pakke',
  Maaned: 'Maaned',
  Dag: 'Dag',
} as const;
export type Enhet = (typeof Enhet)[keyof typeof Enhet];

export const EnhetNavn: Record<Enhet, string> = {
  [Enhet.Stykk]: 'stk',
  [Enhet.Timer]: 'timer',
  [Enhet.Kilogram]: 'kg',
  [Enhet.Liter]: 'liter',
  [Enhet.Meter]: 'm',
  [Enhet.Kvadratmeter]: 'm\u00B2',
  [Enhet.Pakke]: 'pk',
  [Enhet.Maaned]: 'mnd',
  [Enhet.Dag]: 'dag',
};

// --- Faktura Response DTOs ---

export interface FakturaResponse {
  id: string;
  fakturaId: string | null;
  dokumenttype: FakturaDokumenttype;
  status: FakturaStatus;
  kundeId: string;
  kundeNavn: string;
  kundenummer: string | null;
  fakturadato: string | null;
  forfallsdato: string | null;
  leveringsdato: string | null;
  belopEksMva: number;
  mvaBelop: number;
  belopInklMva: number;
  kidNummer: string | null;
  valutakode: string;
  bestillingsnummer: string | null;
  kjopersReferanse: string | null;
  vaarReferanse: string | null;
  eksternReferanse: string | null;
  merknad: string | null;
  leveringsformat: FakturaLeveringsformat;
  kreditertFakturaId: string | null;
  krediteringsaarsak: string | null;
  ehfGenerert: boolean;
  pdfFilsti: string | null;
  bilagId: string | null;
  linjer: FakturaLinjeResponse[];
  mvaLinjer: FakturaMvaLinjeResponse[];
}

export interface FakturaLinjeResponse {
  id: string;
  linjenummer: number;
  beskrivelse: string;
  antall: number;
  enhet: Enhet;
  enhetspris: number;
  rabattType: RabattType | null;
  rabattProsent: number | null;
  rabattBelop: number | null;
  nettobelop: number;
  mvaKode: string;
  mvaSats: number;
  mvaBelop: number;
  bruttobelop: number;
  kontonummer: string;
}

export interface FakturaMvaLinjeResponse {
  mvaKode: string;
  mvaSats: number;
  grunnlag: number;
  mvaBelop: number;
  ehfTaxCategoryId: string;
}

// --- Faktura Liste DTO (forenklet) ---

export interface FakturaListeResponse {
  id: string;
  fakturaId: string | null;
  dokumenttype: FakturaDokumenttype;
  status: FakturaStatus;
  kundeNavn: string;
  kundenummer: string | null;
  fakturadato: string | null;
  forfallsdato: string | null;
  belopEksMva: number;
  mvaBelop: number;
  belopInklMva: number;
  valutakode: string;
  leveringsformat: FakturaLeveringsformat;
  ehfGenerert: boolean;
}

// --- Request DTOs ---

export interface OpprettFakturaRequest {
  kundeId: string;
  leveringsdato?: string | null;
  leveringsperiodeSlutt?: string | null;
  bestillingsnummer?: string | null;
  kjopersReferanse?: string | null;
  vaarReferanse?: string | null;
  eksternReferanse?: string | null;
  merknad?: string | null;
  valutakode: string;
  leveringsformat: FakturaLeveringsformat;
  linjer: FakturaLinjeRequest[];
}

export interface FakturaLinjeRequest {
  beskrivelse: string;
  antall: number;
  enhet: Enhet;
  enhetspris: number;
  mvaKode: string;
  kontoId: string;
  rabattType?: RabattType | null;
  rabattProsent?: number | null;
  rabattBelop?: number | null;
  avdelingskode?: string | null;
  prosjektkode?: string | null;
}

export interface OpprettKreditnotaRequest {
  krediteringsaarsak: string;
  kjopersReferanse?: string | null;
  linjer?: KreditnotaLinjeRequest[] | null;
}

export interface KreditnotaLinjeRequest {
  opprinneligLinjenummer: number;
  antall: number;
}

// --- Lokale skjematyper (for frontend-tilstand) ---

export interface FakturaLinjeSkjema {
  key: string; // unik nøkkel for React-liste
  beskrivelse: string;
  antall: number;
  enhet: Enhet;
  enhetspris: number;
  mvaKode: string;
  mvaSats: number;
  kontoId: string;
  kontonummer: string;
  rabattType: RabattType | null;
  rabattProsent: number | null;
  rabattBelop: number | null;
}

// --- Beregnede verdier for en linje ---

export interface FakturaLinjeBeregnet {
  bruttolinjebelop: number;
  rabattBelop: number;
  nettobelop: number;
  mvaBelop: number;
  bruttobelop: number;
}

// --- MVA-koder (vanligste norske) ---

export const StandardMvaKoder = [
  { kode: '3', sats: 25, beskrivelse: 'Utgående MVA 25 %' },
  { kode: '31', sats: 15, beskrivelse: 'Utgående MVA 15 % (mat)' },
  { kode: '33', sats: 12, beskrivelse: 'Utgående MVA 12 % (transport)' },
  { kode: '5', sats: 0, beskrivelse: 'Fritatt MVA (eksport)' },
  { kode: '6', sats: 0, beskrivelse: 'Utenfor MVA-loven' },
] as const;
