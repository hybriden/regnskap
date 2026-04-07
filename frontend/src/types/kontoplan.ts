// TypeScript-typer for Kontoplan-modulen
// Matcher API DTOs fra spec-kontoplan.md

// --- Enums (som const objekter for erasableSyntaxOnly) ---

export const Kontoklasse = {
  Eiendeler: 1,
  EgenkapitalOgGjeld: 2,
  Salgsinntekt: 3,
  Varekostnad: 4,
  Lonnskostnad: 5,
  AvskrivningOgAnnenDriftskostnad: 6,
  AnnenDriftskostnad: 7,
  FinansposterSkatt: 8,
} as const;
export type Kontoklasse = (typeof Kontoklasse)[keyof typeof Kontoklasse];

export const KontoklasseNavn: Record<Kontoklasse, string> = {
  [Kontoklasse.Eiendeler]: 'Eiendeler',
  [Kontoklasse.EgenkapitalOgGjeld]: 'Egenkapital og gjeld',
  [Kontoklasse.Salgsinntekt]: 'Salgsinntekt',
  [Kontoklasse.Varekostnad]: 'Varekostnad',
  [Kontoklasse.Lonnskostnad]: 'Lonnskostnad',
  [Kontoklasse.AvskrivningOgAnnenDriftskostnad]: 'Avskrivning og annen driftskostnad',
  [Kontoklasse.AnnenDriftskostnad]: 'Annen driftskostnad',
  [Kontoklasse.FinansposterSkatt]: 'Finansposter og skatt',
};

export const Kontotype = {
  Eiendel: 'Eiendel',
  Gjeld: 'Gjeld',
  Egenkapital: 'Egenkapital',
  Inntekt: 'Inntekt',
  Kostnad: 'Kostnad',
} as const;
export type Kontotype = (typeof Kontotype)[keyof typeof Kontotype];

export const Normalbalanse = {
  Debet: 'Debet',
  Kredit: 'Kredit',
} as const;
export type Normalbalanse = (typeof Normalbalanse)[keyof typeof Normalbalanse];

export const GrupperingsKategori = {
  RF1167: 'RF1167',
  RF1175: 'RF1175',
  RF1323: 'RF1323',
} as const;
export type GrupperingsKategori = (typeof GrupperingsKategori)[keyof typeof GrupperingsKategori];

export const MvaRetning = {
  Ingen: 'Ingen',
  Inngaende: 'Inngaende',
  Utgaende: 'Utgaende',
  SnuddAvregning: 'SnuddAvregning',
} as const;
export type MvaRetning = (typeof MvaRetning)[keyof typeof MvaRetning];

// --- Kontogruppe DTOs ---

export interface KontogruppeListeDto {
  id: string;
  gruppekode: number;
  navn: string;
  navnEn: string | null;
  kontoklasse: number;
  kontoklasseNavn: string;
  kontotype: Kontotype;
  normalbalanse: Normalbalanse;
  erSystemgruppe: boolean;
  antallKontoer: number;
}

export interface KontogruppeDetaljerDto {
  id: string;
  gruppekode: number;
  navn: string;
  kontoer: KontogruppeKontoDto[];
}

export interface KontogruppeKontoDto {
  id: string;
  kontonummer: string;
  navn: string;
  kontotype: Kontotype;
  erAktiv: boolean;
  standardMvaKode: string | null;
}

// --- Konto DTOs ---

export interface KontoListeDto {
  id: string;
  kontonummer: string;
  navn: string;
  navnEn: string | null;
  kontotype: Kontotype;
  normalbalanse: Normalbalanse;
  kontoklasse: number;
  gruppekode: number;
  gruppeNavn: string;
  standardAccountId: string;
  grupperingsKategori: GrupperingsKategori | null;
  grupperingsKode: string | null;
  erAktiv: boolean;
  erSystemkonto: boolean;
  erBokforbar: boolean;
  standardMvaKode: string | null;
  kreverAvdeling: boolean;
  kreverProsjekt: boolean;
  harUnderkontoer: boolean;
  overordnetKontonummer: string | null;
}

