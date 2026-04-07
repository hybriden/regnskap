import { useState } from 'react';
import { usePerioder, useOpprettAr, useEndreStatus } from '../../hooks/api/useHovedbok';
import { formatBelop } from '../../utils/formatering';
import { PeriodeStatus, PeriodeStatusNavn } from '../../types/hovedbok';
import type { PeriodeDto } from '../../types/hovedbok';

const currentYear = new Date().getFullYear();

export default function PeriodePage() {
  const [ar, setAr] = useState(currentYear);

  const { data: perioderResponse, isLoading, error } = usePerioder(ar);
  const opprettAr = useOpprettAr();

  function handleOpprettAr() {
    opprettAr.mutate({ ar });
  }

  const perioder = perioderResponse?.perioder ?? [];

  if (error) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil ved lasting av perioder</h1>
        <p>Kunne ikke hente perioder. Prøv igjen senere.</p>
      </div>
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <h1 style={{ marginBottom: 24 }}>Periodeadministrasjon</h1>

      {/* Årsvelger og opprett */}
      <div style={{ display: 'flex', gap: 16, alignItems: 'end', marginBottom: 24 }}>
        <div>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600 }}>Regnskapsår</label>
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
              <option key={year} value={year}>{year}</option>
            ))}
          </select>
        </div>
        {perioder.length === 0 && !isLoading && (
          <button
            onClick={handleOpprettAr}
            disabled={opprettAr.isPending}
            style={{
              padding: '8px 16px',
              backgroundColor: '#0066cc',
              color: '#fff',
              border: 'none',
              borderRadius: 4,
              fontSize: 14,
              cursor: 'pointer',
            }}
          >
            {opprettAr.isPending ? 'Oppretter...' : `Opprett perioder for ${ar}`}
          </button>
        )}
      </div>

      {opprettAr.isError && (
        <div style={{ padding: 12, backgroundColor: '#ffebee', color: '#c62828', borderRadius: 4, marginBottom: 16 }}>
          Feil ved opprettelse av perioder. Perioder for dette året finnes kanskje allerede.
        </div>
      )}

      {opprettAr.isSuccess && (
        <div style={{ padding: 12, backgroundColor: '#e8f5e9', color: '#2e7d32', borderRadius: 4, marginBottom: 16 }}>
          Perioder for {ar} ble opprettet.
        </div>
      )}

      {/* Periodeliste */}
      {isLoading ? (
        <p>Laster perioder...</p>
      ) : perioder.length === 0 ? (
        <p style={{ color: '#666', textAlign: 'center', padding: 40 }}>
          Ingen perioder funnet for {ar}. Klikk knappen over for å opprette.
        </p>
      ) : (
        <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
          <thead>
            <tr>
              <th style={thStyle}>Periode</th>
              <th style={thStyle}>Fra dato</th>
              <th style={thStyle}>Til dato</th>
              <th style={thStyle}>Status</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Bilag</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Sum debet</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Sum kredit</th>
              <th style={thStyle}>Handlinger</th>
            </tr>
          </thead>
          <tbody>
            {perioder.map((p) => (
              <PeriodeRad key={p.id} periode={p} />
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

function PeriodeRad({ periode }: { periode: PeriodeDto }) {
  const endreStatus = useEndreStatus(periode.ar, periode.periode);
  const [visMerknad, setVisMerknad] = useState(false);
  const [merknad, setMerknad] = useState('');

  function handleSperr() {
    endreStatus.mutate({ nyStatus: PeriodeStatus.Sperret, merknad: merknad || undefined });
    setVisMerknad(false);
    setMerknad('');
  }

  function handleAapne() {
    endreStatus.mutate({ nyStatus: PeriodeStatus.Apen });
  }

  function handleLukk() {
    const bekreft = window.confirm(
      `Er du sikker på at du vil lukke ${periode.periodenavn}? Denne handlingen kan IKKE reverseres.`,
    );
    if (!bekreft) return;
    endreStatus.mutate({ nyStatus: PeriodeStatus.Lukket, merknad: merknad || undefined });
    setVisMerknad(false);
    setMerknad('');
  }

  return (
    <>
      <tr style={{ backgroundColor: periode.status === 'Lukket' ? '#f5f5f5' : '#fff' }}>
        <td style={tdStyle}>{periode.periodenavn}</td>
        <td style={tdStyle}>{periode.fraDato}</td>
        <td style={tdStyle}>{periode.tilDato}</td>
        <td style={tdStyle}>
          <span
            style={{
              padding: '2px 8px',
              borderRadius: 12,
              fontSize: 12,
              fontWeight: 600,
              backgroundColor:
                periode.status === 'Apen' ? '#e8f5e9' :
                periode.status === 'Sperret' ? '#fff3e0' : '#f5f5f5',
              color:
                periode.status === 'Apen' ? '#2e7d32' :
                periode.status === 'Sperret' ? '#e65100' : '#616161',
            }}
          >
            {PeriodeStatusNavn[periode.status]}
          </span>
        </td>
        <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>{periode.antallBilag}</td>
        <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>{formatBelop(periode.sumDebet)}</td>
        <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>{formatBelop(periode.sumKredit)}</td>
        <td style={tdStyle}>
          <div style={{ display: 'flex', gap: 4 }}>
            {periode.status === 'Apen' && (
              <button
                onClick={() => setVisMerknad(!visMerknad)}
                disabled={endreStatus.isPending}
                style={btnStyle('#ff9800')}
              >
                Sperr
              </button>
            )}
            {periode.status === 'Sperret' && (
              <>
                <button
                  onClick={handleAapne}
                  disabled={endreStatus.isPending}
                  style={btnStyle('#4caf50')}
                >
                  Åpne
                </button>
                <button
                  onClick={() => setVisMerknad(!visMerknad)}
                  disabled={endreStatus.isPending}
                  style={btnStyle('#f44336')}
                >
                  Lukk
                </button>
              </>
            )}
            {periode.status === 'Lukket' && (
              <span style={{ color: '#999', fontSize: 12 }}>
                Lukket{periode.lukketTidspunkt ? ` ${new Date(periode.lukketTidspunkt).toLocaleDateString('nb-NO')}` : ''}
              </span>
            )}
          </div>
          {endreStatus.isError && (
            <div style={{ color: 'red', fontSize: 12, marginTop: 4 }}>
              Feil ved endring av status.
            </div>
          )}
        </td>
      </tr>
      {visMerknad && (
        <tr>
          <td colSpan={8} style={{ padding: '8px 12px', backgroundColor: '#fafafa' }}>
            <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
              <input
                type="text"
                placeholder="Merknad (valgfritt)"
                value={merknad}
                onChange={(e) => setMerknad(e.target.value)}
                style={{
                  flex: 1,
                  padding: '6px 10px',
                  border: '1px solid #ccc',
                  borderRadius: 4,
                  fontSize: 14,
                }}
              />
              {periode.status === 'Apen' && (
                <button
                  onClick={handleSperr}
                  disabled={endreStatus.isPending}
                  style={btnStyle('#ff9800')}
                >
                  {endreStatus.isPending ? 'Sperrer...' : 'Bekreft sperring'}
                </button>
              )}
              {periode.status === 'Sperret' && (
                <button
                  onClick={handleLukk}
                  disabled={endreStatus.isPending}
                  style={btnStyle('#f44336')}
                >
                  {endreStatus.isPending ? 'Lukker...' : 'Bekreft lukking'}
                </button>
              )}
              <button
                onClick={() => { setVisMerknad(false); setMerknad(''); }}
                style={btnStyle('#999')}
              >
                Avbryt
              </button>
            </div>
          </td>
        </tr>
      )}
    </>
  );
}

function btnStyle(bg: string): React.CSSProperties {
  return {
    padding: '4px 10px',
    backgroundColor: bg,
    color: '#fff',
    border: 'none',
    borderRadius: 4,
    fontSize: 12,
    cursor: 'pointer',
    fontWeight: 600,
  };
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
