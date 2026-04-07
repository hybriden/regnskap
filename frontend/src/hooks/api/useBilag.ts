import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../api/client';
import type {
  BilagDto,
  BilagSokParams,
  BilagSokResultat,
  OpprettBilagRequest,
  TilbakeforBilagRequest,
  ValiderBilagRequest,
  BilagValideringResultat,
  VedleggDto,
  LeggTilVedleggRequest,
  BilagSerieDto,
  OpprettBilagSerieRequest,
  OppdaterBilagSerieRequest,
} from '../../types/bilag';

// --- Bilag CRUD ---

export function useBilagDetaljer(id: string) {
  return useQuery({
    queryKey: ['bilag', id],
    queryFn: async () => {
      const { data } = await apiClient.get<BilagDto>(`/bilag/${id}`);
      return data;
    },
    enabled: id.length > 0,
  });
}

export function useBilagSok(params: BilagSokParams) {
  return useQuery({
    queryKey: ['bilag', 'sok', params],
    queryFn: async () => {
      const { data } = await apiClient.post<BilagSokResultat>('/bilag/sok', params);
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

export function useBokforBilag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await apiClient.post<BilagDto>(`/bilag/${id}/bokfor`);
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

export function useTilbakeforBilag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: TilbakeforBilagRequest) => {
      const { data } = await apiClient.post<BilagDto>(
        `/bilag/${request.originalBilagId}/tilbakefor`,
        request,
      );
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

export function useValiderBilag() {
  return useMutation({
    mutationFn: async (request: ValiderBilagRequest) => {
      const { data } = await apiClient.post<BilagValideringResultat>('/bilag/valider', request);
      return data;
    },
  });
}

// --- Vedlegg ---

export function useVedlegg(bilagId: string) {
  return useQuery({
    queryKey: ['bilag', bilagId, 'vedlegg'],
    queryFn: async () => {
      const { data } = await apiClient.get<VedleggDto[]>(`/bilag/${bilagId}/vedlegg`);
      return data;
    },
    enabled: bilagId.length > 0,
  });
}

export function useLastOppVedlegg() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: LeggTilVedleggRequest) => {
      const { data } = await apiClient.post<VedleggDto>(
        `/bilag/${request.bilagId}/vedlegg`,
        request,
      );
      return data;
    },
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['bilag', variables.bilagId, 'vedlegg'] });
      void queryClient.invalidateQueries({ queryKey: ['bilag', variables.bilagId] });
    },
  });
}

export function useSlettVedlegg() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (params: { bilagId: string; vedleggId: string }) => {
      await apiClient.delete(`/bilag/${params.bilagId}/vedlegg/${params.vedleggId}`);
    },
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['bilag', variables.bilagId, 'vedlegg'] });
      void queryClient.invalidateQueries({ queryKey: ['bilag', variables.bilagId] });
    },
  });
}

// --- Bilagserier ---

export function useBilagSerier() {
  return useQuery({
    queryKey: ['bilagserier'],
    queryFn: async () => {
      const { data } = await apiClient.get<BilagSerieDto[]>('/bilagserier');
      return data;
    },
  });
}

export function useBilagSerie(kode: string) {
  return useQuery({
    queryKey: ['bilagserier', kode],
    queryFn: async () => {
      const { data } = await apiClient.get<BilagSerieDto>(`/bilagserier/${kode}`);
      return data;
    },
    enabled: kode.length > 0,
  });
}

export function useOpprettBilagSerie() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OpprettBilagSerieRequest) => {
      const { data } = await apiClient.post<BilagSerieDto>('/bilagserier', request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['bilagserier'] });
    },
  });
}

export function useOppdaterBilagSerie(kode: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OppdaterBilagSerieRequest) => {
      const { data } = await apiClient.put<BilagSerieDto>(`/bilagserier/${kode}`, request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['bilagserier'] });
    },
  });
}
