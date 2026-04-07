import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import BilagsEditor from '../../components/BilagsEditor';
import { useOpprettBilag } from '../../hooks/api/useBilag';
import { BokforingSide } from '../../types/hovedbok';
import type { BilagsEditorData } from '../../components/BilagsEditor';
import type { OpprettBilagRequest, PosteringLinjeRequest } from '../../types/bilag';
import type { BilagType } from '../../types/hovedbok';

export default function NyttBilagPage() {
  const navigate = useNavigate();
  const opprettBilag = useOpprettBilag();
  const [valideringsfeil, setValideringsfeil] = useState<string[]>([]);

  function valider(data: BilagsEditorData): string[] {
    const feil: string[] = [];
    if (!data.dato) feil.push('Bilagsdato er pakrevd.');
    if (!data.beskrivelse.trim()) feil.push('Beskrivelse er pakrevd.');

    const linjerMedData = data.linjer.filter(
      (l) => l.kontonummer.length > 0 && (l.debet > 0 || l.kredit > 0),
    );
    if (linjerMedData.length < 2) {
      feil.push('Et bilag ma ha minimum 2 posteringslinjer med konto og belop.');
    }

    const sumDebet = linjerMedData.reduce((s, l) => s + l.debet, 0);
    const sumKredit = linjerMedData.reduce((s, l) => s + l.kredit, 0);
    const diff = Math.round((sumDebet - sumKredit) * 100) / 100;
    if (diff !== 0) {
      feil.push(`Bilaget er ikke i balanse. Differanse: ${diff.toFixed(2)}`);
    }

    for (let i = 0; i < linjerMedData.length; i++) {
      const l = linjerMedData[i];
      if (l.debet > 0 && l.kredit > 0) {
        feil.push(`Linje ${i + 1}: En linje kan ikke ha bade debet og kredit.`);
      }
      if (l.debet === 0 && l.kredit === 0) {
        feil.push(`Linje ${i + 1}: Belop ma vaere storre enn 0.`);
      }
    }

    return feil;
  }

  function handleLagre(data: BilagsEditorData) {
    const feil = valider(data);
    setValideringsfeil(feil);
    if (feil.length > 0) return;

    const linjerMedData = data.linjer.filter(
      (l) => l.kontonummer.length > 0 && (l.debet > 0 || l.kredit > 0),
    );

    const posteringer: PosteringLinjeRequest[] = linjerMedData.map((l) => ({
      kontonummer: l.kontonummer,
      side: l.debet > 0 ? BokforingSide.Debet : BokforingSide.Kredit,
      belop: l.debet > 0 ? l.debet : l.kredit,
      beskrivelse: l.beskrivelse || data.beskrivelse,
      mvaKode: l.mvaKode || null,
    }));

    const request: OpprettBilagRequest = {
      type: data.bilagType as BilagType,
      bilagsdato: data.dato,
      beskrivelse: data.beskrivelse,
      eksternReferanse: data.eksternReferanse || null,
      serieKode: data.serieKode || null,
      posteringer,
      bokforDirekte: true,
    };

    opprettBilag.mutate(request, {
      onSuccess: (bilag) => {
        navigate(`/bilag/${bilag.id}`);
      },
      onError: (error) => {
        if (error && typeof error === 'object' && 'response' in error) {
          const axiosError = error as { response?: { data?: { melding?: string; title?: string } } };
          const melding =
            axiosError.response?.data?.melding ??
            axiosError.response?.data?.title ??
            'Ukjent feil ved opprettelse av bilag.';
          setValideringsfeil([melding]);
        } else {
          setValideringsfeil(['Kunne ikke opprette bilag. Prov igjen.']);
        }
      },
    });
  }

  return (
    <div style={{ padding: 24, maxWidth: 1400, margin: '0 auto' }}>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 16,
          marginBottom: 24,
        }}
      >
        <Link
          to="/bilag"
          style={{
            color: '#0066cc',
            textDecoration: 'none',
            fontSize: 14,
          }}
        >
          Bilag
        </Link>
        <span style={{ color: '#999' }}>/</span>
        <h1 style={{ margin: 0 }}>Nytt bilag</h1>
      </div>

      <BilagsEditor
        onLagre={handleLagre}
        onAvbryt={() => navigate('/bilag')}
        laster={opprettBilag.isPending}
        valideringsfeil={valideringsfeil}
      />
    </div>
  );
}
