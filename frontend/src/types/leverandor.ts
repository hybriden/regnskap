// TypeScript-typer for Leverandorreskontro-modulen
// Matcher API DTOs fra spec-leverandor.md

// --- Enums (som const objekter for erasableSyntaxOnly) ---

export const FakturaStatus = {
  Registrert: 'Registrert',
  Godkjent: 'Godkjent',
  IBetalingsforslag: 'IBetalingsforslag',
  SendtTilBank: 'SendtTilBank',
  Betalt: 'Betalt',
  DelvisBetalt: 'DelvisBetalt',
  Kreditert: 'Kreditert',
  Sperret: 'Sperret',
} as const;
export type FakturaStatus = (typeof FakturaStatus)[keyof typeof FakturaStatus];

export const FakturaStatusNavn: Record<FakturaStatus, string> = {
  [FakturaStatus.Registrert]: 'Registrert',
  [FakturaStatus.Godkjent]: 'Godkjent',
  [FakturaStatus.IBetalingsforslag]: 'I betalingsforslag',
  [FakturaStatus.SendtTilBank]: 'Sendt til bank',
  [FakturaStatus.Betalt]: 'Betalt',
  [FakturaStatus.DelvisBetalt]: 'Delvis betalt',
  [FakturaStatus.Kreditert]: 'Kreditert',
  [FakturaStatus.Sperret]: 'Sperret',
};

export const BetalingsforslagStatus = {
  Utkast: 'Utkast',
  Godkjent: 'Godkjent',
  FilGenerert: 'FilGenerert',
  SendtTilBank: 'SendtTilBank',
  Utfort: 'Utfort',
  Avvist: 'Avvist',
  Kansellert: 'Kansellert',
} as const;
export type BetalingsforslagStatus =
  (typeof BetalingsforslagStatus)[keyof typeof BetalingsforslagStatus];

export const BetalingsforslagStatusNavn: Record<BetalingsforslagStatus, string> = {
  [BetalingsforslagStatus.Utkast]: 'Utkast',
  [BetalingsforslagStatus.Godkjent]: 'Godkjent',
  [BetalingsforslagStatus.FilGenerert]: 'Fil generert',
  [BetalingsforslagStatus.SendtTilBank]: 'Sendt til bank',
  [BetalingsforslagStatus.Utfort]: 'Utfort',
  [BetalingsforslagStatus.Avvist]: 'Avvist',
  [BetalingsforslagStatus.Kansellert]: 'Kansellert',
};

export const LeverandorTransaksjonstype = {
  Faktura: 'Faktura',
  Kreditnota: 'Kreditnota',
  Betaling: 'Betaling',
  Forskudd: 'Forskudd',
} as const;
export type LeverandorTransaksjonstype =
  (typeof LeverandorTransaksjonstype)[keyof typeof LeverandorTransaksjonstype];

export const Betalingsbetingelse = {
  Netto10: 'Netto10',
  Netto14: 'Netto14',
  Netto20: 'Netto20',
  Netto30: 'Netto30',
  Netto45: 'Netto45',
  Netto60: 'Netto60',
  Netto90: 'Netto90',
  Kontant: 'Kontant',
  Egendefinert: 'Egendefinert',
} as const;
export type Betalingsbetingelse = (typeof Betalingsbetingelse)[keyof typeof Betalingsbetingelse];

export const BetalingsbetingelseNavn: Record<Betalingsbetingelse, string> = {
  [Betalingsbetingelse.Netto10]: 'Netto 10 dager',
  [Betalingsbetingelse.Netto14]: 'Netto 14 dager',
  [Betalingsbetingelse.Netto20]: 'Netto 20 dager',
  [Betalingsbetingelse.Netto30]: 'Netto 30 dager',
  [Betalingsbetingelse.Netto45]: 'Netto 45 dager',
  [Betalingsbetingelse.Netto60]: 'Netto 60 dager',
  [Betalingsbetingelse.Netto90]: 'Netto 90 dager',
  [Betalingsbetingelse.Kontant]: 'Kontant',
  [Betalingsbetingelse.Egendefinert]: 'Egendefinert',
};

