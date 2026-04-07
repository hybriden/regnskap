import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../api/client';
import type {
  LeverandorDto,
  LeverandorDetaljerDto,
  LeverandorSokParams,
  LeverandorSokResultat,
  OpprettLeverandorRequest,
  OppdaterLeverandorRequest,
  LeverandorFakturaDto,
  FakturaSokParams,
  FakturaSokResultat,
  RegistrerFakturaRequest,
  BetalingsforslagDto,
  GenererBetalingsforslagRequest,
  AldersfordelingDto,
  LeverandorutskriftDto,
  ApnePostRapportDto,
} from '../../types/leverandor';

// ============================================================
// Leverandorregister
// ============================================================

export function useLeverandorSok(params: LeverandorSokParams) {
  return useQuery({
    queryKey: ['leverandorer', 'sok', params],
    queryFn: async () => {
      const { data } = await apiClient.get<LeverandorSokResultat>('/leverandorer', { params });
      return data;
    },
  });
}

export function useLeverandorOppslag(query: string) {
  return useQuery({
    queryKey: ['leverandorer', 'oppslag', query],
    queryFn: async () => {
      const { data } = await apiClient.get<LeverandorDto[]>('/leverandorer/sok', {
        params: { q: query },
      });
      return data;
    },
    enabled: query.length >= 1,
  });
}

export function useLeverandor(id: string) {
  return useQuery({
    queryKey: ['leverandorer', id],
    queryFn: async () => {
      const { data } = await apiClient.get<LeverandorDetaljerDto>(`/leverandorer/${id}`);
      return data;
    },
    enabled: id.length > 0,
  });
}

export function useLeverandorSaldo(id: string) {
  return useQuery({
    queryKey: ['leverandorer', id, 'saldo'],
    queryFn: async () => {
      const { data } = await apiClient.get<{ saldo: number }>(`/leverandorer/${id}/saldo`);
      return data;
    },
    enabled: id.length > 0,
  });
}

export function useOpprettLeverandor() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OpprettLeverandorRequest) => {
      const { data } = await apiClient.post<LeverandorDto>('/leverandorer', request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leverandorer'] });
    },
  });
}

export function useOppdaterLeverandor(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OppdaterLeverandorRequest) => {
      const { data } = await apiClient.put<LeverandorDto>(`/leverandorer/${id}`, request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leverandorer'] });
    },
  });
}

