import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useApnePoster, useGodkjennFaktura, useSperrFaktura, useOpphevSperring } from '../../hooks/api/useLeverandor';
import { formatBelop, formatDato } from '../../utils/formatering';
import { FakturaStatus, FakturaStatusNavn } from '../../types/leverandor';

export default function ApnePostPage() {
  const iDag = new Date().toISOString().slice(0, 10);
  const [dato, setDato] = useState(iDag);

  const { data: rapport, isLoading, error } = useApnePoster(dato);
  const godkjennMutation = useGodkjennFaktura();
  const sperrMutation = useSperrFaktura();
  const opphevMutation = useOpphevSperring();

  const [sperreArsak, setSperreArsak] = useState('');
  const [sperreFakturaId, setSperreFakturaId] = useState<string | null>(null);

  function handleSperr(fakturaId: string) {
    if (!sperreArsak.trim()) return;
    sperrMutation.mutate({ id: fakturaId, arsak: sperreArsak });
    setSperreFakturaId(null);
    setSperreArsak('');
  }

  return (
    <div style={{ padding: 24, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <div>
          <h1 style={{ margin: 0 }}>Apne poster - leverandorer</h1>
          <div style={{ marginTop: 4, fontSize: 13, color: '#666' }}>
            <Link to="/leverandor" style={{ color: '#1565c0' }}>
              Leverandorer
            </Link>{' '}
            / Apne poster
          </div>
        </div>
        <div style={{ display: 'flex', gap: 12, alignItems: 'flex-end' }}>
          <div>
            <label style={labelStil}>Dato</label>
            <input
              type="date"
              value={dato}
              onChange={(e) => setDato(e.target.value)}
              style={inputStil}
            />
          </div>
        </div>
      </div>

      {error && (
        <div style={feilStil}>Feil: {(error as Error).message}</div>
      )}

      {isLoading ? (
        <p>Laster apne poster...</p>
      ) : rapport ? (
        <>
          {/* Sammendrag */}
          <div style={{ ...kortStil, marginBottom: 24 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <span style={{ fontSize: 13, color: '#666' }}>
                  {rapport.leverandorer.length} leverandorer med apne poster per {formatDato(rapport.dato)}
                </span>
              </div>
              <div style={{ textAlign: 'right' }}>
                <div style={{ fontSize: 13, color: '#666' }}>Total gjenstaaende</div>
                <div style={{ fontSize: 24, fontWeight: 700, fontFamily: 'monospace' }}>
                  {formatBelop(rapport.totalGjenstaende)}
                </div>
              </div>
            </div>
          </div>

          {/* Sperr-dialog */}
          {sperreFakturaId && (
            <div style={{ ...kortStil, marginBottom: 16, borderColor: '#ef9a9a' }}>
              <div style={{ display: 'flex', gap: 12, alignItems: 'flex-end' }}>
                <div style={{ flex: 1 }}>
                  <label style={labelStil}>Arsak til sperring</label>
                  <input
                    type="text"
                    value={sperreArsak}
                    onChange={(e) => setSperreArsak(e.target.value)}
                    placeholder="Skriv arsak..."
                    style={fullInputStil}
                    autoFocus
                  />
                </div>
                <button
                  onClick={() => handleSperr(sperreFakturaId)}
                  disabled={!sperreArsak.trim()}
                  style={fareKnappStil}
                >
                  Sperr
                </button>
                <button
                  onClick={() => { setSperreFakturaId(null); setSperreArsak(''); }}
                  style={sekundaerKnappStil}
                >
                  Avbryt
                </button>
              </div>
            </div>
          )}

          {/* Per leverandor */}
          {rapport.leverandorer.map((lev) => (
            <div key={lev.leverandorId} style={{ marginBottom: 24 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', marginBottom: 8 }}>
                <h3 style={{ margin: 0 }}>
                  <Link to={`/leverandor/${lev.leverandorId}`} style={{ color: '#1565c0' }}>
                    {lev.leverandornummer}
                  </Link>{' '}
                  {lev.leverandornavn}
                </h3>
                <span style={{ fontFamily: 'monospace', fontWeight: 700 }}>
                  {formatBelop(lev.sumGjenstaende)}
                </span>
              </div>

              <table style={tabellStil}>
                <thead>
                  <tr>
                    <th style={{ ...headerCelleStil, textAlign: 'left' }}>Fakturanr</th>
                    <th style={headerCelleStil}>Type</th>
                    <th style={headerCelleStil}>Fakturadato</th>
                    <th style={headerCelleStil}>Forfall</th>
                    <th style={{ ...headerCelleStil, textAlign: 'left' }}>Beskrivelse</th>
                    <th style={headerCelleStil}>Opprinnelig</th>
                    <th style={headerCelleStil}>Gjenstaaende</th>
                    <th style={headerCelleStil}>Status</th>
                    <th style={{ ...headerCelleStil, width: 160 }}>Handlinger</th>
                  </tr>
                </thead>
                <tbody>
                  {lev.fakturaer.map((f) => {
                    const erForfalt =
                      new Date(f.forfallsdato) < new Date(dato) && f.gjenstaendeBelop > 0;
                    return (
                      <tr
                        key={f.id}
                        style={{
                          backgroundColor: f.erSperret
                            ? '#fff8e1'
                            : erForfalt
                            ? '#fff3e0'
                            : 'transparent',
                        }}
                      >
                        <td style={{ ...celleStil, textAlign: 'left' }}>
                          {f.eksternFakturanummer}
                        </td>
                        <td style={celleStil}>{f.type}</td>
                        <td style={celleStil}>{formatDato(f.fakturadato)}</td>
                        <td
                          style={{
                            ...celleStil,
                            color: erForfalt ? '#c62828' : 'inherit',
                            fontWeight: erForfalt ? 700 : 400,
                          }}
                        >
                          {formatDato(f.forfallsdato)}
                        </td>
                        <td style={{ ...celleStil, textAlign: 'left' }}>{f.beskrivelse}</td>
                        <td style={{ ...celleStil, fontFamily: 'monospace' }}>
                          {formatBelop(f.belopInklMva)}
                        </td>
                        <td style={{ ...celleStil, fontFamily: 'monospace', fontWeight: 700 }}>
                          {formatBelop(f.gjenstaendeBelop)}
                        </td>
                        <td style={celleStil}>
                          <span
                            style={{
                              padding: '2px 8px',
                              borderRadius: 12,
                              fontSize: 12,
                              fontWeight: 600,
                              backgroundColor: statusFarge(f.status).bg,
                              color: statusFarge(f.status).color,
                              whiteSpace: 'nowrap',
                            }}
                          >
                            {FakturaStatusNavn[f.status]}
                          </span>
                        </td>
                        <td style={{ ...celleStil, whiteSpace: 'nowrap' }}>
                          {f.status === FakturaStatus.Registrert && (
                            <button
                              onClick={() => godkjennMutation.mutate(f.id)}
                              style={miniKnappStil}
                              title="Godkjenn for betaling"
                            >
                              Godkjenn
                            </button>
                          )}
                          {!f.erSperret && f.status !== FakturaStatus.Betalt && (
                            <button
                              onClick={() => setSperreFakturaId(f.id)}
                              style={{ ...miniKnappStil, color: '#c62828' }}
                              title="Sperr for betaling"
                            >
                              Sperr
                            </button>
                          )}
                          {f.erSperret && (
                            <button
                              onClick={() => opphevMutation.mutate(f.id)}
                              style={{ ...miniKnappStil, color: '#2e7d32' }}
                              title="Opphev sperring"
                            >
                              Opphev
                            </button>
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          ))}

          {rapport.leverandorer.length === 0 && (
            <div style={{ textAlign: 'center', color: '#666', padding: 48 }}>
              Ingen apne poster
            </div>
          )}
        </>
      ) : null}
    </div>
  );
}

// --- Hjelper ---

function statusFarge(status: FakturaStatus): { bg: string; color: string } {
  const map: Record<string, { bg: string; color: string }> = {
    Registrert: { bg: '#fff3e0', color: '#e65100' },
    Godkjent: { bg: '#e8f5e9', color: '#2e7d32' },
    IBetalingsforslag: { bg: '#e3f2fd', color: '#1565c0' },
    SendtTilBank: { bg: '#e3f2fd', color: '#1565c0' },
    Betalt: { bg: '#e8f5e9', color: '#1b5e20' },
    DelvisBetalt: { bg: '#fff8e1', color: '#f57f17' },
    Kreditert: { bg: '#fce4ec', color: '#c62828' },
    Sperret: { bg: '#ffebee', color: '#b71c1c' },
  };
  return map[status] ?? { bg: '#f5f5f5', color: '#333' };
}

// --- Stiler ---

const labelStil: React.CSSProperties = {
  display: 'block',
  marginBottom: 4,
  fontWeight: 600,
  fontSize: 13,
};

const inputStil: React.CSSProperties = {
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
};

const fullInputStil: React.CSSProperties = {
  ...inputStil,
  width: '100%',
  boxSizing: 'border-box',
};

const feilStil: React.CSSProperties = {
  padding: 12,
  background: '#ffebee',
  color: '#c62828',
  borderRadius: 4,
  marginBottom: 16,
};

const kortStil: React.CSSProperties = {
  padding: 16,
  border: '1px solid #e0e0e0',
  borderRadius: 8,
  backgroundColor: '#fafafa',
};

const tabellStil: React.CSSProperties = {
  width: '100%',
  borderCollapse: 'collapse',
  border: '1px solid #e0e0e0',
};

const headerCelleStil: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '2px solid #333',
  fontWeight: 700,
  backgroundColor: '#f8f8f8',
  textAlign: 'right',
  fontSize: 13,
};

const celleStil: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '1px solid #e0e0e0',
  textAlign: 'right',
  fontSize: 14,
};

const sekundaerKnappStil: React.CSSProperties = {
  padding: '8px 16px',
  backgroundColor: '#f5f5f5',
  color: '#333',
  border: '1px solid #ccc',
  borderRadius: 4,
  cursor: 'pointer',
  fontSize: 13,
};

const fareKnappStil: React.CSSProperties = {
  padding: '8px 16px',
  backgroundColor: '#c62828',
  color: '#fff',
  border: 'none',
  borderRadius: 4,
  cursor: 'pointer',
  fontSize: 13,
  fontWeight: 600,
};

const miniKnappStil: React.CSSProperties = {
  background: 'none',
  border: 'none',
  cursor: 'pointer',
  fontSize: 12,
  fontWeight: 600,
  color: '#1565c0',
  padding: '2px 6px',
};
