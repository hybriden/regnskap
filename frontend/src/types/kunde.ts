// TypeScript-typer for Kundereskontro-modulen
// Matcher API DTOs fra spec-kunde.md

// --- Enums (som const objekter for erasableSyntaxOnly) ---

export const KundeFakturaStatus = {
  Utstedt: 'Utstedt',
  Betalt: 'Betalt',
  DelvisBetalt: 'DelvisBetalt',
  Kreditert: 'Kreditert',
  Purring1: 'Purring1',
  Purring2: 'Purring2',
  Purring3: 'Purring3',
  Inkasso: 'Inkasso',
  Tap: 'Tap',
} as const;
export type KundeFakturaStatus = (typeof KundeFakturaStatus)[keyof typeof KundeFakturaStatus];

export const KundeFakturaStatusNavn: Record<KundeFakturaStatus, string> = {
  [KundeFakturaStatus.Utstedt]: 'Utstedt',
  [KundeFakturaStatus.Betalt]: 'Betalt',
  [KundeFakturaStatus.DelvisBetalt]: 'Delvis betalt',
  [KundeFakturaStatus.Kreditert]: 'Kreditert',
  [KundeFakturaStatus.Purring1]: '1. purring',
  [KundeFakturaStatus.Purring2]: '2. purring',
  [KundeFakturaStatus.Purring3]: '3. purring',
  [KundeFakturaStatus.Inkasso]: 'Inkasso',
  [KundeFakturaStatus.Tap]: 'Tap',
};

export const KundeTransaksjonstype = {
  Faktura: 'Faktura',
  Kreditnota: 'Kreditnota',
  Innbetaling: 'Innbetaling',
  Purregebyr: 'Purregebyr',
  Tap: 'Tap',
} as const;
export type KundeTransaksjonstype =
  (typeof KundeTransaksjonstype)[keyof typeof KundeTransaksjonstype];

export const KundeTransaksjonTypeNavn: Record<KundeTransaksjonstype, string> = {
  [KundeTransaksjonstype.Faktura]: 'Faktura',
  [KundeTransaksjonstype.Kreditnota]: 'Kreditnota',
  [KundeTransaksjonstype.Innbetaling]: 'Innbetaling',
  [KundeTransaksjonstype.Purregebyr]: 'Purregebyr',
  [KundeTransaksjonstype.Tap]: 'Tap',
};

export const KundeBetalingsbetingelse = {
  Netto10: 'Netto10',
  Netto14: 'Netto14',
  Netto20: 'Netto20',
  Netto30: 'Netto30',
  Netto45: 'Netto45',
  Netto60: 'Netto60',
  Kontant: 'Kontant',
  Forskudd: 'Forskudd',
  Egendefinert: 'Egendefinert',
} as const;
export type KundeBetalingsbetingelse =
  (typeof KundeBetalingsbetingelse)[keyof typeof KundeBetalingsbetingelse];

export const KundeBetalingsbetingelseNavn: Record<KundeBetalingsbetingelse, string> = {
  [KundeBetalingsbetingelse.Netto10]: 'Netto 10 dager',
  [KundeBetalingsbetingelse.Netto14]: 'Netto 14 dager',
  [KundeBetalingsbetingelse.Netto20]: 'Netto 20 dager',
  [KundeBetalingsbetingelse.Netto30]: 'Netto 30 dager',
  [KundeBetalingsbetingelse.Netto45]: 'Netto 45 dager',
  [KundeBetalingsbetingelse.Netto60]: 'Netto 60 dager',
  [KundeBetalingsbetingelse.Kontant]: 'Kontant',
  [KundeBetalingsbetingelse.Forskudd]: 'Forskudd',
  [KundeBetalingsbetingelse.Egendefinert]: 'Egendefinert',
};

export const KidAlgoritme = {
  MOD10: 'MOD10',
  MOD11: 'MOD11',
} as const;
export type KidAlgoritme = (typeof KidAlgoritme)[keyof typeof KidAlgoritme];

export const PurringType = {
  Purring1: 'Purring1',
  Purring2: 'Purring2',
  Purring3Inkassovarsel: 'Purring3Inkassovarsel',
} as const;
export type PurringType = (typeof PurringType)[keyof typeof PurringType];

