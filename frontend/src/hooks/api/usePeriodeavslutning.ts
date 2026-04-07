import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import apiClient from '../../api/client';
import type {
  AvstemmingResultatDto,
  LukkPeriodeRequest,
  PeriodeLukkingDto,
  GjenapnePeriodeRequest,
  ArsavslutningRequest,
  ArsavslutningDto,
  ArsavslutningStatusDto,
  AnleggsmiddelDto,
  OpprettAnleggsmiddelRequest,
  BeregnAvskrivningerRequest,
  AvskrivningBeregningDto,
  BokforAvskrivningerRequest,
  PeriodiseringDto,
  OpprettPeriodiseringRequest,
  BokforPeriodiseringerRequest,
  PeriodiseringBokforingDto,
  ArsregnskapsklarDto,
  PeriodeStatusDto,
} from '../../types/periodeavslutning';

// --- Periodeoversikt ---

export function usePeriodeStatus(ar: number) {
  return useQuery({
    queryKey: ['periodeStatus', ar],
    queryFn: async () => {
      // Use hovedbok perioder endpoint and map to PeriodeStatusDto
      const { data } = await apiClient.get<{ ar: number; perioder: Array<{
        id: string; ar: number; periode: number; periodenavn: string;
        status: string; lukketTidspunkt: string | null; lukketAv: string | null;
      }> }>(`/perioder/${ar}`);
      return (data.perioder ?? []).map((p): PeriodeStatusDto => ({
        ar: p.ar,
        periode: p.periode,
        periodenavn: p.periodenavn,
        status: p.status,
        lukketTidspunkt: p.lukketTidspunkt,
        lukketAv: p.lukketAv,
        antallBilag: 0,
        sumDebet: 0,
        sumKredit: 0,
      }));
    },
    enabled: ar > 0,
  });
}

// --- Månedlig avstemming ---

export function useKjorAvstemming() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (params: { ar: number; periode: number }) => {
      const { data } = await apiClient.post<AvstemmingResultatDto>(
        `/periodeavslutning/${params.ar}/${params.periode}/avstemming`,
      );
      return data;
    },
    onSuccess: (_data, params) => {
      void queryClient.invalidateQueries({
        queryKey: ['periodeStatus', params.ar],
      });
    },
  });
}

// --- Månedlig lukking ---

export function useLukkPeriode() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (params: {
      ar: number;
      periode: number;
      request: LukkPeriodeRequest;
    }) => {
      const { data } = await apiClient.post<PeriodeLukkingDto>(
        `/periodeavslutning/${params.ar}/${params.periode}/lukk`,
        params.request,
      );
      return data;
    },
    onSuccess: (_data, params) => {
      void queryClient.invalidateQueries({
        queryKey: ['periodeStatus', params.ar],
      });
    },
  });
}

export function useGjenapnePeriode() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (params: {
      ar: number;
      periode: number;
      request: GjenapnePeriodeRequest;
    }) => {
      await apiClient.post(
        `/periodeavslutning/${params.ar}/${params.periode}/gjenapne`,
        params.request,
      );
    },
    onSuccess: (_data, params) => {
      void queryClient.invalidateQueries({
        queryKey: ['periodeStatus', params.ar],
      });
    },
  });
}

// --- Årsavslutning ---

export function useArsavslutningStatus(ar: number) {
  return useQuery({
    queryKey: ['arsavslutning', ar],
    queryFn: async () => {
      const { data } = await apiClient.get<ArsavslutningStatusDto>(
        `/periodeavslutning/${ar}/arsavslutning/status`,
      );
      return data;
    },
    enabled: ar > 0,
  });
}

export function useKjorArsavslutning() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (params: { ar: number; request: ArsavslutningRequest }) => {
      const { data } = await apiClient.post<ArsavslutningDto>(
        `/periodeavslutning/${params.ar}/arsavslutning`,
        params.request,
      );
      return data;
    },
    onSuccess: (_data, params) => {
      void queryClient.invalidateQueries({
        queryKey: ['arsavslutning', params.ar],
      });
      void queryClient.invalidateQueries({
        queryKey: ['periodeStatus', params.ar],
      });
    },
  });
}

export function useReverserArsavslutning() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (ar: number) => {
      await apiClient.post(`/periodeavslutning/${ar}/arsavslutning/reverser`);
    },
    onSuccess: (_data, ar) => {
      void queryClient.invalidateQueries({ queryKey: ['arsavslutning', ar] });
      void queryClient.invalidateQueries({ queryKey: ['periodeStatus', ar] });
    },
  });
}

// --- Anleggsmidler ---

export function useAnleggsmidler(aktive?: boolean) {
  return useQuery({
    queryKey: ['anleggsmidler', { aktive }],
    queryFn: async () => {
      const { data } = await apiClient.get<AnleggsmiddelDto[]>('/anleggsmidler', {
        params: { aktive },
      });
      return data;
    },
  });
}

export function useAnleggsmiddel(id: string) {
  return useQuery({
    queryKey: ['anleggsmidler', id],
    queryFn: async () => {
      const { data } = await apiClient.get<AnleggsmiddelDto>(
        `/anleggsmidler/${id}`,
      );
      return data;
    },
    enabled: id.length > 0,
  });
}

export function useOpprettAnleggsmiddel() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OpprettAnleggsmiddelRequest) => {
      const { data } = await apiClient.post<AnleggsmiddelDto>(
        '/anleggsmidler',
        request,
      );
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['anleggsmidler'] });
    },
  });
}

// --- Avskrivninger ---

export function useBeregnAvskrivninger() {
  return useMutation({
    mutationFn: async (request: BeregnAvskrivningerRequest) => {
      const { data } = await apiClient.post<AvskrivningBeregningDto>(
        '/avskrivninger/beregn',
        request,
      );
      return data;
    },
  });
}

export function useBokforAvskrivninger() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: BokforAvskrivningerRequest) => {
      const { data } = await apiClient.post<AvskrivningBeregningDto>(
        '/avskrivninger/bokfor',
        request,
      );
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['anleggsmidler'] });
      void queryClient.invalidateQueries({ queryKey: ['periodeStatus'] });
    },
  });
}

// --- Periodiseringer ---

export function usePeriodiseringer(aktive?: boolean) {
  return useQuery({
    queryKey: ['periodiseringer', { aktive }],
    queryFn: async () => {
      const { data } = await apiClient.get<PeriodiseringDto[]>(
        '/periodiseringer',
        { params: { aktive } },
      );
      return data;
    },
  });
}

export function useOpprettPeriodisering() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: OpprettPeriodiseringRequest) => {
      const { data } = await apiClient.post<PeriodiseringDto>(
        '/periodiseringer',
        request,
      );
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['periodiseringer'] });
    },
  });
}

export function useBokforPeriodiseringer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: BokforPeriodiseringerRequest) => {
      const { data } = await apiClient.post<PeriodiseringBokforingDto>(
        '/periodiseringer/bokfor',
        request,
      );
      return data;
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['periodiseringer'] });
      void queryClient.invalidateQueries({ queryKey: ['periodeStatus'] });
    },
  });
}

// --- Årsregnskapsklargjøring ---

export function useArsregnskapsklargjoring(ar: number) {
  return useQuery({
    queryKey: ['arsregnskapsklargjoring', ar],
    queryFn: async () => {
      const { data } = await apiClient.get<ArsregnskapsklarDto>(
        `/periodeavslutning/${ar}/klargjoring`,
      );
      return data;
    },
    enabled: ar > 0,
  });
}