export function useSlettLeverandor() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/leverandorer/${id}`);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leverandorer'] });
    },
  });
}

// ============================================================
// Inngaende faktura
// ============================================================

export function useFakturaSok(params: FakturaSokParams) {
  return useQuery({
    queryKey: ['leverandorfakturaer', 'sok', params],
    queryFn: async () => {
      const { data } = await apiClient.get<FakturaSokResultat>('/leverandorfakturaer', { params });
      return data;
    },
  });
}

export function useFaktura(id: string) {
  return useQuery({
    queryKey: ['leverandorfakturaer', id],
    queryFn: async () => {
      const { data } = await apiClient.get<LeverandorFakturaDto>(`/leverandorfakturaer/${id}`);
      return data;
    },
    enabled: id.length > 0,
  });
}

export function useRegistrerFaktura() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: RegistrerFakturaRequest) => {
      const { data } = await apiClient.post<LeverandorFakturaDto>(
        '/leverandorfakturaer',
        request,
      );
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leverandorfakturaer'] });
      void queryClient.invalidateQueries({ queryKey: ['leverandorer'] });
      void queryClient.invalidateQueries({ queryKey: ['bilag'] });
      void queryClient.invalidateQueries({ queryKey: ['saldobalanse'] });
    },
  });
}

export function useGodkjennFaktura() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await apiClient.put<LeverandorFakturaDto>(
        `/leverandorfakturaer/${id}/godkjenn`,
      );
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leverandorfakturaer'] });
    },
  });
}

export function useSperrFaktura() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (params: { id: string; arsak: string }) => {
      const { data } = await apiClient.put<LeverandorFakturaDto>(
        `/leverandorfakturaer/${params.id}/sperr`,
        { arsak: params.arsak },
      );
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leverandorfakturaer'] });
    },
  });
}

export function useOpphevSperring() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await apiClient.put<LeverandorFakturaDto>(
        `/leverandorfakturaer/${id}/opphev-sperring`,
      );
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['leverandorfakturaer'] });
    },
  });
}

// ============================================================
// Apne poster
// ============================================================

export function useApnePoster(dato?: string) {
  return useQuery({
    queryKey: ['leverandorfakturaer', 'apne-poster', dato],
    queryFn: async () => {
      const { data } = await apiClient.get<ApnePostRapportDto>(
        '/leverandorfakturaer/apne-poster',
        { params: dato ? { dato } : undefined },
      );
      return data;
    },
  });
}

// ============================================================
// Aldersfordeling
// ============================================================

export function useAldersfordeling(dato: string) {
  return useQuery({
    queryKey: ['rapporter', 'leverandor', 'aldersfordeling', dato],
    queryFn: async () => {
      const { data } = await apiClient.get<AldersfordelingDto>(
        '/rapporter/leverandor/aldersfordeling',
        { params: { dato } },
      );
      return data;
    },
    enabled: dato.length > 0,
  });
}

// ============================================================
// Leverandorutskrift
// ============================================================

export function useLeverandorutskrift(leverandorId: string, fraDato: string, tilDato: string) {
  return useQuery({
    queryKey: ['rapporter', 'leverandor', 'utskrift', leverandorId, fraDato, tilDato],
    queryFn: async () => {
      const { data } = await apiClient.get<LeverandorutskriftDto>(
        `/rapporter/leverandor/utskrift/${leverandorId}`,
        { params: { fra: fraDato, til: tilDato } },
      );
      return data;
    },
    enabled: leverandorId.length > 0 && fraDato.length > 0 && tilDato.length > 0,
  });
}

// ============================================================
// Betalingsforslag
// ============================================================

export function useBetalingsforslag(params?: { side?: number; antall?: number }) {
  return useQuery({
    queryKey: ['betalingsforslag', params],
    queryFn: async () => {
      const { data } = await apiClient.get<{
        data: BetalingsforslagDto[];
        totaltAntall: number;
        side: number;
        antall: number;
      }>('/betalingsforslag', { params });
      return data;
    },
  });
}

export function useBetalingsforslagDetaljer(id: string) {
  return useQuery({
    queryKey: ['betalingsforslag', id],
    queryFn: async () => {
      const { data } = await apiClient.get<BetalingsforslagDto>(`/betalingsforslag/${id}`);
      return data;
    },
    enabled: id.length > 0,
  });
}

export function useGenererBetalingsforslag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: GenererBetalingsforslagRequest) => {
      const { data } = await apiClient.post<BetalingsforslagDto>(
        '/betalingsforslag/generer',
        request,
      );
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['betalingsforslag'] });
      void queryClient.invalidateQueries({ queryKey: ['leverandorfakturaer'] });
    },
  });
}

export function useGodkjennBetalingsforslag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await apiClient.put<BetalingsforslagDto>(
        `/betalingsforslag/${id}/godkjenn`,
      );
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['betalingsforslag'] });
    },
  });
}

export function useEkskluderLinje() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (params: { forslagId: string; linjeId: string }) => {
      await apiClient.put(
        `/betalingsforslag/${params.forslagId}/linjer/${params.linjeId}/ekskluder`,
      );
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['betalingsforslag'] });
    },
  });
}

export function useInkluderLinje() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (params: { forslagId: string; linjeId: string }) => {
      await apiClient.put(
        `/betalingsforslag/${params.forslagId}/linjer/${params.linjeId}/inkluder`,
      );
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['betalingsforslag'] });
    },
  });
}

export function useGenererBetalingsfil() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const { data } = await apiClient.post<Blob>(
        `/betalingsforslag/${id}/generer-fil`,
        null,
        { responseType: 'blob' },
      );
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['betalingsforslag'] });
    },
  });
}

export function useMarkerSendt() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.put(`/betalingsforslag/${id}/marker-sendt`);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['betalingsforslag'] });
      void queryClient.invalidateQueries({ queryKey: ['leverandorfakturaer'] });
    },
  });
}

export function useKansellerBetalingsforslag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await apiClient.delete(`/betalingsforslag/${id}`);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['betalingsforslag'] });
      void queryClient.invalidateQueries({ queryKey: ['leverandorfakturaer'] });
    },
  });
}
