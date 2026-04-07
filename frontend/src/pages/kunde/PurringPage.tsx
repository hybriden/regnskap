import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  usePurreforslag,
  useOpprettPurringer,
  useSendPurring,
  usePurringer,
} from '../../hooks/api/useKunde';
import { PurringType, PurringTypeNavn } from '../../types/kunde';
import type { PurreforslagRequest, PurreforslagDto } from '../../types/kunde';
import { formatBelop, formatDato } from '../../utils/formatering';

const cellStyle: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '1px solid #e0e0e0',
  fontSize: 14,
};

const headerStyle: React.CSSProperties = {
  ...cellStyle,
  fontWeight: 700,
  borderBottom: '2px solid #333',
  backgroundColor: '#f8f8f8',
  textAlign: 'left',
};

type ViewMode = 'forslag' | 'historikk';

export default function PurringPage() {
  const [viewMode, setViewMode] = useState<ViewMode>('forslag');
  const [melding, setMelding] = useState<{ type: 'ok' | 'feil'; tekst: string } | null>(null);

  // Forslagsparametre
  const [forslagParams, setForslagParams] = useState<PurreforslagRequest>({
    dato: new Date().toISOString().slice(0, 10),
    minimumDagerForfalt: 14,
    inkluderPurring1: true,
    inkluderPurring2: true,
    inkluderPurring3: true,
  });
  const [hentForslag, setHentForslag] = useState(false);
  const [valgteForslag, setValgteForslag] = useState<Set<string>>(new Set());

  const { data: forslag, isLoading: forslagLoading } = usePurreforslag(
    hentForslag ? forslagParams : null,
  );
  const { data: purringer } = usePurringer();
  const opprettPurringer = useOpprettPurringer();
  const sendPurring = useSendPurring();

  function handleGenererForslag(e: React.FormEvent) {
    e.preventDefault();
    setHentForslag(true);
    setValgteForslag(new Set());
  }

  function toggleForslag(fakturaId: string) {
    setValgteForslag((prev) => {
      const neste = new Set(prev);
      if (neste.has(fakturaId)) {
        neste.delete(fakturaId);
      } else {
        neste.add(fakturaId);
      }
      return neste;
    });
  }

  function velgAlle() {
    if (!forslag) return;
    if (valgteForslag.size === forslag.length) {
      setValgteForslag(new Set());
    } else {
      setValgteForslag(new Set(forslag.map((f) => f.fakturaId)));
    }
  }

  function handleOpprettPurringer() {
    if (valgteForslag.size === 0 || !forslag) return;
    // Group by type and create purringer for each group
    const valgte = forslag.filter((f) => valgteForslag.has(f.fakturaId));
    const forsteType = valgte[0]?.foreslattType;
    if (!forsteType) return;

    opprettPurringer.mutate(
      {
        fakturaIder: valgte.map((f) => f.fakturaId),
        type: forsteType,
      },
      {
        onSuccess: (resultat) => {
          setMelding({
            type: 'ok',
            tekst: `${resultat.length} purringer opprettet.`,
          });
          setValgteForslag(new Set());
          setHentForslag(false);
        },
        onError: () => {
          setMelding({ type: 'feil', tekst: 'Feil ved opprettelse av purringer.' });
        },
      },
    );
  }

  function handleSendPurring(purringId: string) {
    sendPurring.mutate(
      { id: purringId, sendemetode: 'Epost' },
      {
        onSuccess: () => {
          setMelding({ type: 'ok', tekst: 'Purring markert som sendt.' });
        },
        onError: () => {
          setMelding({ type: 'feil', tekst: 'Feil ved sending av purring.' });
        },
      },
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ marginBottom: 16 }}>
        <Link to="/kunde" style={{ color: '#0066cc', textDecoration: 'none', fontSize: 14 }}>
          Tilbake til kundeliste
        </Link>
      </div>

      <h1 style={{ marginBottom: 24 }}>Purring</h1>

      {melding && (
        <div
          style={{
            padding: 12,
            marginBottom: 16,
            borderRadius: 4,
            backgroundColor: melding.type === 'ok' ? '#e8f5e9' : '#ffebee',
            color: melding.type === 'ok' ? '#2e7d32' : '#c62828',
            border: `1px solid ${melding.type === 'ok' ? '#a5d6a7' : '#ef9a9a'}`,
          }}
        >
          {melding.tekst}
        </div>
      )}

      {/* Modus */}
      <div style={{ marginBottom: 24, display: 'flex', gap: 8 }}>
        <button
          onClick={() => setViewMode('forslag')}
          style={{
            padding: '10px 20px',
            background: viewMode === 'forslag' ? '#0066cc' : '#f5f5f5',
            color: viewMode === 'forslag' ? '#fff' : '#333',
            border: viewMode === 'forslag' ? 'none' : '1px solid #ccc',
            borderRadius: 4,
            cursor: 'pointer',
            fontWeight: viewMode === 'forslag' ? 700 : 400,
          }}
        >
          Generer purreforslag
        </button>
        <button
          onClick={() => setViewMode('historikk')}
          style={{
            padding: '10px 20px',
            background: viewMode === 'historikk' ? '#0066cc' : '#f5f5f5',
            color: viewMode === 'historikk' ? '#fff' : '#333',
            border: viewMode === 'historikk' ? 'none' : '1px solid #ccc',
            borderRadius: 4,
            cursor: 'pointer',
            fontWeight: viewMode === 'historikk' ? 700 : 400,
          }}
        >
          Purrehistorikk
        </button>
      </div>

      {/* Purreforslag */}
      {viewMode === 'forslag' && (
        <>
          <form
            onSubmit={handleGenererForslag}
            style={{
              padding: 16,
              border: '1px solid #e0e0e0',
              borderRadius: 4,
              backgroundColor: '#fafafa',
              marginBottom: 16,
            }}
          >
            <h3 style={{ marginTop: 0 }}>Parametre</h3>
            <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', alignItems: 'end' }}>
              <div>
                <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                  Purringsdato
                </label>
                <input
                  type="date"
                  value={forslagParams.dato}
                  onChange={(e) => setForslagParams({ ...forslagParams, dato: e.target.value })}
                  style={{ padding: '8px 10px', border: '1px solid #ccc', borderRadius: 4 }}
                />
              </div>
              <div>
                <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                  Min. dager forfalt
                </label>
                <input
                  type="number"
                  value={forslagParams.minimumDagerForfalt}
                  onChange={(e) =>
                    setForslagParams({
                      ...forslagParams,
                      minimumDagerForfalt: Number(e.target.value),
                    })
                  }
                  min={1}
                  style={{ padding: '8px 10px', border: '1px solid #ccc', borderRadius: 4, width: 80 }}
                />
              </div>
              <label style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: 13 }}>
                <input
                  type="checkbox"
                  checked={forslagParams.inkluderPurring1}
                  onChange={(e) =>
                    setForslagParams({ ...forslagParams, inkluderPurring1: e.target.checked })
                  }
                />
                1. purring
              </label>
              <label style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: 13 }}>
                <input
                  type="checkbox"
                  checked={forslagParams.inkluderPurring2}
                  onChange={(e) =>
                    setForslagParams({ ...forslagParams, inkluderPurring2: e.target.checked })
                  }
                />
                2. purring
              </label>
              <label style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: 13 }}>
                <input
                  type="checkbox"
                  checked={forslagParams.inkluderPurring3}
                  onChange={(e) =>
                    setForslagParams({ ...forslagParams, inkluderPurring3: e.target.checked })
                  }
                />
                3. purring
              </label>
              <button
                type="submit"
                style={{
                  padding: '10px 20px',
                  background: '#0066cc',
                  color: '#fff',
                  border: 'none',
                  borderRadius: 4,
                  cursor: 'pointer',
                  fontWeight: 600,
                }}
              >
                Generer forslag
              </button>
            </div>
          </form>

          {forslagLoading && <p>Genererer purreforslag...</p>}

          {forslag && forslag.length > 0 && (
            <>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                <h3>Purreforslag ({forslag.length} fakturaer)</h3>
                <button
                  onClick={handleOpprettPurringer}
                  disabled={valgteForslag.size === 0 || opprettPurringer.isPending}
                  style={{
                    padding: '8px 20px',
                    background: valgteForslag.size > 0 ? '#2e7d32' : '#ccc',
                    color: '#fff',
                    border: 'none',
                    borderRadius: 4,
                    cursor: valgteForslag.size > 0 ? 'pointer' : 'default',
                    fontWeight: 600,
                  }}
                >
                  {opprettPurringer.isPending
                    ? 'Oppretter...'
                    : `Opprett purringer (${valgteForslag.size})`}
                </button>
              </div>

              <table
                style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}
              >
                <thead>
                  <tr>
                    <th style={{ ...headerStyle, width: 40 }}>
                      <input
                        type="checkbox"
                        checked={valgteForslag.size === forslag.length && forslag.length > 0}
                        onChange={velgAlle}
                      />
                    </th>
                    <th style={headerStyle}>Kundenr</th>
                    <th style={headerStyle}>Kunde</th>
                    <th style={headerStyle}>Fakturanr</th>
                    <th style={headerStyle}>Forfallsdato</th>
                    <th style={headerStyle}>Dager forfalt</th>
                    <th style={{ ...headerStyle, textAlign: 'right' }}>Gjenstående</th>
                    <th style={headerStyle}>Type</th>
                    <th style={{ ...headerStyle, textAlign: 'right' }}>Gebyr</th>
                    <th style={{ ...headerStyle, textAlign: 'right' }}>Rente</th>
                  </tr>
                </thead>
                <tbody>
                  {forslag.map((f: PurreforslagDto, i: number) => (
                    <tr
                      key={f.fakturaId}
                      style={{
                        backgroundColor: valgteForslag.has(f.fakturaId)
                          ? '#e3f2fd'
                          : i % 2 === 0
                            ? '#fff'
                            : '#fafafa',
                      }}
                    >
                      <td style={cellStyle}>
                        <input
                          type="checkbox"
                          checked={valgteForslag.has(f.fakturaId)}
                          onChange={() => toggleForslag(f.fakturaId)}
                        />
                      </td>
                      <td style={{ ...cellStyle, fontFamily: 'monospace' }}>{f.kundenummer}</td>
                      <td style={cellStyle}>{f.kundenavn}</td>
                      <td style={{ ...cellStyle, fontFamily: 'monospace' }}>{f.fakturanummer}</td>
                      <td style={cellStyle}>{formatDato(f.forfallsdato)}</td>
                      <td style={{ ...cellStyle, color: '#c62828', fontWeight: 600 }}>
                        {f.dagerForfalt} dager
                      </td>
                      <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                        {formatBelop(f.gjenstaendeBelop)}
                      </td>
                      <td style={cellStyle}>{PurringTypeNavn[f.foreslattType]}</td>
                      <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                        {f.gebyr > 0 ? formatBelop(f.gebyr) : '-'}
                      </td>
                      <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                        {f.forsinkelsesrente > 0 ? formatBelop(f.forsinkelsesrente) : '-'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          )}

          {forslag && forslag.length === 0 && hentForslag && (
            <p style={{ fontStyle: 'italic', color: '#666' }}>
              Ingen fakturaer å purre med valgte parametre.
            </p>
          )}
        </>
      )}

      {/* Purrehistorikk */}
      {viewMode === 'historikk' && (
        <>
          <h3>Purrehistorikk</h3>
          <table
            style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}
          >
            <thead>
              <tr>
                <th style={headerStyle}>Kundenr</th>
                <th style={headerStyle}>Kunde</th>
                <th style={headerStyle}>Fakturanr</th>
                <th style={headerStyle}>Type</th>
                <th style={headerStyle}>Dato</th>
                <th style={headerStyle}>Ny frist</th>
                <th style={{ ...headerStyle, textAlign: 'right' }}>Gebyr</th>
                <th style={{ ...headerStyle, textAlign: 'right' }}>Rente</th>
                <th style={{ ...headerStyle, textAlign: 'center' }}>Sendt</th>
                <th style={headerStyle}></th>
              </tr>
            </thead>
            <tbody>
              {(!purringer || purringer.length === 0) && (
                <tr>
                  <td colSpan={10} style={{ ...cellStyle, textAlign: 'center', fontStyle: 'italic' }}>
                    Ingen purringer registrert
                  </td>
                </tr>
              )}
              {(purringer ?? []).map((p, i) => (
                <tr key={p.id} style={{ backgroundColor: i % 2 === 0 ? '#fff' : '#fafafa' }}>
                  <td style={{ ...cellStyle, fontFamily: 'monospace' }}>{p.kundenummer}</td>
                  <td style={cellStyle}>{p.kundenavn}</td>
                  <td style={{ ...cellStyle, fontFamily: 'monospace' }}>{p.fakturanummer}</td>
                  <td style={cellStyle}>{PurringTypeNavn[p.type]}</td>
                  <td style={cellStyle}>{formatDato(p.purringsdato)}</td>
                  <td style={cellStyle}>{formatDato(p.nyForfallsdato)}</td>
                  <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                    {p.gebyr > 0 ? formatBelop(p.gebyr) : '-'}
                  </td>
                  <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                    {p.forsinkelsesrente > 0 ? formatBelop(p.forsinkelsesrente) : '-'}
                  </td>
                  <td style={{ ...cellStyle, textAlign: 'center' }}>
                    {p.erSendt ? (
                      <span style={{ color: '#2e7d32' }}>
                        Ja ({p.sendemetode})
                      </span>
                    ) : (
                      <span style={{ color: '#666' }}>Nei</span>
                    )}
                  </td>
                  <td style={cellStyle}>
                    {!p.erSendt && (
                      <button
                        onClick={() => handleSendPurring(p.id)}
                        disabled={sendPurring.isPending}
                        style={{
                          padding: '4px 12px',
                          background: '#0066cc',
                          color: '#fff',
                          border: 'none',
                          borderRadius: 4,
                          cursor: 'pointer',
                          fontSize: 12,
                        }}
                      >
                        Send
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}
    </div>
  );
}