export const Alderskategori = {
  IkkeForfalt: 'IkkeForfalt',
  Dager0Til30: 'Dager0Til30',
  Dager31Til60: 'Dager31Til60',
  Dager61Til90: 'Dager61Til90',
  Over90Dager: 'Over90Dager',
} as const;
export type Alderskategori = (typeof Alderskategori)[keyof typeof Alderskategori];

// --- Leverandor DTOs ---

export interface LeverandorDto {
  id: string;
  leverandornummer: string;
  navn: string;
  organisasjonsnummer: string | null;
  erMvaRegistrert: boolean;
  adresse1: string | null;
  postnummer: string | null;
  poststed: string | null;
  landkode: string;
  kontaktperson: string | null;
  epost: string | null;
  betalingsbetingelse: Betalingsbetingelse;
  bankkontonummer: string | null;
  iban: string | null;
  erAktiv: boolean;
  saldo: number;
}

export interface LeverandorDetaljerDto extends LeverandorDto {
  adresse2: string | null;
  telefon: string | null;
  bic: string | null;
  banknavn: string | null;
  standardKontoId: string | null;
  standardMvaKode: string | null;
  valutakode: string;
  egendefinertBetalingsfrist: number | null;
  erSperret: boolean;
  notat: string | null;
}

export interface OpprettLeverandorRequest {
  leverandornummer: string;
  navn: string;
  organisasjonsnummer?: string | null;
  erMvaRegistrert: boolean;
  adresse1?: string | null;
  adresse2?: string | null;
  postnummer?: string | null;
  poststed?: string | null;
  landkode: string;
  kontaktperson?: string | null;
  telefon?: string | null;
  epost?: string | null;
  betalingsbetingelse: Betalingsbetingelse;
  egendefinertBetalingsfrist?: number | null;
  bankkontonummer?: string | null;
  iban?: string | null;
  bic?: string | null;
  standardKontoId?: string | null;
  standardMvaKode?: string | null;
}

export interface OppdaterLeverandorRequest extends OpprettLeverandorRequest {
  erAktiv: boolean;
  erSperret: boolean;
  notat?: string | null;
}

export interface LeverandorSokParams {
  q?: string;
  erAktiv?: boolean;
  side?: number;
  antall?: number;
}

export interface LeverandorSokResultat {
  items: LeverandorDto[];
  totalCount: number;
  side: number;
  antall: number;
  totalSider: number;
  harNesteSide: boolean;
}

// --- Faktura DTOs ---

export interface LeverandorFakturaLinjeDto {
  id: string;
  linjenummer: number;
  kontonummer: string;
  beskrivelse: string;
  belop: number;
  mvaKode: string | null;
  mvaSats: number | null;
  mvaBelop: number | null;
  avdelingskode: string | null;
  prosjektkode: string | null;
}

export interface LeverandorFakturaDto {
  id: string;
  leverandorId: string;
  leverandornummer: string;
  leverandornavn: string;
  eksternFakturanummer: string;
  internNummer: number;
  type: LeverandorTransaksjonstype;
  fakturadato: string;
  mottaksdato: string;
  forfallsdato: string;
  beskrivelse: string;
  belopEksMva: number;
  mvaBelop: number;
  belopInklMva: number;
  gjenstaendeBelop: number;
  status: FakturaStatus;
  kidNummer: string | null;
  valutakode: string;
  valutakurs: number | null;
  bilagId: string | null;
  erSperret: boolean;
  sperreArsak: string | null;
  linjer: LeverandorFakturaLinjeDto[];
}