export interface KontoDetaljerDto {
  id: string;
  kontonummer: string;
  navn: string;
  navnEn: string | null;
  kontotype: Kontotype;
  normalbalanse: Normalbalanse;
  kontoklasse: number;
  gruppekode: number;
  gruppeNavn: string;
  standardAccountId: string;
  grupperingsKategori: GrupperingsKategori | null;
  grupperingsKode: string | null;
  erAktiv: boolean;
  erSystemkonto: boolean;
  erBokforbar: boolean;
  standardMvaKode: string | null;
  beskrivelse: string | null;
  kreverAvdeling: boolean;
  kreverProsjekt: boolean;
  underkontoer: UnderkontoDto[];
}

export interface UnderkontoDto {
  kontonummer: string;
  navn: string;
  erAktiv: boolean;
}

export interface OpprettKontoRequest {
  kontonummer: string;
  navn: string;
  navnEn?: string | null;
  kontotype: Kontotype;
  gruppekode: number;
  standardAccountId: string;
  grupperingsKategori?: GrupperingsKategori | null;
  grupperingsKode?: string | null;
  erBokforbar: boolean;
  standardMvaKode?: string | null;
  beskrivelse?: string | null;
  overordnetKontonummer?: string | null;
  kreverAvdeling: boolean;
  kreverProsjekt: boolean;
}

export interface OppdaterKontoRequest {
  navn: string;
  navnEn?: string | null;
  erAktiv: boolean;
  erBokforbar: boolean;
  standardMvaKode?: string | null;
  beskrivelse?: string | null;
  grupperingsKategori?: GrupperingsKategori | null;
  grupperingsKode?: string | null;
  kreverAvdeling: boolean;
  kreverProsjekt: boolean;
}

export interface OpprettKontoResponse {
  id: string;
  kontonummer: string;
  navn: string;
}

// --- Konto-oppslag ---

export interface KontoOppslagDto {
  kontonummer: string;
  navn: string;
  kontotype: Kontotype;
  standardMvaKode: string | null;
}

// --- MVA-kode DTOs ---

export interface MvaKodeDto {
  id: string;
  kode: string;
  beskrivelse: string;
  beskrivelseEn: string | null;
  standardTaxCode: string;
  sats: number;
  retning: MvaRetning;
  utgaendeKontonummer: string | null;
  inngaendeKontonummer: string | null;
  erAktiv: boolean;
  erSystemkode: boolean;
}

export interface OpprettMvaKodeRequest {
  kode: string;
  beskrivelse: string;
  standardTaxCode: string;
  sats: number;
  retning: MvaRetning;
  inngaendeKontonummer?: string | null;
  utgaendeKontonummer?: string | null;
}

export interface OppdaterMvaKodeRequest {
  beskrivelse: string;
  beskrivelseEn?: string | null;
  sats: number;
  retning: MvaRetning;
  inngaendeKontonummer?: string | null;
  utgaendeKontonummer?: string | null;
  erAktiv: boolean;
}

// --- Import/Eksport ---

export type ImportModus = 'opprett' | 'oppdater' | 'erstatt';
export type ImportFormat = 'csv' | 'json';
export type EksportFormat = 'csv' | 'json' | 'saft';

export interface ImportResultatDto {
  opprettet: number;
  oppdatert: number;
  hoppetOver: number;
  feil: ImportFeilDto[];
}

export interface ImportFeilDto {
  linje: number;
  kontonummer: string;
  melding: string;
}

// --- Paginering ---

export interface PaginertResultat<T> {
  data: T[];
  side: number;
  antall: number;
  totaltAntall: number;
}

export interface KontoSokParams {
  kontoklasse?: number;
  kontotype?: Kontotype;
  gruppekode?: number;
  erAktiv?: boolean;
  erBokforbar?: boolean;
  sok?: string;
  side?: number;
  antall?: number;
}

export interface MvaKodeSokParams {
  erAktiv?: boolean;
  retning?: MvaRetning;
}