export const PurringTypeNavn: Record<PurringType, string> = {
  [PurringType.Purring1]: '1. purring',
  [PurringType.Purring2]: '2. purring',
  [PurringType.Purring3Inkassovarsel]: '3. purring / inkassovarsel',
};

export const Alderskategori = {
  IkkeForfalt: 'IkkeForfalt',
  Dager0Til30: 'Dager0Til30',
  Dager31Til60: 'Dager31Til60',
  Dager61Til90: 'Dager61Til90',
  Over90Dager: 'Over90Dager',
} as const;
export type Alderskategori = (typeof Alderskategori)[keyof typeof Alderskategori];

// --- Kunde DTOs ---

export interface KundeDto {
  id: string;
  kundenummer: string;
  navn: string;
  organisasjonsnummer: string | null;
  fodselsnummer: string | null;
  erBedrift: boolean;
  adresse1: string | null;
  adresse2: string | null;
  postnummer: string | null;
  poststed: string | null;
  landkode: string;
  kontaktperson: string | null;
  telefon: string | null;
  epost: string | null;
  betalingsbetingelse: KundeBetalingsbetingelse;
  egendefinertBetalingsfrist: number | null;
  kidAlgoritme: KidAlgoritme | null;
  standardKontoId: string | null;
  standardMvaKode: string | null;
  valutakode: string;
  kredittgrense: number;
  erAktiv: boolean;
  erSperret: boolean;
  notat: string | null;
  peppolId: string | null;
  kanMottaEhf: boolean;
}

export interface OpprettKundeRequest {
  kundenummer: string;
  navn: string;
  erBedrift: boolean;
  organisasjonsnummer?: string | null;
  fodselsnummer?: string | null;
  adresse1?: string | null;
  adresse2?: string | null;
  postnummer?: string | null;
  poststed?: string | null;
  landkode: string;
  kontaktperson?: string | null;
  telefon?: string | null;
  epost?: string | null;
  betalingsbetingelse: KundeBetalingsbetingelse;
  egendefinertBetalingsfrist?: number | null;
  standardKontoId?: string | null;
  standardMvaKode?: string | null;
  kredittgrense?: number | null;
  peppolId?: string | null;
  kanMottaEhf: boolean;
}

export interface OppdaterKundeRequest extends OpprettKundeRequest {}

// --- KundeFaktura DTOs ---

export interface KundeFakturaDto {
  id: string;
  kundeId: string;
  kundenummer: string;
  kundenavn: string;
  fakturanummer: number;
  type: KundeTransaksjonstype;
  fakturadato: string;
  forfallsdato: string;
  leveringsdato: string | null;
  beskrivelse: string;
  belopEksMva: number;
  mvaBelop: number;
  belopInklMva: number;
  gjenstaendeBelop: number;
  status: KundeFakturaStatus;
  kidNummer: string | null;
  valutakode: string;
  valutakurs: number | null;
  bilagId: string | null;
  kreditnotaForFakturaId: string | null;
  eksternReferanse: string | null;
  bestillingsnummer: string | null;
  antallPurringer: number;
  sistePurringDato: string | null;
  purregebyrTotalt: number;
  erForfalt: boolean;
  dagerForfalt: number;
  linjer: KundeFakturaLinjeDto[];
  innbetalinger: KundeInnbetalingDto[];
}

export interface KundeFakturaLinjeDto {
  id: string;
  linjenummer: number;
  kontoId: string;
  kontonummer: string;
  beskrivelse: string;
  antall: number;
  enhetspris: number;
  belop: number;
  mvaKode: string | null;
  mvaSats: number | null;
  mvaBelop: number | null;
  rabatt: number;
  avdelingskode: string | null;
  prosjektkode: string | null;
}

export interface RegistrerKundeFakturaRequest {
  kundeId: string;
  type: KundeTransaksjonstype;
  fakturadato: string;
  forfallsdato?: string | null;
  leveringsdato?: string | null;
  beskrivelse: string;
  eksternReferanse?: string | null;
  bestillingsnummer?: string | null;
  valutakode: string;
  valutakurs?: number | null;
  linjer: KundeFakturaLinjeRequest[];
}

