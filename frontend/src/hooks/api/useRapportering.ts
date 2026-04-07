import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../api/client';
import type {
  ResultatregnskapDto,
  ResultatregnskapParams,
  BalanseDto,
  BalanseParams,
  KontantstromDto,
  KontantstromParams,
  SaldobalanseRapportDto,
  SaldobalanseRapportParams,
  HovedboksutskriftDto,
  HovedboksutskriftParams,
  SaftEksportRequest,
  NokkeltallDto,
  NokkeltallParams,
  SammenligningDto,
  SammenligningParams,
  BudsjettDto,
  OpprettBudsjettRequest,
  BudsjettBulkRequest,
} from '../../types/rapportering';

// --- Resultatregnskap ---

export function useResultatregnskap(params: ResultatregnskapParams) {
  return useQuery({
    queryKey: ['rapporter', 'resultatregnskap', params],
    queryFn: async () => {
      const { data } = await apiClient.get<ResultatregnskapDto>(
        '/rapporter/resultatregnskap',
        { params },
      );
      return data;
    },
    enabled: params.ar >= 2000 && params.ar <= 2099,
  });
}

// --- Balanse ---

export function useBalanse(params: BalanseParams) {
  return useQuery({
    queryKey: ['rapporter', 'balanse', params],
    queryFn: async () => {
      const { data } = await apiClient.get<BalanseDto>(
        '/rapporter/balanse',
        { params },
      );
      return data;
    },
    enabled: params.ar >= 2000 && params.ar <= 2099,
  });
}

// --- Kontantstrom ---

export function useKontantstrom(params: KontantstromParams) {
  return useQuery({
    queryKey: ['rapporter', 'kontantstrom', params],
    queryFn: async () => {
      const { data } = await apiClient.get<KontantstromDto>(
        '/rapporter/kontantstrom',
        { params },
      );
      return data;
    },
    enabled: params.ar >= 2000 && params.ar <= 2099,
  });
}

// --- Saldobalanse (utvidet) ---

export function useSaldobalanseRapport(params: SaldobalanseRapportParams) {
  return useQuery({
    queryKey: ['rapporter', 'saldobalanse', params],
    queryFn: async () => {
      const { data } = await apiClient.get<SaldobalanseRapportDto>(
        '/rapporter/saldobalanse',
        { params },
      );
      return data;
    },
    enabled: params.ar >= 2000 && params.ar <= 2099,
  });
}

// --- Hovedboksutskrift ---

export function useHovedboksutskrift(params: HovedboksutskriftParams) {
  return useQuery({
    queryKey: ['rapporter', 'hovedboksutskrift', params],
    queryFn: async () => {
      const { data } = await apiClient.get<HovedboksutskriftDto>(
        '/rapporter/hovedboksutskrift',
        { params },
      );
      return data;
    },
    enabled: params.ar >= 2000 && params.ar <= 2099,
  });
}

// --- SAF-T Eksport ---

export function useSaftEksport() {
  return useMutation({
    mutationFn: async (request: SaftEksportRequest) => {
      const response = await apiClient.post('/rapporter/saft', request, {
        responseType: 'blob',
      });
      // Trigger nedlasting av XML-fil
      const blob = new Blob([response.data as BlobPart], { type: 'application/xml' });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `SAF-T_${request.ar}_P${request.fraPeriode ?? 1}-${request.tilPeriode ?? 12}.xml`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    },
  });
}

// --- Sammenligning ---

export function useSammenligning(params: SammenligningParams) {
  return useQuery({
    queryKey: ['rapporter', 'sammenligning', params],
    queryFn: async () => {
      const { data } = await apiClient.get<SammenligningDto>(
        '/rapporter/sammenligning',
        { params },
      );
      return data;
    },
    enabled: params.ar >= 2000 && params.ar <= 2099,
  });
}

// --- Nokkeltall ---

export function useNokkeltall(params: NokkeltallParams) {
  return useQuery({
    queryKey: ['rapporter', 'nokkeltall', params],
    queryFn: async () => {
      const { data } = await apiClient.get<NokkeltallDto>(
        '/rapporter/nokkeltall',
        { params },
      );
      return data;
    },
    enabled: params.ar >= 2000 && params.ar <= 2099,
  });
}

// --- Budsjett ---

export function useBudsjett(ar: number, versjon: string = 'Opprinnelig') {
  return useQuery({
    queryKey: ['budsjett', ar, versjon],
    queryFn: async () => {
      const { data } = await apiClient.get<BudsjettDto[]>(
        '/budsjett',
        { params: { ar, versjon } },
      );
      return data;
    },
    enabled: ar >= 2000 && ar <= 2099,
  });
}

export function useOpprettBudsjett() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OpprettBudsjettRequest) => {
      const { data } = await apiClient.post<BudsjettDto>('/budsjett', request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['budsjett'] });
    },
  });
}

export function useBudsjettBulkImport() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: BudsjettBulkRequest) => {
      const { data } = await apiClient.post<BudsjettDto[]>('/budsjett/bulk', request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['budsjett'] });
    },
  });
}

export function useSlettBudsjett() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ ar, versjon }: { ar: number; versjon: string }) => {
      await apiClient.delete(`/budsjett`, { params: { ar, versjon } });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['budsjett'] });
    },
  });
}
