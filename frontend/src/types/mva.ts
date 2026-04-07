// TypeScript-typer for MVA-modulen
// Matcher API DTOs fra spec-mva.md

// --- Enums (som const objekter for erasableSyntaxOnly) ---

export const MvaTerminType = {
  Tomaaneders: 'Tomaaneders',
  Arlig: 'Arlig',
} as const;
export type MvaTerminType = (typeof MvaTerminType)[keyof typeof MvaTerminType];

export const MvaTerminTypeNavn: Record<MvaTerminType, string> = {
  [MvaTerminType.Tomaaneders]: 'Tomånedersperiode',
  [MvaTerminType.Arlig]: 'Årstermin',
};

export const MvaTerminStatus = {
  Apen: 'Apen',
  Beregnet: 'Beregnet',
  Avstemt: 'Avstemt',
  Innsendt: 'Innsendt',
  Betalt: 'Betalt',
} as const;
export type MvaTerminStatus = (typeof MvaTerminStatus)[keyof typeof MvaTerminStatus];

export const MvaTerminStatusNavn: Record<MvaTerminStatus, string> = {
  [MvaTerminStatus.Apen]: 'Åpen',
  [MvaTerminStatus.Beregnet]: 'Beregnet',
  [MvaTerminStatus.Avstemt]: 'Avstemt',
  [MvaTerminStatus.Innsendt]: 'Innsendt',
  [MvaTerminStatus.Betalt]: 'Betalt',
};

// --- Termin DTOs ---

export interface MvaTerminDto {
  id: string;
  ar: number;
  termin: number;
  type: MvaTerminType;
  fraDato: string;
  tilDato: string;
  frist: string;
  status: MvaTerminStatus;
  terminnavn: string;
  avsluttetTidspunkt: string | null;
  avsluttetAv: string | null;
  oppgjorsBilagId: string | null;
  harOppgjor: boolean;
  erForfalt: boolean;
}

export interface GenererTerminerRequest {
  ar: number;
  type?: MvaTerminType;
}

// --- Oppgjor DTOs ---

export interface MvaOppgjorDto {
  id: string;
  mvaTerminId: string;
  terminnavn: string;
  beregnetTidspunkt: string;
  beregnetAv: string;
  sumUtgaendeMva: number;
  sumInngaendeMva: number;
  sumSnuddAvregningUtgaende: number;
  sumSnuddAvregningInngaende: number;
  mvaTilBetaling: number;
  erLast: boolean;
  linjer: MvaOppgjorLinjeDto[];
}

export interface MvaOppgjorLinjeDto {
  mvaKode: string;
  standardTaxCode: string;
  sats: number;
  retning: string;
  rfPostnummer: number;
  sumGrunnlag: number;
  sumMvaBelop: number;
  antallPosteringer: number;
}

// --- Melding DTOs (RF-0002) ---

export interface MvaMeldingDto {
  mvaTerminId: string;
  terminnavn: string;
  ar: number;
  termin: number;
  fraDato: string;
  tilDato: string;
  poster: MvaMeldingPostDto[];
  sumUtgaendeMva: number;
  sumInngaendeMva: number;
  mvaGrunnlagHoySats: number;
  mvaGrunnlagMiddelsSats: number;
  mvaGrunnlagLavSats: number;
  mvaTilBetaling: number;
}

export interface MvaMeldingPostDto {
  postnummer: number;
  beskrivelse: string;
  grunnlag: number;
  mvaBelop: number;
  standardTaxCodes: string[];
}

// --- Avstemming DTOs ---

export interface MvaAvstemmingDto {
  id: string;
  mvaTerminId: string;
  terminnavn: string;
  avstemmingTidspunkt: string;
  avstemmingAv: string;
  erGodkjent: boolean;
  merknad: string | null;
  harAvvik: boolean;
  totaltAvvik: number;
  linjer: MvaAvstemmingLinjeDto[];
}

export interface MvaAvstemmingLinjeDto {
  kontonummer: string;
  kontonavn: string;
  saldoIflgHovedbok: number;
  beregnetFraPosteringer: number;
  avvik: number;
  harAvvik: boolean;
}

// --- Sammenstilling DTOs ---

export interface MvaSammenstillingDto {
  ar: number;
  termin: number;
  fraDato: string;
  tilDato: string;
  grupper: MvaSammenstillingGruppeDto[];
  totaltMvaGrunnlag: number;
  totaltMvaBelop: number;
}

export interface MvaSammenstillingGruppeDto {
  mvaKode: string;
  beskrivelse: string;
  standardTaxCode: string;
  sats: number;
  retning: string;
  sumGrunnlag: number;
  sumMvaBelop: number;
  antallPosteringer: number;
}

export interface MvaSammenstillingDetaljDto {
  mvaKode: string;
  beskrivelse: string;
  posteringer: MvaPosteringDetaljDto[];
  sumGrunnlag: number;
  sumMvaBelop: number;
  totaltAntall: number;
}

export interface MvaPosteringDetaljDto {
  posteringId: string;
  bilagId: string;
  bilagsnummer: number;
  bilagsdato: string;
  kontonummer: string;
  beskrivelse: string;
  side: string;
  belop: number;
  mvaGrunnlag: number;
  mvaBelop: number;
  mvaSats: number;
  erAutoGenerertMva: boolean;
}

// --- Sammenstilling-parametere ---

export interface MvaSammenstillingParams {
  ar: number;
  termin?: number;
}

export interface MvaSammenstillingDetaljParams {
  ar: number;
  termin?: number;
  mvaKode: string;
}