export interface RegistrerFakturaRequest {
  leverandorId: string;
  eksternFakturanummer: string;
  type: LeverandorTransaksjonstype;
  fakturadato: string;
  forfallsdato?: string | null;
  beskrivelse: string;
  kidNummer?: string | null;
  valutakode: string;
  valutakurs?: number | null;
  linjer: FakturaLinjeRequest[];
}

export interface FakturaLinjeRequest {
  kontoId: string;
  beskrivelse: string;
  belop: number;
  mvaKode?: string | null;
  avdelingskode?: string | null;
  prosjektkode?: string | null;
}

export interface FakturaSokParams {
  leverandorId?: string;
  status?: FakturaStatus;
  fraDato?: string;
  tilDato?: string;
  forfaltFra?: string;
  forfaltTil?: string;
  side?: number;
  antall?: number;
}

export interface FakturaSokResultat {
  data: LeverandorFakturaDto[];
  totaltAntall: number;
  side: number;
  antall: number;
}

// --- Betaling DTOs ---

export interface LeverandorBetalingDto {
  id: string;
  leverandorFakturaId: string;
  betalingsforslagId: string | null;
  betalingsdato: string;
  belop: number;
  bilagId: string | null;
  bankreferanse: string | null;
  betalingsmetode: string;
}

// --- Betalingsforslag DTOs ---

export interface BetalingsforslagLinjeDto {
  id: string;
  betalingsforslagId: string;
  leverandorFakturaId: string;
  leverandorId: string;
  leverandornummer: string;
  leverandornavn: string;
  eksternFakturanummer: string;
  forfallsdato: string;
  belop: number;
  mottakerKontonummer: string | null;
  mottakerIban: string | null;
  kidNummer: string | null;
  erInkludert: boolean;
  erUtfort: boolean | null;
  feilmelding: string | null;
}

export interface BetalingsforslagDto {
  id: string;
  forslagsnummer: number;
  beskrivelse: string;
  opprettdato: string;
  betalingsdato: string;
  forfallTilOgMed: string;
  status: BetalingsforslagStatus;
  totalBelop: number;
  antallBetalinger: number;
  fraKontonummer: string | null;
  betalingsfilReferanse: string | null;
  filGenererTidspunkt: string | null;
  sendtTilBankTidspunkt: string | null;
  godkjentAv: string | null;
  godkjentTidspunkt: string | null;
  linjer: BetalingsforslagLinjeDto[];
}

export interface GenererBetalingsforslagRequest {
  forfallTilOgMed: string;
  betalingsdato: string;
  fraBankkontoId?: string | null;
  fraKontonummer?: string | null;
  inkluderAlleredeGodkjente: boolean;
  leverandorIder?: string[] | null;
}

// --- Rapport DTOs ---

export interface AldersfordelingLeverandorDto {
  leverandorId: string;
  leverandornummer: string;
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

export interface AldersfordelingDto {
  leverandorer: AldersfordelingLeverandorDto[];
  totalt: AldersfordelingSummaryDto;
  dato: string;
}

export interface LeverandorutskriftLinjeDto {
  dato: string;
  bilagsId: string;
  beskrivelse: string;
  type: LeverandorTransaksjonstype;
  debet: number | null;
  kredit: number | null;
  saldo: number;
  eksternFakturanummer: string | null;
}

export interface LeverandorutskriftDto {
  leverandorId: string;
  leverandornummer: string;
  navn: string;
  inngaaendeSaldo: number;
  transaksjoner: LeverandorutskriftLinjeDto[];
  utgaaendeSaldo: number;
  fraDato: string;
  tilDato: string;
}

// --- Apne poster ---

export interface ApnePostLeverandorDto {
  leverandorId: string;
  leverandornummer: string;
  leverandornavn: string;
  fakturaer: LeverandorFakturaDto[];
  sumGjenstaende: number;
}

export interface ApnePostRapportDto {
  dato: string;
  leverandorer: ApnePostLeverandorDto[];
  totalGjenstaende: number;
}
