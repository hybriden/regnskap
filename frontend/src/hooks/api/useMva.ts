import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../api/client';
import type {
  MvaTerminDto,
  GenererTerminerRequest,
  MvaOppgjorDto,
  MvaMeldingDto,
  MvaAvstemmingDto,
  MvaSammenstillingDto,
  MvaSammenstillingDetaljDto,
  MvaSammenstillingParams,
  MvaSammenstillingDetaljParams,
} from '../../types/mva';

// --- MVA-terminer ---

export function useMvaTerminer(ar: number) {
  return useQuery({
    queryKey: ['mvaTerminer', ar],
    queryFn: async () => {
      const { data } = await apiClient.get<MvaTerminDto[]>('/mva/terminer', {
        params: { ar },
      });
      return data;
    },
    enabled: ar > 0,
  });
}

export function useMvaTermin(id: string) {
  return useQuery({
    queryKey: ['mvaTerminer', 'detalj', id],
    queryFn: async () => {
      const { data } = await apiClient.get<MvaTerminDto>(`/mva/terminer/${id}`);
      return data;
    },
    enabled: id.length > 0,
  });
}

export function useOpprettTerminer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: GenererTerminerRequest) => {
      const { data } = await apiClient.post<MvaTerminDto[]>('/mva/terminer/generer', request);
      return data;
    },
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['mvaTerminer', variables.ar] });
    },
  });
}

// --- MVA-oppgjor ---

export function useMvaOppgjor(terminId: string) {
  return useQuery({
    queryKey: ['mvaOppgjor', terminId],
    queryFn: async () => {
      const { data } = await apiClient.get<MvaOppgjorDto>(`/mva/terminer/${terminId}/oppgjor`);
      return data;
    },
    enabled: terminId.length > 0,
  });
}

export function useBeregnOppgjor() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (terminId: string) => {
      const { data } = await apiClient.post<MvaOppgjorDto>(
        `/mva/terminer/${terminId}/oppgjor/beregn`,
      );
      return data;
    },
    onSuccess: (_data, terminId) => {
      void queryClient.invalidateQueries({ queryKey: ['mvaOppgjor', terminId] });
      void queryClient.invalidateQueries({ queryKey: ['mvaTerminer'] });
    },
  });
}

export function useBokforOppgjor() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (terminId: string) => {
      const { data } = await apiClient.post(`/mva/terminer/${terminId}/oppgjor/bokfor`);
      return data;
    },
    onSuccess: (_data, terminId) => {
      void queryClient.invalidateQueries({ queryKey: ['mvaOppgjor', terminId] });
      void queryClient.invalidateQueries({ queryKey: ['mvaTerminer'] });
    },
  });
}

// --- MVA-melding ---

export function useMvaMelding(terminId: string) {
  return useQuery({
    queryKey: ['mvaMelding', terminId],
    queryFn: async () => {
      const { data } = await apiClient.get<MvaMeldingDto>(`/mva/terminer/${terminId}/melding`);
      return data;
    },
    enabled: terminId.length > 0,
  });
}

export function useMarkerInnsendt() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (terminId: string) => {
      await apiClient.post(`/mva/terminer/${terminId}/melding/marker-innsendt`);
    },
    onSuccess: (_data, terminId) => {
      void queryClient.invalidateQueries({ queryKey: ['mvaMelding', terminId] });
      void queryClient.invalidateQueries({ queryKey: ['mvaTerminer'] });
    },
  });
}

// --- MVA-avstemming ---

export function useMvaAvstemming(terminId: string) {
  return useQuery({
    queryKey: ['mvaAvstemming', terminId],
    queryFn: async () => {
      const { data } = await apiClient.get<MvaAvstemmingDto>(
        `/mva/terminer/${terminId}/avstemming`,
      );
      return data;
    },
    enabled: terminId.length > 0,
  });
}

export function useKjorAvstemming() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (terminId: string) => {
      const { data } = await apiClient.post<MvaAvstemmingDto>(
        `/mva/terminer/${terminId}/avstemming/kjor`,
      );
      return data;
    },
    onSuccess: (_data, terminId) => {
      void queryClient.invalidateQueries({ queryKey: ['mvaAvstemming', terminId] });
      void queryClient.invalidateQueries({ queryKey: ['mvaTerminer'] });
    },
  });
}

export function useGodkjennAvstemming() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (params: { terminId: string; avstemmingId: string; merknad?: string }) => {
      await apiClient.post(
        `/mva/terminer/${params.terminId}/avstemming/${params.avstemmingId}/godkjenn`,
        { merknad: params.merknad },
      );
    },
    onSuccess: (_data, params) => {
      void queryClient.invalidateQueries({ queryKey: ['mvaAvstemming', params.terminId] });
      void queryClient.invalidateQueries({ queryKey: ['mvaTerminer'] });
    },
  });
}

// --- MVA-sammenstilling ---

export function useMvaSammenstilling(params: MvaSammenstillingParams) {
  return useQuery({
    queryKey: ['mvaSammenstilling', params],
    queryFn: async () => {
      const { data } = await apiClient.get<MvaSammenstillingDto>('/mva/sammenstilling', {
        params,
      });
      return data;
    },
    enabled: params.ar > 0,
  });
}

export function useMvaSammenstillingDetalj(params: MvaSammenstillingDetaljParams) {
  return useQuery({
    queryKey: ['mvaSammenstilling', 'detalj', params],
    queryFn: async () => {
      const { data } = await apiClient.get<MvaSammenstillingDetaljDto>(
        '/mva/sammenstilling/detalj',
        { params },
      );
      return data;
    },
    enabled: params.ar > 0 && params.mvaKode.length > 0,
  });
}
