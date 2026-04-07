import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useBilagSok } from '../../hooks/api/useBilag';
import { formatBelop, formatDato } from '../../utils/formatering';
import { BilagStatus, BilagStatusNavn } from '../../types/bilag';
import type { BilagSokParams, BilagDto } from '../../types/bilag';

const currentYear = new Date().getFullYear();

function bilagStatus(bilag: BilagDto): BilagStatus {
  if (bilag.erTilbakfort) return BilagStatus.Tilbakfort;
  if (bilag.erBokfort) return BilagStatus.Bokfort;
  return BilagStatus.Kladd;
}

function statusFarge(status: BilagStatus): { bg: string; color: string } {
  switch (status) {
    case BilagStatus.Bokfort:
      return { bg: '#e8f5e9', color: '#2e7d32' };
    case BilagStatus.Tilbakfort:
      return { bg: '#ffebee', color: '#c62828' };
    case BilagStatus.Validert:
      return { bg: '#e3f2fd', color: '#1565c0' };
    case BilagStatus.Kladd:
    default:
      return { bg: '#fff3e0', color: '#e65100' };
  }
}

export default function BilagListePage() {
  const navigate = useNavigate();
  const [sokParams, setSokParams] = useState<BilagSokParams>({
    ar: currentYear,
    side: 1,
    antall: 50,
  });
  const [tekstSok, setTekstSok] = useState('');
  const [kontoSok, setKontoSok] = useState('');
  const [fraDato, setFraDato] = useState('');
  const [tilDato, setTilDato] = useState('');

  const { data: resultat, isLoading, error } = useBilagSok(sokParams);

  function handleSok() {
    setSokParams((prev) => ({
      ...prev,
      beskrivelse: tekstSok || undefined,
      kontonummer: kontoSok || undefined,
      fraDato: fraDato || undefined,
      tilDato: tilDato || undefined,
      side: 1,
    }));
  }

  function handleSideEndring(nySide: number) {
    setSokParams((prev) => ({ ...prev, side: nySide }));
  }

  const totaleSider = resultat ? Math.ceil(resultat.totaltAntall / (sokParams.antall ?? 50)) : 0;

  if (error) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil ved lasting av bilag</h1>
        <p>Kunne ikke hente bilag fra server. Prov igjen senere.</p>
      </div>
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 1400, margin: '0 auto' }}>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: 24,
        }}
      >
        <h1 style={{ margin: 0 }}>Bilag</h1>
        <div style={{ display: 'flex', gap: 8 }}>
          <Link
            to="/bilag/ny"
            style={{
              padding: '8px 16px',
              background: '#0066cc',
              color: '#fff',
              borderRadius: 4,
              textDecoration: 'none',
              fontSize: 14,
              fontWeight: 600,
            }}
          >
            + Nytt bilag
          </Link>
          <Link
            to="/bilag/serier"
            style={{
              padding: '8px 16px',
              background: '#f0f0f0',
              borderRadius: 4,
              textDecoration: 'none',
              fontSize: 14,
              color: '#333',
            }}
          >
            Bilagserier
          </Link>
        </div>
      </div>

      {/* Sok-filter */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '120px 1fr 140px 140px 140px auto',
          gap: 12,
          marginBottom: 16,
          alignItems: 'end',
        }}
      >
        <div>
          <label style={filterLabelStyle}>Ar</label>
          <select
            value={sokParams.ar ?? currentYear}
            onChange={(e) =>
              setSokParams((prev) => ({ ...prev, ar: Number(e.target.value), side: 1 }))
            }
            style={filterInputStyle}
          >
            {Array.from({ length: 5 }, (_, i) => currentYear - 2 + i).map((year) => (
              <option key={year} value={year}>
                {year}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label style={filterLabelStyle}>Beskrivelse / tekst</label>
          <input
            type="text"
            value={tekstSok}
            onChange={(e) => setTekstSok(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleSok()}
            placeholder="Sok i beskrivelse..."
            style={filterInputStyle}
          />
        </div>

        <div>
          <label style={filterLabelStyle}>Konto</label>
          <input
            type="text"
            value={kontoSok}
            onChange={(e) => setKontoSok(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleSok()}
            placeholder="Kontonr"
            style={filterInputStyle}
          />
        </div>

        <div>
          <label style={filterLabelStyle}>Fra dato</label>
          <input
            type="date"
            value={fraDato}
            onChange={(e) => setFraDato(e.target.value)}
            style={filterInputStyle}
          />
        </div>

        <div>
          <label style={filterLabelStyle}>Til dato</label>
          <input
            type="date"
            value={tilDato}
            onChange={(e) => setTilDato(e.target.value)}
            style={filterInputStyle}
          />
        </div>

        <button
          type="button"
          onClick={handleSok}
          style={{
            padding: '8px 20px',
            background: '#0066cc',
            color: '#fff',
            border: 'none',
            borderRadius: 4,
            fontSize: 14,
            cursor: 'pointer',
            height: 38,
          }}
        >
          Sok
        </button>
      </div>

      {/* Resultat */}
      {isLoading ? (
        <p>Laster bilag...</p>
      ) : !resultat || resultat.data.length === 0 ? (
        <div style={{ padding: 24, textAlign: 'center', color: '#666' }}>
          <p>Ingen bilag funnet.</p>
        </div>
      ) : (
        <>
          <div style={{ marginBottom: 8, color: '#666', fontSize: 13 }}>
            Viser {resultat.data.length} av {resultat.totaltAntall} bilag
          </div>
          <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
            <thead>
              <tr>
                <th style={thStyle}>Bilagsnr</th>
                <th style={thStyle}>Serie</th>
                <th style={thStyle}>Dato</th>
                <th style={{ ...thStyle, textAlign: 'left' }}>Beskrivelse</th>
                <th style={thStyle}>Status</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Debet</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Kredit</th>
                <th style={thStyle}>Linjer</th>
              </tr>
            </thead>
            <tbody>
              {resultat.data.map((bilag: BilagDto) => {
                const status = bilagStatus(bilag);
                const farge = statusFarge(status);
                return (
                  <tr
                    key={bilag.id}
                    onClick={() => navigate(`/bilag/${bilag.id}`)}
                    style={{
                      cursor: 'pointer',
                      backgroundColor: bilag.erTilbakfort ? '#fafafa' : '#fff',
                    }}
                    onMouseEnter={(e) =>
                      (e.currentTarget.style.backgroundColor = '#f0f7ff')
                    }
                    onMouseLeave={(e) =>
                      (e.currentTarget.style.backgroundColor = bilag.erTilbakfort
                        ? '#fafafa'
                        : '#fff')
                    }
                  >
                    <td style={{ ...tdStyle, fontFamily: 'monospace', fontWeight: 600 }}>
                      {bilag.bilagsnummer}
                    </td>
                    <td style={{ ...tdStyle, fontFamily: 'monospace', fontSize: 12 }}>
                      {bilag.serieBilagsId ?? bilag.bilagsId}
                    </td>
                    <td style={tdStyle}>{formatDato(bilag.bilagsdato)}</td>
                    <td
                      style={{
                        ...tdStyle,
                        textAlign: 'left',
                        textDecoration: bilag.erTilbakfort ? 'line-through' : 'none',
                        color: bilag.erTilbakfort ? '#999' : 'inherit',
                      }}
                    >
                      {bilag.beskrivelse}
                    </td>
                    <td style={tdStyle}>
                      <span
                        style={{
                          padding: '2px 8px',
                          borderRadius: 12,
                          fontSize: 12,
                          fontWeight: 600,
                          backgroundColor: farge.bg,
                          color: farge.color,
                        }}
                      >
                        {BilagStatusNavn[status]}
                      </span>
                    </td>
                    <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                      {formatBelop(bilag.sumDebet)}
                    </td>
                    <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                      {formatBelop(bilag.sumKredit)}
                    </td>
                    <td style={{ ...tdStyle, textAlign: 'center' }}>
                      {bilag.posteringer.length}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>

          {/* Paginering */}
          {totaleSider > 1 && (
            <div
              style={{
                marginTop: 16,
                display: 'flex',
                justifyContent: 'center',
                gap: 4,
              }}
            >
              <button
                type="button"
                onClick={() => handleSideEndring((sokParams.side ?? 1) - 1)}
                disabled={(sokParams.side ?? 1) <= 1}
                style={pagineringKnappStyle}
              >
                Forrige
              </button>
              <span style={{ padding: '8px 12px', fontSize: 14 }}>
                Side {sokParams.side ?? 1} av {totaleSider}
              </span>
              <button
                type="button"
                onClick={() => handleSideEndring((sokParams.side ?? 1) + 1)}
                disabled={(sokParams.side ?? 1) >= totaleSider}
                style={pagineringKnappStyle}
              >
                Neste
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}

// --- Styles ---

const filterLabelStyle: React.CSSProperties = {
  display: 'block',
  marginBottom: 4,
  fontWeight: 600,
  fontSize: 12,
  color: '#555',
};

const filterInputStyle: React.CSSProperties = {
  width: '100%',
  padding: '8px 10px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  boxSizing: 'border-box',
};

const thStyle: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '2px solid #333',
  textAlign: 'center',
  fontWeight: 700,
  backgroundColor: '#f8f8f8',
  fontSize: 13,
};

const tdStyle: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '1px solid #e0e0e0',
  fontSize: 14,
  textAlign: 'center',
};

const pagineringKnappStyle: React.CSSProperties = {
  padding: '8px 16px',
  border: '1px solid #ccc',
  borderRadius: 4,
  background: '#fff',
  cursor: 'pointer',
  fontSize: 14,
};
