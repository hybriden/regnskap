import { useState } from 'react';
import { useHovedboksutskrift } from '../../hooks/api/useRapportering';
import { formatBelop, formatDato } from '../../utils/formatering';
import type { KontoUtskriftDto, PosteringUtskriftDto } from '../../types/rapportering';

const containerStyle: React.CSSProperties = { maxWidth: 1100, margin: '0 auto', padding: 24 };
const filterStyle: React.CSSProperties = { display: 'flex', gap: 16, alignItems: 'flex-end', flexWrap: 'wrap', marginBottom: 24, padding: 16, backgroundColor: '#f8f8f8', borderRadius: 8 };
const feltStyle: React.CSSProperties = { display: 'flex', flexDirection: 'column', gap: 4 };
const inputStyle: React.CSSProperties = { padding: '8px 12px', border: '1px solid #ccc', borderRadius: 4, fontSize: 14 };
const buttonStyle: React.CSSProperties = { padding: '8px 20px', backgroundColor: '#1a5276', color: '#fff', border: 'none', borderRadius: 4, fontSize: 14, cursor: 'pointer' };

const printStyle = `
  @media print {
    body * { visibility: hidden; }
    #rapport-innhold, #rapport-innhold * { visibility: visible; }
    #rapport-innhold { position: absolute; left: 0; top: 0; width: 100%; }
    .no-print { display: none !important; }
  }
`;

const thStyle: React.CSSProperties = { textAlign: 'right', padding: '6px 8px', fontWeight: 700, fontSize: 12 };
const tdStyle: React.CSSProperties = { padding: '3px 8px', fontSize: 12, fontFamily: 'monospace' };

function belopTd(verdi: number, style?: React.CSSProperties) {
  return (
    <td style={{ ...tdStyle, textAlign: 'right', color: verdi < 0 ? 'red' : 'inherit', ...style }}>
      {formatBelop(verdi)}
    </td>
  );
}

