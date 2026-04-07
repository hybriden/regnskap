import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../api/client';
import type {
  BankkontoDto,
  OpprettBankkontoRequest,
  KontoutskriftDto,
  ImportKontoutskriftResponse,
  BankbevegelseDto,
  BankbevegelserParams,
  ManuellMatchRequest,
  SplittMatchRequest,
  BokforBankbevegelseRequest,
  MatcheForslagDto,
  AvstemmingDto,
  OppdaterAvstemmingRequest,
  AvstemmingsrapportDto,
} from '../../types/bank';

// --- Bankkontoer ---

export function useBankkontoer() {
  return useQuery({
    queryKey: ['bankkontoer'],
    queryFn: async () => {
      const { data } = await apiClient.get<BankkontoDto[]>('/bankkontoer');
      return data;
    },
  });
}

export function useBankkonto(id: string) {
  return useQuery({
    queryKey: ['bankkontoer', id],
    queryFn: async () => {
      const { data } = await apiClient.get<BankkontoDto>(`/bankkontoer/${id}`);
      return data;
    },
    enabled: id.length > 0,
  });
}

export function useOpprettBankkonto() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OpprettBankkontoRequest) => {
      const { data } = await apiClient.post<BankkontoDto>('/bankkontoer', request);
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['bankkontoer'] });
    },
  });
}

export function useOppdaterBankkonto() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, ...request }: OpprettBankkontoRequest & { id: string }) => {
      const { data } = await apiClient.put<BankkontoDto>(`/bankkontoer/${id}`, request);
      return data;
    },
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['bankkontoer', variables.id] });
      void queryClient.invalidateQueries({ queryKey: ['bankkontoer'] });
    },
  });
}

// --- Kontoutskrifter ---

export function useKontoutskrifter(bankkontoId: string) {
  return useQuery({
    queryKey: ['kontoutskrifter', bankkontoId],
    queryFn: async () => {
      const { data } = await apiClient.get<KontoutskriftDto[]>(
        `/bankkontoer/${bankkontoId}/kontoutskrifter`,
      );
      return data;
    },
    enabled: bankkontoId.length > 0,
  });
}

export function useKontoutskrift(id: string) {
  return useQuery({
    queryKey: ['kontoutskrifter', 'detalj', id],
    queryFn: async () => {
      const { data } = await apiClient.get<KontoutskriftDto>(`/kontoutskrifter/${id}`);
      return data;
    },
    enabled: id.length > 0,
  });
}

export function useImporterKontoutskrift() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ bankkontoId, fil }: { bankkontoId: string; fil: File }) => {
      const formData = new FormData();
      formData.append('fil', fil);
      const { data } = await apiClient.post<ImportKontoutskriftResponse>(
        `/bankkontoer/${bankkontoId}/import`,
        formData,
        { headers: { 'Content-Type': 'multipart/form-data' } },
      );
      return data;
    },
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({ queryKey: ['kontoutskrifter', variables.bankkontoId] });
      void queryClient.invalidateQueries({ queryKey: ['bankbevegelser'] });
      void queryClient.invalidateQueries({ queryKey: ['bankkontoer'] });
    },
  });
}

// --- Bankbevegelser ---

export function useBankbevegelser(params: BankbevegelserParams) {
  return useQuery({
    queryKey: ['bankbevegelser', params],
    queryFn: async () => {
      const { bankkontoId, ...rest } = params;
      const { data } = await apiClient.get<BankbevegelseDto[]>(
        `/bankkontoer/${bankkontoId}/bevegelser`,
        { params: rest },
      );
      return data;
    },
    enabled: params.bankkontoId.length > 0,
  });
}

export function useBankbevegelse(id: string) {
  return useQuery({
    queryKey: ['bankbevegelser', 'detalj', id],
    queryFn: async () => {
      const { data } = await apiClient.get<BankbevegelseDto>(`/bankbevegelser/${id}`);
      return data;
    },
    enabled: id.length > 0,
  });
}

// --- Matching ---

