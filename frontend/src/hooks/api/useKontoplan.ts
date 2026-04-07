import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../api/client';
import type {
  KontogruppeListeDto,
  KontogruppeDetaljerDto,
  KontoListeDto,
  KontoDetaljerDto,
  OpprettKontoRequest,
  OpprettKontoResponse,
  OppdaterKontoRequest,
  KontoOppslagDto,
  KontoSokParams,
  PaginertResultat,
  MvaKodeDto,
  MvaKodeSokParams,
  OpprettMvaKodeRequest,
  OppdaterMvaKodeRequest,
  ImportResultatDto,
  ImportModus,
  ImportFormat,
  EksportFormat,
} from '../../types/kontoplan';

// --- Kontogrupper ---

export function useKontogrupper() {
  return useQuery({
    queryKey: ['kontogrupper'],
    queryFn: async () => {
      const { data } = await apiClient.get<KontogruppeListeDto[]>('/kontogrupper');
      return data;
    },
  });
}

export function useKontogruppe(gruppekode: number) {
  return useQuery({
    queryKey: ['kontogrupper', gruppekode],
    queryFn: async () => {
      const { data } = await apiClient.get<KontogruppeDetaljerDto>(`/kontogrupper/${gruppekode}`);
      return data;
    },
    enabled: gruppekode > 0,
  });
}

// --- Kontoer ---

export function useKontoer(params?: KontoSokParams) {
  return useQuery({
    queryKey: ['kontoer', params],
    queryFn: async () => {
      const { data } = await apiClient.get<PaginertResultat<KontoListeDto>>('/kontoer', { params });
      return data;
    },
  });
}

export function useKonto(kontonummer: string) {
  return useQuery({
    queryKey: ['kontoer', kontonummer],
    queryFn: async () => {
      const { data } = await apiClient.get<KontoDetaljerDto>(`/kontoer/${kontonummer}`);
      return data;
    },
    enabled: kontonummer.length >= 4,
  });
}

export function useOpprettKonto() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OpprettKontoRequest) => {
      const { data } = await apiClient.post<OpprettKontoResponse>('/kontoer', request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['kontoer'] });
      void queryClient.invalidateQueries({ queryKey: ['kontogrupper'] });
    },
  });
}

export function useOppdaterKonto(kontonummer: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OppdaterKontoRequest) => {
      const { data } = await apiClient.put(`/kontoer/${kontonummer}`, request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['kontoer'] });
      void queryClient.invalidateQueries({ queryKey: ['kontogrupper'] });
    },
  });
}

export function useSlettKonto() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (kontonummer: string) => {
      await apiClient.delete(`/kontoer/${kontonummer}`);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['kontoer'] });
      void queryClient.invalidateQueries({ queryKey: ['kontogrupper'] });
    },
  });
}

export function useDeaktiverKonto() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (kontonummer: string) => {
      await apiClient.post(`/kontoer/${kontonummer}/deaktiver`);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['kontoer'] });
    },
  });
}

export function useAktiverKonto() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (kontonummer: string) => {
      await apiClient.post(`/kontoer/${kontonummer}/aktiver`);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['kontoer'] });
    },
  });
}

// --- Konto-oppslag (autocomplete) ---

export function useKontoOppslag(sok: string, antall = 10) {
  return useQuery({
    queryKey: ['kontoer', 'oppslag', sok, antall],
    queryFn: async () => {
      const { data } = await apiClient.get<KontoOppslagDto[]>('/kontoer/oppslag', {
        params: { q: sok, antall },
      });
      return data;
    },
    enabled: sok.length >= 1,
  });
}

// --- MVA-koder ---

export function useMvaKoder(params?: MvaKodeSokParams) {
  return useQuery({
    queryKey: ['mvaKoder', params],
    queryFn: async () => {
      const { data } = await apiClient.get<MvaKodeDto[]>('/mva-koder', { params });
      return data;
    },
  });
}

export function useMvaKode(kode: string) {
  return useQuery({
    queryKey: ['mvaKoder', kode],
    queryFn: async () => {
      const { data } = await apiClient.get<MvaKodeDto>(`/mva-koder/${kode}`);
      return data;
    },
    enabled: kode.length > 0,
  });
}

export function useOpprettMvaKode() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OpprettMvaKodeRequest) => {
      const { data } = await apiClient.post<MvaKodeDto>('/mva-koder', request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['mvaKoder'] });
    },
  });
}

export function useOppdaterMvaKode(kode: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OppdaterMvaKodeRequest) => {
      const { data } = await apiClient.put(`/mva-koder/${kode}`, request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['mvaKoder'] });
    },
  });
}

// --- Import/Eksport ---

export function useImporterKontoplan() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (params: { fil: File; format: ImportFormat; modus: ImportModus }) => {
      const formData = new FormData();
      formData.append('fil', params.fil);
      formData.append('format', params.format);
      formData.append('modus', params.modus);
      const { data } = await apiClient.post<ImportResultatDto>('/kontoer/importer', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['kontoer'] });
      void queryClient.invalidateQueries({ queryKey: ['kontogrupper'] });
    },
  });
}

export function useEksporterKontoplan() {
  return useMutation({
    mutationFn: async (params: { format: EksportFormat; inkluderInaktive?: boolean }) => {
      const { data } = await apiClient.get('/kontoer/eksporter', {
        params,
        responseType: 'blob',
      });
      return data as Blob;
    },
  });
}
