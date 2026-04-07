import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAldersfordeling } from '../../hooks/api/useKunde';
import { formatBelop, formatDato } from '../../utils/formatering';

const cellStyle: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '1px solid #e0e0e0',
  fontSize: 14,
  textAlign: 'right',
  fontFamily: 'monospace',
};

const headerStyle: React.CSSProperties = {
  ...cellStyle,
  fontWeight: 700,
  borderBottom: '2px solid #333',
  backgroundColor: '#f8f8f8',
};

const sumCellStyle: React.CSSProperties = {
  ...cellStyle,
  fontWeight: 700,
  borderTop: '2px solid #333',
  borderBottom: 'none',
  backgroundColor: '#f8f8f8',
};

function belopEllerDash(verdi: number) {
  return verdi !== 0 ? formatBelop(verdi) : '-';
}

export default function AldersfordelingPage() {
  const [dato, setDato] = useState(new Date().toISOString().slice(0, 10));
  const { data: rapport, isLoading, error } = useAldersfordeling(dato);

  if (error) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil</h1>
        <p>Kunne ikke hente aldersfordeling.</p>
      </div>
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ marginBottom: 16 }}>
        <Link to="/kunde" style={{ color: '#0066cc', textDecoration: 'none', fontSize: 14 }}>
          Tilbake til kundeliste
        </Link>
      </div>

      <h1 style={{ marginBottom: 24 }}>Aldersfordeling - Kundereskontro</h1>

      <div style={{ marginBottom: 24, display: 'flex', gap: 12, alignItems: 'center' }}>
        <label style={{ fontWeight: 600, fontSize: 14 }}>Dato:</label>
        <input
          type="date"
          value={dato}
          onChange={(e) => setDato(e.target.value)}
          style={{ padding: '8px 12px', border: '1px solid #ccc', borderRadius: 4 }}
        />
        {rapport && (
          <span style={{ fontSize: 14, color: '#666' }}>
            Rapport per {formatDato(rapport.dato)}
          </span>
        )}
      </div>

      {isLoading && <p>Laster aldersfordeling...</p>}

      {rapport && (
        <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
          <thead>
            <tr>
              <th style={{ ...headerStyle, textAlign: 'left' }}>Kundenr</th>
              <th style={{ ...headerStyle, textAlign: 'left' }}>Kunde</th>
              <th style={headerStyle}>Ikke forfalt</th>
              <th style={headerStyle}>0-30 dager</th>
              <th style={headerStyle}>31-60 dager</th>
              <th style={headerStyle}>61-90 dager</th>
              <th style={headerStyle}>Over 90 dager</th>
              <th style={headerStyle}>Totalt</th>
            </tr>
          </thead>
          <tbody>
            {rapport.kunder.length === 0 && (
              <tr>
                <td
                  colSpan={8}
                  style={{ ...cellStyle, textAlign: 'center', fontStyle: 'italic', fontFamily: 'inherit' }}
                >
                  Ingen åpne poster
                </td>
              </tr>
            )}
            {rapport.kunder.map((k, i) => (
              <tr key={k.kundeId} style={{ backgroundColor: i % 2 === 0 ? '#fff' : '#fafafa' }}>
                <td style={{ ...cellStyle, textAlign: 'left' }}>
                  <Link
                    to={`/kunde/${k.kundeId}`}
                    style={{ color: '#0066cc', textDecoration: 'none' }}
                  >
                    {k.kundenummer}
                  </Link>
                </td>
                <td style={{ ...cellStyle, textAlign: 'left', fontFamily: 'inherit' }}>{k.navn}</td>
                <td style={cellStyle}>{belopEllerDash(k.ikkeForfalt)}</td>
                <td style={{ ...cellStyle, color: k.dager0Til30 > 0 ? '#e65100' : 'inherit' }}>
                  {belopEllerDash(k.dager0Til30)}
                </td>
                <td style={{ ...cellStyle, color: k.dager31Til60 > 0 ? '#e65100' : 'inherit' }}>
                  {belopEllerDash(k.dager31Til60)}
                </td>
                <td style={{ ...cellStyle, color: k.dager61Til90 > 0 ? '#c62828' : 'inherit' }}>
                  {belopEllerDash(k.dager61Til90)}
                </td>
                <td
                  style={{
                    ...cellStyle,
                    color: k.over90Dager > 0 ? '#b71c1c' : 'inherit',
                    fontWeight: k.over90Dager > 0 ? 700 : 400,
                  }}
                >
                  {belopEllerDash(k.over90Dager)}
                </td>
                <td style={{ ...cellStyle, fontWeight: 600 }}>{formatBelop(k.totalt)}</td>
              </tr>
            ))}
          </tbody>
          {rapport.kunder.length > 0 && (
            <tfoot>
              <tr>
                <td style={{ ...sumCellStyle, textAlign: 'left', fontFamily: 'inherit' }} colSpan={2}>
                  Totalt
                </td>
                <td style={sumCellStyle}>{formatBelop(rapport.totalt.ikkeForfalt)}</td>
                <td style={sumCellStyle}>{formatBelop(rapport.totalt.dager0Til30)}</td>
                <td style={sumCellStyle}>{formatBelop(rapport.totalt.dager31Til60)}</td>
                <td style={sumCellStyle}>{formatBelop(rapport.totalt.dager61Til90)}</td>
                <td style={sumCellStyle}>{formatBelop(rapport.totalt.over90Dager)}</td>
                <td style={sumCellStyle}>{formatBelop(rapport.totalt.totalt)}</td>
              </tr>
            </tfoot>
          )}
        </table>
      )}
    </div>
  );
}
