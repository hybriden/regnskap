// TypeScript-typer for Bankavstemming-modulen
// Matcher API DTOs fra spec-bank.md
// TODO: Synk med backend

// --- Enums (som const objekter for erasableSyntaxOnly) ---

export const BankbevegelseStatus = {
  IkkeMatchet: 'IkkeMatchet',
  AutoMatchet: 'AutoMatchet',
  ManueltMatchet: 'ManueltMatchet',
  Splittet: 'Splittet',
  Bokfort: 'Bokfort',
  Ignorert: 'Ignorert',
} as const;
export type BankbevegelseStatus =
  (typeof BankbevegelseStatus)[keyof typeof BankbevegelseStatus];

export const BankbevegelseStatusNavn: Record<BankbevegelseStatus, string> = {
  [BankbevegelseStatus.IkkeMatchet]: 'Ikke matchet',
  [BankbevegelseStatus.AutoMatchet]: 'Auto-matchet',
  [BankbevegelseStatus.ManueltMatchet]: 'Manuelt matchet',
  [BankbevegelseStatus.Splittet]: 'Splittet',
  [BankbevegelseStatus.Bokfort]: 'Bokført',
  [BankbevegelseStatus.Ignorert]: 'Ignorert',
};

export const BankbevegelseRetning = {
  Inn: 'Inn',
  Ut: 'Ut',
} as const;
export type BankbevegelseRetning =
  (typeof BankbevegelseRetning)[keyof typeof BankbevegelseRetning];

export const BankbevegelseRetningNavn: Record<BankbevegelseRetning, string> = {
  [BankbevegelseRetning.Inn]: 'Innbetaling',
  [BankbevegelseRetning.Ut]: 'Utbetaling',
};

export const MatcheType = {
  Kid: 'Kid',
  Belop: 'Belop',
  Referanse: 'Referanse',
  Manuell: 'Manuell',
  Splitt: 'Splitt',
} as const;
export type MatcheType = (typeof MatcheType)[keyof typeof MatcheType];

export const MatcheTypeNavn: Record<MatcheType, string> = {
  [MatcheType.Kid]: 'KID-nummer',
  [MatcheType.Belop]: 'Beløp',
  [MatcheType.Referanse]: 'Referanse',
  [MatcheType.Manuell]: 'Manuell',
  [MatcheType.Splitt]: 'Splitt',
};

export const KontoutskriftStatus = {
  Importert: 'Importert',
  Ferdig: 'Ferdig',
  DelvisBehandlet: 'DelvisBehandlet',
} as const;
export type KontoutskriftStatus =
  (typeof KontoutskriftStatus)[keyof typeof KontoutskriftStatus];

export const KontoutskriftStatusNavn: Record<KontoutskriftStatus, string> = {
  [KontoutskriftStatus.Importert]: 'Importert',
  [KontoutskriftStatus.Ferdig]: 'Ferdig',
  [KontoutskriftStatus.DelvisBehandlet]: 'Delvis behandlet',
};

export const AvstemmingStatus = {
  UnderArbeid: 'UnderArbeid',
  Avstemt: 'Avstemt',
  AvstemtMedDifferanse: 'AvstemtMedDifferanse',
} as const;
export type AvstemmingStatus =
  (typeof AvstemmingStatus)[keyof typeof AvstemmingStatus];

export const AvstemmingStatusNavn: Record<AvstemmingStatus, string> = {
  [AvstemmingStatus.UnderArbeid]: 'Under arbeid',
  [AvstemmingStatus.Avstemt]: 'Avstemt',
  [AvstemmingStatus.AvstemtMedDifferanse]: 'Avstemt med differanse',
};

// --- Bankkonto DTOs ---

export interface BankkontoDto {
  id: string;
  kontonummer: string;
  iban: string | null;
  bic: string | null;
  banknavn: string;
  beskrivelse: string;
  valutakode: string;
  hovedbokkkontoId: string;
  hovedbokkontonummer: string;
  erAktiv: boolean;
  erStandardUtbetaling: boolean;
  erStandardInnbetaling: boolean;
}

export interface OpprettBankkontoRequest {
  kontonummer: string;
  iban?: string;
  bic?: string;
  banknavn: string;
  beskrivelse: string;
  valutakode?: string;
  hovedbokkkontoId: string;
  erStandardUtbetaling?: boolean;
  erStandardInnbetaling?: boolean;
}

// --- Kontoutskrift DTOs ---