export interface KundeFakturaLinjeRequest {
  kontoId: string;
  beskrivelse: string;
  antall: number;
  enhetspris: number;
  mvaKode?: string | null;
  rabatt: number;
  avdelingskode?: string | null;
  prosjektkode?: string | null;
}

// --- Innbetaling DTOs ---

export interface KundeInnbetalingDto {
  id: string;
  kundeFakturaId: string;
  innbetalingsdato: string;
  belop: number;
  bilagId: string | null;
  bankreferanse: string | null;
  kidNummer: string | null;
  erAutoMatchet: boolean;
  betalingsmetode: string;
}

export interface RegistrerInnbetalingRequest {
  kundeFakturaId: string;
  innbetalingsdato: string;
  belop: number;
  bankreferanse?: string | null;
  kidNummer?: string | null;
  betalingsmetode: string;
}

export interface MatchKidRequest {
  kidNummer: string;
  belop: number;
  innbetalingsdato: string;
  bankreferanse?: string | null;
}

export interface UmatchetInnbetalingDto {
  bankreferanse: string;
  belop: number;
  dato: string;
  kidNummer: string | null;
  status: string;
}

// --- Purring DTOs ---

export interface PurringDto {
  id: string;
  kundeFakturaId: string;
  fakturanummer: number;
  kundenavn: string;
  kundenummer: string;
  type: PurringType;
  purringsdato: string;
  nyForfallsdato: string;
  gebyr: number;
  forsinkelsesrente: number;
  gebyrBilagId: string | null;
  erSendt: boolean;
  sendtTidspunkt: string | null;
  sendemetode: string | null;
}

export interface PurreforslagDto {
  fakturaId: string;
  fakturanummer: number;
  kundeId: string;
  kundenummer: string;
  kundenavn: string;
  fakturadato: string;
  forfallsdato: string;
  belopInklMva: number;
  gjenstaendeBelop: number;
  dagerForfalt: number;
  antallPurringer: number;
  foreslattType: PurringType;
  gebyr: number;
  forsinkelsesrente: number;
}

export interface PurreforslagRequest {
  dato: string;
  minimumDagerForfalt: number;
  inkluderPurring1: boolean;
  inkluderPurring2: boolean;
  inkluderPurring3: boolean;
  kundeIder?: string[] | null;
}

export interface OpprettPurringerRequest {
  fakturaIder: string[];
  type: PurringType;
}

// --- Rapport DTOs ---

export interface KundeAldersfordelingDto {
  kunder: AldersfordelingKundeDto[];
  totalt: AldersfordelingSummaryDto;
  dato: string;
}

export interface AldersfordelingKundeDto {
  kundeId: string;
  kundenummer: string;
  navn: string;
  ikkeForfalt: number;
  dager0Til30: number;
  dager31Til60: number;
  dager61Til90: number;
  over90Dager: number;
  totalt: number;
}

export interface AldersfordelingSummaryDto {
  ikkeForfalt: number;
  dager0Til30: number;
  dager31Til60: number;
  dager61Til90: number;
  over90Dager: number;
  totalt: number;
}

export interface KundeutskriftDto {
  kundeId: string;
  kundenummer: string;
  navn: string;
  inngaaendeSaldo: number;
  transaksjoner: KundeutskriftLinjeDto[];
  utgaaendeSaldo: number;
  fraDato: string;
  tilDato: string;
}

export interface KundeutskriftLinjeDto {
  dato: string;
  bilagsId: string;
  beskrivelse: string;
  type: KundeTransaksjonstype;
  debet: number | null;
  kredit: number | null;
  saldo: number;
  fakturanummer: number | null;
  kidNummer: string | null;
}

export interface KundeSaldoDto {
  kundeId: string;
  kundenummer: string;
  navn: string;
  saldo: number;
  antallApnePoster: number;
}

// --- CAMT.053 Import ---

export interface Camt053ImportResultDto {
  totaltAntall: number;
  autoMatchet: number;
  manuellBehandling: number;
  feilet: number;
  transaksjoner: Camt053TransaksjonDto[];
}

export interface Camt053TransaksjonDto {
  bankreferanse: string;
  belop: number;
  dato: string;
  kidNummer: string | null;
  matchetFakturaId: string | null;
  matchetKundenavn: string | null;
  status: string;
}

// --- Paginering ---

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
