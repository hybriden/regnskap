import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../api/client';
import type {
  FakturaResponse,
  FakturaListeResponse,
  OpprettFakturaRequest,
  OpprettKreditnotaRequest,
} from '../../types/faktura';
import type { PagedResult } from '../../types/kunde';

// --- Fakturaliste ---

export function useFakturaer(params?: {
  page?: number;
  pageSize?: number;
  status?: string;
  kundeId?: string;
  sok?: string;
  fraDato?: string;
  tilDato?: string;
}) {
  return useQuery({
    queryKey: ['fakturaer', params],
    queryFn: async () => {
      const { data } = await apiClient.get<PagedResult<FakturaListeResponse>>('/fakturaer', {
        params,
      });
      return data;
    },
  });
}

// --- Enkelt faktura ---

export function useFaktura(id: string) {
  return useQuery({
    queryKey: ['fakturaer', 'detalj', id],
    queryFn: async () => {
      const { data } = await apiClient.get<FakturaResponse>(`/fakturaer/${id}`);
      return data;
    },
    enabled: id.length > 0,
  });
}

// --- Opprett faktura (utkast) ---

export function useOpprettFaktura() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OpprettFakturaRequest) => {
      const { data } = await apiClient.post<FakturaResponse>('/fakturaer', request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['fakturaer'] });
    },
  });
}

// --- Oppdater utkast ---

export function useOppdaterFaktura() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: OpprettFakturaRequest }) => {
      const { data } = await apiClient.put<FakturaResponse>(`/fakturaer/${id}`, request);
      return data;
    },
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['fakturaer'] });
      void queryClient.invalidateQueries({ queryKey: ['fakturaer', 'detalj', variables.id] });
    },
  });
}

// --- Utstede faktura ---

export function useUtstedeFaktura() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await apiClient.post<FakturaResponse>(`/fakturaer/${id}/utstede`);
      return data;
    },
    onSuccess: (_data, id) => {
      void queryClient.invalidateQueries({ queryKey: ['fakturaer'] });
      void queryClient.invalidateQueries({ queryKey: ['fakturaer', 'detalj', id] });
    },
  });
}

// --- Opprett kreditnota ---

export function useOpprettKreditnota() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      id,
      request,
    }: {
      id: string;
      request: OpprettKreditnotaRequest;
    }) => {
      const { data } = await apiClient.post<FakturaResponse>(
        `/fakturaer/${id}/kreditnota`,
        request,
      );
      return data;
    },
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['fakturaer'] });
      void queryClient.invalidateQueries({ queryKey: ['fakturaer', 'detalj', variables.id] });
    },
  });
}

// --- Generer EHF ---

export function useGenererEhf() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.post(`/fakturaer/${id}/ehf`);
    },
    onSuccess: (_data, id) => {
      void queryClient.invalidateQueries({ queryKey: ['fakturaer', 'detalj', id] });
    },
  });
}

// --- Generer PDF ---

export function useGenererPdf() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.post(`/fakturaer/${id}/pdf`);
    },
    onSuccess: (_data, id) => {
      void queryClient.invalidateQueries({ queryKey: ['fakturaer', 'detalj', id] });
    },
  });
}

// --- Last ned EHF ---

export function useLastNedEhf() {
  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await apiClient.get<Blob>(`/fakturaer/${id}/ehf`, {
        responseType: 'blob',
      });
      return data;
    },
  });
}

// --- Last ned PDF ---

export function useLastNedPdf() {
  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await apiClient.get<Blob>(`/fakturaer/${id}/pdf`, {
        responseType: 'blob',
      });
      return data;
    },
  });
}

// --- Kanseller utkast ---

export function useKansellerFaktura() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/fakturaer/${id}`);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['fakturaer'] });
    },
  });
}