export function useMatchForslag(bankbevegelseId: string) {
  return useQuery({
    queryKey: ['matchForslag', bankbevegelseId],
    queryFn: async () => {
      const { data } = await apiClient.get<MatcheForslagDto[]>(
        `/bankbevegelser/${bankbevegelseId}/forslag`,
      );
      return data;
    },
    enabled: bankbevegelseId.length > 0,
  });
}

export function useManuellMatch() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      bankbevegelseId,
      request,
    }: {
      bankbevegelseId: string;
      request: ManuellMatchRequest;
    }) => {
      await apiClient.post(`/bankbevegelser/${bankbevegelseId}/match`, request);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['bankbevegelser'] });
      void queryClient.invalidateQueries({ queryKey: ['bankavstemming'] });
    },
  });
}

export function useSplittMatch() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      bankbevegelseId,
      request,
    }: {
      bankbevegelseId: string;
      request: SplittMatchRequest;
    }) => {
      await apiClient.post(`/bankbevegelser/${bankbevegelseId}/splitt`, request);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['bankbevegelser'] });
      void queryClient.invalidateQueries({ queryKey: ['bankavstemming'] });
    },
  });
}

export function useBokforBevegelse() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      bankbevegelseId,
      request,
    }: {
      bankbevegelseId: string;
      request: BokforBankbevegelseRequest;
    }) => {
      await apiClient.post(`/bankbevegelser/${bankbevegelseId}/bokfor`, request);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['bankbevegelser'] });
      void queryClient.invalidateQueries({ queryKey: ['bankavstemming'] });
    },
  });
}

export function useIgnorerBevegelse() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (bankbevegelseId: string) => {
      await apiClient.post(`/bankbevegelser/${bankbevegelseId}/ignorer`);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['bankbevegelser'] });
      void queryClient.invalidateQueries({ queryKey: ['bankavstemming'] });
    },
  });
}

export function useFjernMatch() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (bankbevegelseId: string) => {
      await apiClient.delete(`/bankbevegelser/${bankbevegelseId}/match`);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['bankbevegelser'] });
      void queryClient.invalidateQueries({ queryKey: ['bankavstemming'] });
    },
  });
}

// --- Auto-matching ---

export function useAutoMatch() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (bankkontoId: string) => {
      const { data } = await apiClient.post<{ antallMatchet: number }>(
        `/bankkontoer/${bankkontoId}/auto-match`,
      );
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['bankbevegelser'] });
      void queryClient.invalidateQueries({ queryKey: ['bankavstemming'] });
    },
  });
}

// --- Avstemming ---

export function useBankavstemming(bankkontoId: string) {
  return useQuery({
    queryKey: ['bankavstemming', bankkontoId],
    queryFn: async () => {
      const { data } = await apiClient.get<AvstemmingDto>(
        `/bankkontoer/${bankkontoId}/avstemming`,
      );
      return data;
    },
    enabled: bankkontoId.length > 0,
  });
}

export function useOppdaterAvstemming() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({
      bankkontoId,
      request,
    }: {
      bankkontoId: string;
      request: OppdaterAvstemmingRequest;
    }) => {
      const { data } = await apiClient.post<AvstemmingDto>(
        `/bankkontoer/${bankkontoId}/avstemming`,
        request,
      );
      return data;
    },
    onSuccess: (_data, variables) => {
      void queryClient.invalidateQueries({
        queryKey: ['bankavstemming', variables.bankkontoId],
      });
    },
  });
}

export function useGodkjennAvstemming() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (bankkontoId: string) => {
      await apiClient.post(`/bankkontoer/${bankkontoId}/avstemming/godkjenn`);
    },
    onSuccess: (_data, bankkontoId) => {
      void queryClient.invalidateQueries({ queryKey: ['bankavstemming', bankkontoId] });
      void queryClient.invalidateQueries({ queryKey: ['bankkontoer'] });
    },
  });
}

export function useAvstemmingsrapport(bankkontoId: string, dato?: string) {
  return useQuery({
    queryKey: ['avstemmingsrapport', bankkontoId, dato],
    queryFn: async () => {
      const { data } = await apiClient.get<AvstemmingsrapportDto>(
        `/bankkontoer/${bankkontoId}/avstemming/rapport`,
        { params: dato ? { dato } : undefined },
      );
      return data;
    },
    enabled: bankkontoId.length > 0,
  });
}