function PosteringRad({ postering }: { postering: PosteringUtskriftDto }) {
  return (
    <tr>
      <td style={{ ...tdStyle, fontFamily: 'inherit' }}>{formatDato(postering.dato)}</td>
      <td style={tdStyle}>{postering.bilagsId}</td>
      <td style={{ ...tdStyle, fontFamily: 'inherit', maxWidth: 250, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
        {postering.beskrivelse}
      </td>
      <td style={{ ...tdStyle, textAlign: 'center' }}>{postering.side}</td>
      {belopTd(postering.belop)}
      {belopTd(postering.lopendeSaldo)}
      <td style={{ ...tdStyle, textAlign: 'center' }}>{postering.mvaKode ?? ''}</td>
      <td style={tdStyle}>{postering.motkontonummer ?? ''}</td>
    </tr>
  );
}

function KontoBlokk({ konto }: { konto: KontoUtskriftDto }) {
  const [apen, setApen] = useState(true);

  return (
    <div style={{ marginBottom: 24, border: '1px solid #e0e0e0', borderRadius: 8, overflow: 'hidden' }}>
      <div
        style={{
          padding: '10px 16px',
          backgroundColor: '#1a5276',
          color: '#fff',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          cursor: 'pointer',
        }}
        onClick={() => setApen(!apen)}
      >
        <div>
          <span style={{ fontWeight: 700, fontSize: 15 }}>{konto.kontonummer} – {konto.kontonavn}</span>
          <span style={{ marginLeft: 16, fontSize: 13, opacity: 0.8 }}>
            {konto.kontotype} | {konto.antallPosteringer} posteringer
          </span>
        </div>
        <span style={{ fontSize: 13 }}>{apen ? 'Skjul' : 'Vis'}</span>
      </div>

      {apen && (
        <div style={{ padding: 12 }}>
          <div style={{ display: 'flex', gap: 24, marginBottom: 12, fontSize: 13 }}>
            <span>IB: <strong style={{ fontFamily: 'monospace' }}>{formatBelop(konto.inngaendeBalanse)}</strong></span>
            <span>Sum debet: <strong style={{ fontFamily: 'monospace' }}>{formatBelop(konto.sumDebet)}</strong></span>
            <span>Sum kredit: <strong style={{ fontFamily: 'monospace' }}>{formatBelop(konto.sumKredit)}</strong></span>
            <span>UB: <strong style={{ fontFamily: 'monospace' }}>{formatBelop(konto.utgaendeBalanse)}</strong></span>
          </div>

          {konto.posteringer.length > 0 ? (
            <div style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                <thead>
                  <tr style={{ borderBottom: '2px solid #333' }}>
                    <th style={{ ...thStyle, textAlign: 'left', width: 90 }}>Dato</th>
                    <th style={{ ...thStyle, textAlign: 'left', width: 80 }}>Bilag</th>
                    <th style={{ ...thStyle, textAlign: 'left' }}>Beskrivelse</th>
                    <th style={{ ...thStyle, textAlign: 'center', width: 50 }}>D/K</th>
                    <th style={{ ...thStyle, width: 110 }}>Beløp</th>
                    <th style={{ ...thStyle, width: 110 }}>Saldo</th>
                    <th style={{ ...thStyle, textAlign: 'center', width: 50 }}>MVA</th>
                    <th style={{ ...thStyle, textAlign: 'left', width: 70 }}>Motkonto</th>
                  </tr>
                </thead>
                <tbody>
                  {konto.posteringer.map((postering, idx) => (
                    <PosteringRad key={`${postering.bilagsId}-${postering.linjenummer}-${idx}`} postering={postering} />
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <p style={{ color: '#999', fontSize: 13 }}>Ingen posteringer i valgt periode.</p>
          )}
        </div>
      )}
    </div>
  );
}

export default function HovedbokUtskriftPage() {
  const innevaerendeAr = new Date().getFullYear();
  const [ar, setAr] = useState(innevaerendeAr);
  const [fraPeriode, setFraPeriode] = useState(1);
  const [tilPeriode, setTilPeriode] = useState(12);
  const [fraKonto, setFraKonto] = useState('');
  const [tilKonto, setTilKonto] = useState('');

  const { data, isLoading, error } = useHovedboksutskrift({
    ar,
    fraPeriode,
    tilPeriode,
    fraKonto: fraKonto || undefined,
    tilKonto: tilKonto || undefined,
  });

  return (
    <div style={containerStyle}>
      <style>{printStyle}</style>
      <h1 style={{ margin: 0 }}>Hovedbokutskrift</h1>
      <p style={{ color: '#666', marginTop: 4 }}>Kontospesifikasjon per konto (Bokføringsforskriften 3-1)</p>

      <div style={filterStyle} className="no-print">
        <div style={feltStyle}>
          <label style={{ fontWeight: 600, fontSize: 13 }}>År</label>
          <input type="number" value={ar} onChange={(e) => setAr(Number(e.target.value))} style={{ ...inputStyle, width: 100 }} />
        </div>
        <div style={feltStyle}>
          <label style={{ fontWeight: 600, fontSize: 13 }}>Fra periode</label>
          <select value={fraPeriode} onChange={(e) => setFraPeriode(Number(e.target.value))} style={inputStyle}>
            {Array.from({ length: 12 }, (_, i) => (
              <option key={i + 1} value={i + 1}>{i + 1}</option>
            ))}
          </select>
        </div>
        <div style={feltStyle}>
          <label style={{ fontWeight: 600, fontSize: 13 }}>Til periode</label>
          <select value={tilPeriode} onChange={(e) => setTilPeriode(Number(e.target.value))} style={inputStyle}>
            {Array.from({ length: 12 }, (_, i) => (
              <option key={i + 1} value={i + 1}>{i + 1}</option>
            ))}
          </select>
        </div>
        <div style={feltStyle}>
          <label style={{ fontWeight: 600, fontSize: 13 }}>Fra konto</label>
          <input
            type="text"
            value={fraKonto}
            onChange={(e) => setFraKonto(e.target.value)}
            placeholder="f.eks. 1000"
            style={{ ...inputStyle, width: 100 }}
          />
        </div>
        <div style={feltStyle}>
          <label style={{ fontWeight: 600, fontSize: 13 }}>Til konto</label>
          <input
            type="text"
            value={tilKonto}
            onChange={(e) => setTilKonto(e.target.value)}
            placeholder="f.eks. 9999"
            style={{ ...inputStyle, width: 100 }}
          />
        </div>
        <button style={buttonStyle} className="no-print" onClick={() => window.print()}>Skriv ut</button>
      </div>

      {isLoading && <p>Laster hovedbokutskrift...</p>}
      {error && <p style={{ color: 'red' }}>Feil ved lasting av hovedbokutskrift: {String(error)}</p>}

      {data && (
        <div id="rapport-innhold">
          <div style={{ textAlign: 'center', marginBottom: 16 }}>
            <h2 style={{ margin: 0 }}>Hovedbokutskrift</h2>
            <p style={{ margin: '4px 0', color: '#666' }}>
              Periode {data.fraPeriode}–{data.tilPeriode}, {data.ar}
              {data.kontoer.length > 0 && ` | ${data.kontoer.length} kontoer`}
            </p>
          </div>

          {data.kontoer.length === 0 ? (
            <p style={{ textAlign: 'center', color: '#999' }}>Ingen kontoer funnet for valgt periode og kontoområde.</p>
          ) : (
            data.kontoer.map((konto) => (
              <KontoBlokk key={konto.kontonummer} konto={konto} />
            ))
          )}
        </div>
      )}
    </div>
  );
}
