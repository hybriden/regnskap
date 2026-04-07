import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../api/client';
import type {
  KundeDto,
  OpprettKundeRequest,
  OppdaterKundeRequest,
  KundeFakturaDto,
  RegistrerKundeFakturaRequest,
  KundeInnbetalingDto,
  RegistrerInnbetalingRequest,
  MatchKidRequest,
  UmatchetInnbetalingDto,
  PurringDto,
  PurreforslagDto,
  PurreforslagRequest,
  OpprettPurringerRequest,
  KundeAldersfordelingDto,
  KundeutskriftDto,
  KundeSaldoDto,
  PagedResult,
} from '../../types/kunde';

// --- Kunderegister ---

export function useKunder(params?: { page?: number; pageSize?: number }) {
  return useQuery({
    queryKey: ['kunder', params],
    queryFn: async () => {
      const { data } = await apiClient.get<PagedResult<KundeDto>>('/kunder', { params });
      return data;
    },
  });
}

export function useKunde(id: string) {
  return useQuery({
    queryKey: ['kunder', 'detalj', id],
    queryFn: async () => {
      const { data } = await apiClient.get<KundeDto>(`/kunder/${id}`);
      return data;
    },
    enabled: id.length > 0,
  });
}

export function useSokKunder(query: string) {
  return useQuery({
    queryKey: ['kunder', 'sok', query],
    queryFn: async () => {
      const { data } = await apiClient.get<KundeDto[]>('/kunder/sok', {
        params: { q: query },
      });
      return data;
    },
    enabled: query.length >= 2,
  });
}

export function useKundeSaldo(id: string) {
  return useQuery({
    queryKey: ['kunder', 'saldo', id],
    queryFn: async () => {
      const { data } = await apiClient.get<KundeSaldoDto>(`/kunder/${id}/saldo`);
      return data;
    },
    enabled: id.length > 0,
  });
}

export function useOpprettKunde() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OpprettKundeRequest) => {
      const { data } = await apiClient.post<KundeDto>('/kunder', request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['kunder'] });
    },
  });
}

export function useOppdaterKunde() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: OppdaterKundeRequest }) => {
      const { data } = await apiClient.put<KundeDto>(`/kunder/${id}`, request);
      return data;
    },
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['kunder'] });
      void queryClient.invalidateQueries({ queryKey: ['kunder', 'detalj', variables.id] });
    },
  });
}

export function useSlettKunde() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/kunder/${id}`);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['kunder'] });
    },
  });
}

// --- Kundefakturaer ---

export function useKundeFakturaer(params?: {
  page?: number;
  pageSize?: number;
  kundeId?: string;
  status?: string;
}) {
  return useQuery({
    queryKey: ['kundefakturaer', params],
    queryFn: async () => {
      const { data } = await apiClient.get<PagedResult<KundeFakturaDto>>('/kundefakturaer', {
        params,
      });
      return data;
    },
  });
}

export function useKundeFaktura(id: string) {
  return useQuery({
    queryKey: ['kundefakturaer', 'detalj', id],
    queryFn: async () => {
      const { data } = await apiClient.get<KundeFakturaDto>(`/kundefakturaer/${id}`);
      return data;
    },
    enabled: id.length > 0,
  });
}

export function useRegistrerKundeFaktura() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: RegistrerKundeFakturaRequest) => {
      const { data } = await apiClient.post<KundeFakturaDto>('/kundefakturaer', request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['kundefakturaer'] });
      void queryClient.invalidateQueries({ queryKey: ['kunder'] });
    },
  });
}

// --- Apne poster ---

export function useApnePoster(dato?: string) {
  return useQuery({
    queryKey: ['kundefakturaer', 'apne-poster', dato],
    queryFn: async () => {
      const { data } = await apiClient.get<KundeFakturaDto[]>('/kundefakturaer/apne-poster', {
        params: dato ? { dato } : undefined,
      });
      return data;
    },
  });
}

// --- Innbetalinger ---

export function useRegistrerInnbetaling() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: RegistrerInnbetalingRequest) => {
      const { data } = await apiClient.post<KundeInnbetalingDto>(
        '/kundeinnbetalinger',
        request,
      );
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['kundefakturaer'] });
      void queryClient.invalidateQueries({ queryKey: ['kunder'] });
    },
  });
}

export function useMatchKid() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: MatchKidRequest) => {
      const { data } = await apiClient.post<KundeInnbetalingDto>(
        '/kundeinnbetalinger/match-kid',
        request,
      );
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['kundefakturaer'] });
      void queryClient.invalidateQueries({ queryKey: ['kunder'] });
      void queryClient.invalidateQueries({ queryKey: ['umatchede'] });
    },
  });
}

export function useUmatchedeInnbetalinger() {
  return useQuery({
    queryKey: ['umatchede'],
    queryFn: async () => {
      const { data } = await apiClient.get<UmatchetInnbetalingDto[]>(
        '/kundeinnbetalinger/umatchede',
      );
      return data;
    },
  });
}

// --- Purringer ---

export function usePurreforslag(request: PurreforslagRequest | null) {
  return useQuery({
    queryKey: ['purringer', 'forslag', request],
    queryFn: async () => {
      const { data } = await apiClient.get<PurreforslagDto[]>('/purringer/forslag', {
        params: request,
      });
      return data;
    },
    enabled: request !== null,
  });
}

export function useOpprettPurringer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OpprettPurringerRequest) => {
      const { data } = await apiClient.post<PurringDto[]>('/purringer/opprett', request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['purringer'] });
      void queryClient.invalidateQueries({ queryKey: ['kundefakturaer'] });
    },
  });
}

export function useSendPurring() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, sendemetode }: { id: string; sendemetode: string }) => {
      await apiClient.post(`/purringer/${id}/send`, { sendemetode });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['purringer'] });
    },
  });
}

export function usePurringer(params?: { page?: number; pageSize?: number }) {
  return useQuery({
    queryKey: ['purringer', params],
    queryFn: async () => {
      const { data } = await apiClient.get<PurringDto[]>('/purringer', { params });
      return data;
    },
  });
}

// --- Rapporter ---

export function useAldersfordeling(dato: string) {
  return useQuery({
    queryKey: ['rapporter', 'kunde', 'aldersfordeling', dato],
    queryFn: async () => {
      const { data } = await apiClient.get<KundeAldersfordelingDto>(
        '/rapporter/kunde/aldersfordeling',
        { params: { dato } },
      );
      return data;
    },
    enabled: dato.length > 0,
  });
}

export function useKundeutskrift(kundeId: string, fraDato: string, tilDato: string) {
  return useQuery({
    queryKey: ['rapporter', 'kunde', 'utskrift', kundeId, fraDato, tilDato],
    queryFn: async () => {
      const { data } = await apiClient.get<KundeutskriftDto>(
        `/rapporter/kunde/utskrift/${kundeId}`,
        { params: { fra: fraDato, til: tilDato } },
      );
      return data;
    },
    enabled: kundeId.length > 0 && fraDato.length > 0 && tilDato.length > 0,
  });
}
