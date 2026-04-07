import { useState } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import {
  useKjorAvstemming,
  useLukkPeriode,
} from '../../hooks/api/usePeriodeavslutning';
import type {
  AvstemmingResultatDto,
  AvstemmingKontrollDto,
} from '../../types/periodeavslutning';

const periodeNavn: Record<number, string> = {
  1: 'Januar', 2: 'Februar', 3: 'Mars', 4: 'April',
  5: 'Mai', 6: 'Juni', 7: 'Juli', 8: 'August',
  9: 'September', 10: 'Oktober', 11: 'November', 12: 'Desember',
};

type Steg = 'kontroller' | 'gjennomgang' | 'lukking' | 'ferdig';

function kontrollIkon(status: string): string {
  switch (status) {
    case 'OK': return '\u2705';
    case 'ADVARSEL': return '\u26A0\uFE0F';
    case 'FEIL': return '\u274C';
    default: return '\u2753';
  }
}

function kontrollFarge(status: string): string {
  switch (status) {
    case 'OK': return '#2e7d32';
    case 'ADVARSEL': return '#e65100';
    case 'FEIL': return '#c62828';
    default: return '#666';
  }
}

export default function ManedslukkeningPage() {
  const [searchParams] = useSearchParams();
  const ar = Number(searchParams.get('ar')) || new Date().getFullYear();
  const periode = Number(searchParams.get('periode')) || 1;

  const [steg, setSteg] = useState<Steg>('kontroller');
  const [avstemming, setAvstemming] = useState<AvstemmingResultatDto | null>(null);
  const [merknad, setMerknad] = useState('');
  const [tvingLukking, setTvingLukking] = useState(false);
  const [lukkingResultat, setLukkingResultat] = useState<string | null>(null);

  const kjorAvstemming = useKjorAvstemming();
  const lukkPeriode = useLukkPeriode();

  const harFeil = avstemming?.kontroller.some((k) => k.status === 'FEIL') ?? false;
  const harAdvarsler = avstemming?.kontroller.some((k) => k.status === 'ADVARSEL') ?? false;

  function handleKjorKontroller() {
    kjorAvstemming.mutate(
      { ar, periode },
      {
        onSuccess: (data) => {
          setAvstemming(data);
          setSteg('gjennomgang');
        },
      },
    );
  }

  function handleLukkPeriode() {
    lukkPeriode.mutate(
      { ar, periode, request: { merknad: merknad || undefined, tvingLukking } },
      {
        onSuccess: () => {
          setLukkingResultat('OK');
          setSteg('ferdig');
        },
        onError: () => {
          setLukkingResultat('FEIL');
        },
      },
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 900, margin: '0 auto' }}>
      <Link to="/periodeavslutning" style={{ color: '#0066cc', fontSize: 13, textDecoration: 'none' }}>
        &larr; Tilbake til periodeoversikt
      </Link>

      <h1 style={{ marginTop: 12 }}>
        Månedslukking: {periodeNavn[periode] ?? `Periode ${periode}`} {ar}
      </h1>

      {/* Stegindikator */}
      <div style={{ display: 'flex', gap: 4, marginBottom: 32 }}>
        {(['kontroller', 'gjennomgang', 'lukking', 'ferdig'] as Steg[]).map((s, i) => {
          const labels = ['1. Kjør kontroller', '2. Gjennomgå', '3. Lukk', '4. Ferdig'];
          const erAktivt = s === steg;
          const erFullfort = ['kontroller', 'gjennomgang', 'lukking', 'ferdig'].indexOf(steg) > i;
          return (
            <div
              key={s}
              style={{
                flex: 1,
                padding: '10px 16px',
                background: erAktivt ? '#1565c0' : erFullfort ? '#e8f5e9' : '#f5f5f5',
                color: erAktivt ? '#fff' : erFullfort ? '#2e7d32' : '#666',
                borderRadius: 4,
                fontWeight: erAktivt ? 700 : 400,
                fontSize: 14,
                textAlign: 'center',
              }}
            >
              {labels[i]}
            </div>
          );
        })}
      </div>

      {/* Steg 1: Kjør kontroller */}
      {steg === 'kontroller' && (
        <div style={{ textAlign: 'center', padding: 48 }}>
          <p style={{ fontSize: 16, marginBottom: 24 }}>
            Kjør avstemming og kontroller for {periodeNavn[periode]} {ar} før lukking.
          </p>
          <button
            onClick={handleKjorKontroller}
            disabled={kjorAvstemming.isPending}
            style={primaryButtonStyle}
          >
            {kjorAvstemming.isPending ? 'Kjører kontroller...' : 'Kjør kontroller'}
          </button>
          {kjorAvstemming.isError && (
            <p style={{ color: 'red', marginTop: 12 }}>Feil ved kjøring av kontroller.</p>
          )}
        </div>
      )}

      {/* Steg 2: Gjennomgang */}
      {steg === 'gjennomgang' && avstemming && (
        <div>
          <h2 style={{ fontSize: 18, marginBottom: 16 }}>Kontrollresultat</h2>

          <div style={{
            padding: 12,
            marginBottom: 16,
            borderRadius: 8,
            background: avstemming.erKlarForLukking ? '#e8f5e9' : '#ffebee',
            border: `1px solid ${avstemming.erKlarForLukking ? '#a5d6a7' : '#ef9a9a'}`,
          }}>
            <strong>
              {avstemming.erKlarForLukking
                ? 'Perioden er klar for lukking'
                : 'Perioden har problemer som må løses'}
            </strong>
          </div>

          <table style={{ width: '100%', borderCollapse: 'collapse', marginBottom: 24 }}>
            <thead>
              <tr>
                <th style={thStyle}>Status</th>
                <th style={thStyle}>Kontroll</th>
                <th style={thStyle}>Detaljer</th>
              </tr>
            </thead>
            <tbody>
              {avstemming.kontroller.map((k: AvstemmingKontrollDto) => (
                <tr key={k.kode}>
                  <td style={tdStyle}>
                    <span style={{ color: kontrollFarge(k.status), fontWeight: 600 }}>
                      {kontrollIkon(k.status)} {k.status}
                    </span>
                  </td>
                  <td style={tdStyle}>
                    <div><strong>{k.kode}</strong></div>
                    <div style={{ fontSize: 13, color: '#666' }}>{k.beskrivelse}</div>
                  </td>
                  <td style={tdStyle}>
                    {k.detaljer && (
                      <span style={{ fontSize: 13, color: '#666' }}>{k.detaljer}</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {avstemming.advarsler.length > 0 && (
            <div style={{ marginBottom: 24 }}>
              <h3 style={{ fontSize: 16 }}>Advarsler</h3>
              {avstemming.advarsler.map((a, i) => (
                <div key={i} style={{ padding: 8, marginBottom: 4, background: '#fff3e0', borderRadius: 4, fontSize: 13 }}>
                  <strong>{a.kode}</strong> ({a.alvorlighet}): {a.melding}
                </div>
              ))}
            </div>
          )}

          <div style={{ display: 'flex', gap: 12 }}>
            <button onClick={() => setSteg('kontroller')} style={secondaryButtonStyle}>
              Kjør på nytt
            </button>
            {!harFeil && (
              <button onClick={() => setSteg('lukking')} style={primaryButtonStyle}>
                Gå videre til lukking
              </button>
            )}
            {harFeil && (
              <p style={{ color: '#c62828', fontSize: 14, alignSelf: 'center' }}>
                Det finnes feil som må rettes før perioden kan lukkes.
              </p>
            )}
          </div>
        </div>
      )}

      {/* Steg 3: Lukking */}
      {steg === 'lukking' && (
        <div>
          <h2 style={{ fontSize: 18, marginBottom: 16 }}>Bekreft lukking</h2>

          <div style={{ marginBottom: 16 }}>
            <label style={{ display: 'block', fontWeight: 600, marginBottom: 4 }}>
              Merknad (valgfritt):
            </label>
            <textarea
              value={merknad}
              onChange={(e) => setMerknad(e.target.value)}
              style={{ width: '100%', padding: 8, border: '1px solid #ccc', borderRadius: 4, minHeight: 80, fontSize: 14 }}
              placeholder="Eventuelle kommentarer til periodeavslutningen..."
            />
          </div>

          {harAdvarsler && (
            <div style={{ marginBottom: 16 }}>
              <label style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 14 }}>
                <input
                  type="checkbox"
                  checked={tvingLukking}
                  onChange={(e) => setTvingLukking(e.target.checked)}
                />
                Lukk perioden til tross for advarsler
              </label>
            </div>
          )}

          <div style={{
            padding: 16,
            background: '#fff8e1',
            border: '1px solid #ffe082',
            borderRadius: 8,
            marginBottom: 24,
            fontSize: 14,
          }}>
            <strong>Viktig:</strong> Når perioden lukkes, kan ingen flere posteringer legges til i {periodeNavn[periode]} {ar}.
            Korreksjon av feil må skje i neste åpne periode.
          </div>

          <div style={{ display: 'flex', gap: 12 }}>
            <button onClick={() => setSteg('gjennomgang')} style={secondaryButtonStyle}>
              Tilbake
            </button>
            <button
              onClick={handleLukkPeriode}
              disabled={lukkPeriode.isPending || (harAdvarsler && !tvingLukking)}
              style={{
                ...primaryButtonStyle,
                background: '#c62828',
                opacity: (harAdvarsler && !tvingLukking) ? 0.5 : 1,
              }}
            >
              {lukkPeriode.isPending ? 'Lukker...' : 'Lukk periode'}
            </button>
          </div>

          {lukkingResultat === 'FEIL' && (
            <p style={{ color: 'red', marginTop: 12 }}>Feil ved lukking av periode. Prøv igjen.</p>
          )}
        </div>
      )}

      {/* Steg 4: Ferdig */}
      {steg === 'ferdig' && (
        <div style={{ textAlign: 'center', padding: 48 }}>
          <div style={{ fontSize: 48, marginBottom: 16 }}>{'\u2705'}</div>
          <h2 style={{ color: '#2e7d32' }}>
            {periodeNavn[periode]} {ar} er lukket
          </h2>
          <p style={{ color: '#666', marginBottom: 24 }}>
            Perioden er nå lukket og sperret for videre bokføring.
          </p>
          <Link to="/periodeavslutning" style={primaryButtonStyle}>
            Tilbake til oversikt
          </Link>
        </div>
      )}
    </div>
  );
}

const thStyle: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '2px solid #333',
  textAlign: 'left',
  fontWeight: 700,
  backgroundColor: '#f8f8f8',
  fontSize: 14,
};

const tdStyle: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '1px solid #e0e0e0',
  fontSize: 14,
};

const primaryButtonStyle: React.CSSProperties = {
  padding: '10px 24px',
  background: '#0066cc',
  color: '#fff',
  border: 'none',
  borderRadius: 4,
  fontSize: 14,
  cursor: 'pointer',
  textDecoration: 'none',
  display: 'inline-block',
};

const secondaryButtonStyle: React.CSSProperties = {
  padding: '10px 24px',
  background: '#f5f5f5',
  color: '#333',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  cursor: 'pointer',
};
