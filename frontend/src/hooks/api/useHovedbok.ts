import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../api/client';
import type {
  PerioderResponse,
  OpprettArRequest,
  EndreStatusRequest,
  KontoutskriftResponse,
  KontoutskriftParams,
  SaldobalanseResponse,
  SaldobalanseParams,
  KontoSaldoResponse,
  BilagDto,
  OpprettBilagRequest,
  BilagSokParams,
  PeriodeDto,
} from '../../types/hovedbok';
import type { PaginertResultat } from '../../types/kontoplan';

// --- Regnskapsperioder ---

export function usePerioder(ar: number) {
  return useQuery({
    queryKey: ['perioder', ar],
    queryFn: async () => {
      const { data } = await apiClient.get<PerioderResponse>(`/perioder/${ar}`);
      return data;
    },
    enabled: ar >= 2000 && ar <= 2099,
  });
}

export function usePeriode(ar: number, periode: number) {
  const { data: perioderResponse, ...rest } = usePerioder(ar);
  const periodeData = perioderResponse?.perioder.find((p) => p.periode === periode);
  return { data: periodeData as PeriodeDto | undefined, ...rest };
}

export function useOpprettAr() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OpprettArRequest) => {
      const { data } = await apiClient.post<PerioderResponse>('/perioder/opprett-ar', request);
      return data;
    },
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['perioder', variables.ar] });
    },
  });
}

export function useEndreStatus(ar: number, periode: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: EndreStatusRequest) => {
      const { data } = await apiClient.put(`/perioder/${ar}/${periode}/status`, request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['perioder', ar] });
      void queryClient.invalidateQueries({ queryKey: ['saldobalanse'] });
    },
  });
}

// --- Bilag ---

export function useBilag(id: string) {
  return useQuery({
    queryKey: ['bilag', id],
    queryFn: async () => {
      const { data } = await apiClient.get<BilagDto>(`/bilag/${id}`);
      return data;
    },
    enabled: id.length > 0,
  });
}

export function useBilagListe(params?: BilagSokParams) {
  return useQuery({
    queryKey: ['bilag', 'liste', params],
    queryFn: async () => {
      const { data } = await apiClient.get<PaginertResultat<BilagDto>>('/bilag', { params });
      return data;
    },
  });
}

export function useOpprettBilag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OpprettBilagRequest) => {
      const { data } = await apiClient.post<BilagDto>('/bilag', request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['bilag'] });
      void queryClient.invalidateQueries({ queryKey: ['perioder'] });
      void queryClient.invalidateQueries({ queryKey: ['saldobalanse'] });
      void queryClient.invalidateQueries({ queryKey: ['kontoutskrift'] });
      void queryClient.invalidateQueries({ queryKey: ['kontoSaldo'] });
    },
  });
}

// --- Kontoutskrift ---

export function useKontoutskrift(kontonummer: string, params?: KontoutskriftParams) {
  return useQuery({
    queryKey: ['kontoutskrift', kontonummer, params],
    queryFn: async () => {
      const { data } = await apiClient.get<KontoutskriftResponse>(
        `/kontoutskrift/${kontonummer}`,
        { params },
      );
      return data;
    },
    enabled: kontonummer.length >= 4,
  });
}

// --- Saldobalanse ---

export function useSaldobalanse(ar: number, periode: number, params?: SaldobalanseParams) {
  return useQuery({
    queryKey: ['saldobalanse', ar, periode, params],
    queryFn: async () => {
      const { data } = await apiClient.get<SaldobalanseResponse>(
        `/saldobalanse/${ar}/${periode}`,
        { params },
      );
      return data;
    },
    enabled: ar >= 2000 && ar <= 2099 && periode >= 0 && periode <= 13,
  });
}

// --- Saldooppslag ---

export function useKontoSaldo(kontonummer: string, ar: number, fraPeriode?: number, tilPeriode?: number) {
  return useQuery({
    queryKey: ['kontoSaldo', kontonummer, ar, fraPeriode, tilPeriode],
    queryFn: async () => {
      const { data } = await apiClient.get<KontoSaldoResponse>(
        `/saldo/${kontonummer}`,
        { params: { ar, fraPeriode, tilPeriode } },
      );
      return data;
    },
    enabled: kontonummer.length >= 4 && ar >= 2000,
  });
}
