import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useMvaTerminer, useOpprettTerminer } from '../../hooks/api/useMva';
import { MvaTerminStatusNavn } from '../../types/mva';
import type { MvaTerminDto, MvaTerminStatus } from '../../types/mva';
import { formatDato } from '../../utils/formatering';

const currentYear = new Date().getFullYear();

function statusFarge(status: MvaTerminStatus): { backgroundColor: string; color: string } {
  switch (status) {
    case 'Apen':
      return { backgroundColor: '#e8f5e9', color: '#2e7d32' };
    case 'Beregnet':
      return { backgroundColor: '#e3f2fd', color: '#1565c0' };
    case 'Avstemt':
      return { backgroundColor: '#fff3e0', color: '#e65100' };
    case 'Innsendt':
      return { backgroundColor: '#f3e5f5', color: '#7b1fa2' };
    case 'Betalt':
      return { backgroundColor: '#f5f5f5', color: '#616161' };
    default:
      return { backgroundColor: '#f5f5f5', color: '#616161' };
  }
}

export default function MvaOversiktPage() {
  const [ar, setAr] = useState(currentYear);
  const { data: terminer, isLoading, error } = useMvaTerminer(ar);
  const opprettTerminer = useOpprettTerminer();

  function handleGenererTerminer() {
    opprettTerminer.mutate({ ar });
  }

  if (error) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil ved lasting av MVA-terminer</h1>
        <p>Kunne ikke hente MVA-terminer fra server. Pr&oslash;v igjen senere.</p>
      </div>
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: 24,
        }}
      >
        <h1 style={{ margin: 0 }}>MVA-oversikt</h1>
        <div style={{ display: 'flex', gap: 8 }}>
          <Link
            to="/mva/sammenstilling"
            style={{
              padding: '8px 16px',
              background: '#0066cc',
              color: '#fff',
              borderRadius: 4,
              textDecoration: 'none',
              fontSize: 14,
            }}
          >
            Sammenstilling
          </Link>
        </div>
      </div>

      {/* Arsvelger */}
      <div style={{ marginBottom: 24, display: 'flex', alignItems: 'center', gap: 12 }}>
        <label style={{ fontWeight: 600 }}>Regnskaps&aring;r:</label>
        <select
          value={ar}
          onChange={(e) => setAr(Number(e.target.value))}
          style={{
            padding: '8px 12px',
            border: '1px solid #ccc',
            borderRadius: 4,
            fontSize: 14,
          }}
        >
          {Array.from({ length: 5 }, (_, i) => currentYear - 2 + i).map((year) => (
            <option key={year} value={year}>
              {year}
            </option>
          ))}
        </select>
      </div>

      {isLoading ? (
        <p>Laster MVA-terminer...</p>
      ) : !terminer || terminer.length === 0 ? (
        <div
          style={{
            padding: 32,
            textAlign: 'center',
            border: '1px solid #e0e0e0',
            borderRadius: 8,
          }}
        >
          <p style={{ color: '#666', marginBottom: 16 }}>
            Ingen MVA-terminer funnet for {ar}.
          </p>
          <button
            onClick={handleGenererTerminer}
            disabled={opprettTerminer.isPending}
            style={{
              padding: '10px 24px',
              background: '#0066cc',
              color: '#fff',
              border: 'none',
              borderRadius: 4,
              fontSize: 14,
              cursor: 'pointer',
            }}
          >
            {opprettTerminer.isPending ? 'Genererer...' : `Generer terminer for ${ar}`}
          </button>
          {opprettTerminer.isError && (
            <p style={{ color: 'red', marginTop: 8 }}>
              Feil ved generering av terminer. Pr&oslash;v igjen.
            </p>
          )}
        </div>
      ) : (
        <table
          style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}
        >
          <thead>
            <tr>
              <th style={thStyle}>Termin</th>
              <th style={thStyle}>Periode</th>
              <th style={thStyle}>Frist</th>
              <th style={thStyle}>Status</th>
              <th style={thStyle}>Handlinger</th>
            </tr>
          </thead>
          <tbody>
            {terminer.map((termin: MvaTerminDto) => (
              <tr
                key={termin.id}
                style={{
                  backgroundColor: termin.erForfalt ? '#fff8f8' : '#fff',
                }}
              >
                <td style={tdStyle}>
                  <strong>{termin.terminnavn}</strong>
                </td>
                <td style={tdStyle}>
                  {formatDato(termin.fraDato)} &ndash; {formatDato(termin.tilDato)}
                </td>
                <td style={tdStyle}>
                  <span style={{ color: termin.erForfalt ? 'red' : 'inherit' }}>
                    {formatDato(termin.frist)}
                    {termin.erForfalt && ' (forfalt)'}
                  </span>
                </td>
                <td style={tdStyle}>
                  <span
                    style={{
                      padding: '2px 8px',
                      borderRadius: 12,
                      fontSize: 12,
                      fontWeight: 600,
                      ...statusFarge(termin.status),
                    }}
                  >
                    {MvaTerminStatusNavn[termin.status]}
                  </span>
                </td>
                <td style={tdStyle}>
                  <div style={{ display: 'flex', gap: 8 }}>
                    <Link
                      to={`/mva/oppgjor/${termin.id}`}
                      style={{
                        padding: '4px 12px',
                        background: '#e3f2fd',
                        color: '#1565c0',
                        borderRadius: 4,
                        textDecoration: 'none',
                        fontSize: 13,
                      }}
                    >
                      Oppgj&oslash;r
                    </Link>
                    <Link
                      to={`/mva/avstemming/${termin.id}`}
                      style={{
                        padding: '4px 12px',
                        background: '#fff3e0',
                        color: '#e65100',
                        borderRadius: 4,
                        textDecoration: 'none',
                        fontSize: 13,
                      }}
                    >
                      Avstemming
                    </Link>
                    <Link
                      to={`/mva/melding/${termin.id}`}
                      style={{
                        padding: '4px 12px',
                        background: '#f3e5f5',
                        color: '#7b1fa2',
                        borderRadius: 4,
                        textDecoration: 'none',
                        fontSize: 13,
                      }}
                    >
                      Melding
                    </Link>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
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