export interface KontoutskriftDto {
  id: string;
  bankkontoId: string;
  meldingsId: string;
  utskriftId: string;
  sekvensnummer: string | null;
  opprettetAvBank: string;
  periodeFra: string;
  periodeTil: string;
  inngaendeSaldo: number;
  utgaendeSaldo: number;
  antallBevegelser: number;
  sumInn: number;
  sumUt: number;
  status: KontoutskriftStatus;
}

export interface ImportKontoutskriftResponse {
  kontoutskriftId: string;
  meldingsId: string;
  periodeFra: string;
  periodeTil: string;
  inngaendeSaldo: number;
  utgaendeSaldo: number;
  antallBevegelser: number;
  antallAutoMatchet: number;
  antallIkkeMatchet: number;
}

// --- Bankbevegelse DTOs ---

export interface BankbevegelseDto {
  id: string;
  bokforingsdato: string;
  valuteringsdato: string | null;
  retning: BankbevegelseRetning;
  belop: number;
  kidNummer: string | null;
  motpart: string | null;
  beskrivelse: string | null;
  status: BankbevegelseStatus;
  matcheType: MatcheType | null;
  matcheKonfidens: number | null;
  matchinger: BankbevegelseMatchDto[];
}

export interface BankbevegelseMatchDto {
  id: string;
  belop: number;
  matcheType: MatcheType;
  beskrivelse: string | null;
  kundeFakturaId: string | null;
  kundeFakturaNummer: string | null;
  leverandorFakturaId: string | null;
  leverandorFakturaNummer: string | null;
  bilagId: string | null;
  bilagNummer: string | null;
}

// --- Matching requests ---

export interface ManuellMatchRequest {
  kundeFakturaId?: string;
  leverandorFakturaId?: string;
  bilagId?: string;
  beskrivelse?: string;
}

export interface SplittMatchRequest {
  linjer: SplittLinjeRequest[];
}

export interface SplittLinjeRequest {
  belop: number;
  kundeFakturaId?: string;
  leverandorFakturaId?: string;
  bilagId?: string;
  beskrivelse?: string;
}

export interface BokforBankbevegelseRequest {
  motkontoId: string;
  mvaKode?: string;
  beskrivelse: string;
  avdelingskode?: string;
  prosjektkode?: string;
}

// --- Match-forslag ---

export interface MatcheForslagDto {
  matcheType: MatcheType;
  konfidens: number;
  beskrivelse: string;
  kundeFakturaId: string | null;
  kundeFakturaNummer: string | null;
  kundeFakturaGjenstaende: number | null;
  leverandorFakturaId: string | null;
  leverandorFakturaNummer: string | null;
  leverandorFakturaGjenstaende: number | null;
  bilagId: string | null;
  bilagNummer: string | null;
}

// --- Avstemming DTOs ---

export interface AvstemmingDto {
  id: string;
  bankkontoId: string;
  bankkontonummer: string;
  avstemmingsdato: string;
  saldoHovedbok: number;
  saldoBank: number;
  differanse: number;
  utestaaendeBetalinger: number;
  innbetalingerITransitt: number;
  andreDifferanser: number;
  uforklartDifferanse: number;
  status: AvstemmingStatus;
  godkjentAv: string | null;
  godkjentTidspunkt: string | null;
  antallMatchedeBevegelser: number;
  antallUmatchedeBevegelser: number;
  umatchedeBevegelser: BankbevegelseDto[];
}

export interface OppdaterAvstemmingRequest {
  utestaaendeBetalinger: number;
  innbetalingerITransitt: number;
  andreDifferanser: number;
  differanseForklaring?: string;
}

// --- Avstemmingsrapport ---

export interface AvstemmingsrapportDto {
  bankkontonummer: string;
  banknavn: string;
  hovedbokkontonummer: string;
  avstemmingsdato: string;
  saldoHovedbok: number;
  saldoBank: number;
  differanse: number;
  utestaaendeBetalinger: AvstemmingspostDto[];
  innbetalingerITransitt: AvstemmingspostDto[];
  andrePoster: AvstemmingspostDto[];
  sumTidsavgrensninger: number;
  uforklartDifferanse: number;
  status: AvstemmingStatus;
}

export interface AvstemmingspostDto {
  dato: string;
  beskrivelse: string;
  belop: number;
  referanse: string | null;
}

// --- Filtreringsparametere ---

export interface BankbevegelserParams {
  bankkontoId: string;
  fraDato?: string;
  tilDato?: string;
  status?: BankbevegelseStatus;
  retning?: BankbevegelseRetning;
}
