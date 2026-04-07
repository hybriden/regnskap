import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useApnePoster } from '../../hooks/api/useKunde';
import { KundeFakturaStatusNavn } from '../../types/kunde';
import type { KundeFakturaStatus } from '../../types/kunde';
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

const sumCellStyle: React.CSSProperties = {
  ...cellStyle,
  fontWeight: 700,
  borderTop: '2px solid #333',
  borderBottom: 'none',
  backgroundColor: '#f8f8f8',
};

function statusFarge(status: KundeFakturaStatus): { backgroundColor: string; color: string } {
  switch (status) {
    case 'Utstedt':
      return { backgroundColor: '#e3f2fd', color: '#1565c0' };
    case 'DelvisBetalt':
      return { backgroundColor: '#fff3e0', color: '#e65100' };
    case 'Purring1':
    case 'Purring2':
    case 'Purring3':
      return { backgroundColor: '#fff8e1', color: '#f57f17' };
    case 'Inkasso':
      return { backgroundColor: '#fce4ec', color: '#c62828' };
    default:
      return { backgroundColor: '#f5f5f5', color: '#616161' };
  }
}

export default function ApnePostPage() {
  const [dato, setDato] = useState<string>('');
  const { data: poster, isLoading, error } = useApnePoster(dato || undefined);

  const sumGjenstaaende = (poster ?? []).reduce((sum, p) => sum + p.gjenstaendeBelop, 0);
  const sumBelop = (poster ?? []).reduce((sum, p) => sum + p.belopInklMva, 0);
  const antallForfalt = (poster ?? []).filter((p) => p.erForfalt).length;

  if (error) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil</h1>
        <p>Kunne ikke hente åpne poster.</p>
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

      <h1 style={{ marginBottom: 24 }}>Åpne poster - Kundereskontro</h1>

      <div style={{ marginBottom: 16, display: 'flex', gap: 12, alignItems: 'center' }}>
        <label style={{ fontWeight: 600, fontSize: 14 }}>Per dato (valgfritt):</label>
        <input
          type="date"
          value={dato}
          onChange={(e) => setDato(e.target.value)}
          style={{ padding: '8px 12px', border: '1px solid #ccc', borderRadius: 4 }}
        />
        {dato && (
          <button
            onClick={() => setDato('')}
            style={{
              padding: '8px 12px',
              background: '#f5f5f5',
              border: '1px solid #ccc',
              borderRadius: 4,
              cursor: 'pointer',
              fontSize: 13,
            }}
          >
            Fjern datofilter
          </button>
        )}
      </div>

      {/* Sammendrag */}
      {poster && poster.length > 0 && (
        <div
          style={{
            display: 'flex',
            gap: 24,
            marginBottom: 16,
            padding: 16,
            border: '1px solid #e0e0e0',
            borderRadius: 4,
            backgroundColor: '#fafafa',
          }}
        >
          <div>
            <div style={{ fontSize: 13, color: '#666' }}>Antall åpne poster</div>
            <div style={{ fontSize: 20, fontWeight: 700 }}>{poster.length}</div>
          </div>
          <div>
            <div style={{ fontSize: 13, color: '#666' }}>Sum gjenstående</div>
            <div style={{ fontSize: 20, fontWeight: 700, fontFamily: 'monospace' }}>
              {formatBelop(sumGjenstaaende)}
            </div>
          </div>
          <div>
            <div style={{ fontSize: 13, color: '#666' }}>Sum opprinnelig</div>
            <div style={{ fontSize: 20, fontWeight: 700, fontFamily: 'monospace' }}>
              {formatBelop(sumBelop)}
            </div>
          </div>
          <div>
            <div style={{ fontSize: 13, color: '#666' }}>Forfalt</div>
            <div style={{ fontSize: 20, fontWeight: 700, color: antallForfalt > 0 ? '#c62828' : '#2e7d32' }}>
              {antallForfalt} av {poster.length}
            </div>
          </div>
        </div>
      )}

      {isLoading && <p>Laster åpne poster...</p>}

      {poster && (
        <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
          <thead>
            <tr>
              <th style={headerStyle}>Kundenr</th>
              <th style={headerStyle}>Kunde</th>
              <th style={headerStyle}>Fakturanr</th>
              <th style={headerStyle}>Fakturadato</th>
              <th style={headerStyle}>Forfallsdato</th>
              <th style={headerStyle}>Dager forfalt</th>
              <th style={headerStyle}>KID</th>
              <th style={{ ...headerStyle, textAlign: 'right' }}>Beløp inkl. MVA</th>
              <th style={{ ...headerStyle, textAlign: 'right' }}>Gjenstående</th>
              <th style={{ ...headerStyle, textAlign: 'center' }}>Status</th>
            </tr>
          </thead>
          <tbody>
            {poster.length === 0 && (
              <tr>
                <td colSpan={10} style={{ ...cellStyle, textAlign: 'center', fontStyle: 'italic' }}>
                  Ingen åpne poster
                </td>
              </tr>
            )}
            {poster.map((p, i) => (
              <tr key={p.id} style={{ backgroundColor: i % 2 === 0 ? '#fff' : '#fafafa' }}>
                <td style={{ ...cellStyle, fontFamily: 'monospace' }}>
                  <Link
                    to={`/kunde/${p.kundeId}`}
                    style={{ color: '#0066cc', textDecoration: 'none' }}
                  >
                    {p.kundenummer}
                  </Link>
                </td>
                <td style={cellStyle}>{p.kundenavn}</td>
                <td style={{ ...cellStyle, fontFamily: 'monospace' }}>{p.fakturanummer}</td>
                <td style={cellStyle}>{formatDato(p.fakturadato)}</td>
                <td
                  style={{
                    ...cellStyle,
                    color: p.erForfalt ? '#c62828' : 'inherit',
                    fontWeight: p.erForfalt ? 600 : 400,
                  }}
                >
                  {formatDato(p.forfallsdato)}
                </td>
                <td
                  style={{
                    ...cellStyle,
                    fontWeight: 600,
                    color: p.dagerForfalt > 0 ? '#c62828' : '#2e7d32',
                  }}
                >
                  {p.dagerForfalt > 0 ? `${p.dagerForfalt}` : '-'}
                </td>
                <td style={{ ...cellStyle, fontFamily: 'monospace', fontSize: 12 }}>
                  {p.kidNummer ?? ''}
                </td>
                <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(p.belopInklMva)}
                </td>
                <td
                  style={{
                    ...cellStyle,
                    textAlign: 'right',
                    fontFamily: 'monospace',
                    fontWeight: 600,
                  }}
                >
                  {formatBelop(p.gjenstaendeBelop)}
                </td>
                <td style={{ ...cellStyle, textAlign: 'center' }}>
                  <span
                    style={{
                      ...statusFarge(p.status),
                      padding: '2px 8px',
                      borderRadius: 4,
                      fontSize: 12,
                    }}
                  >
                    {KundeFakturaStatusNavn[p.status]}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
          {poster.length > 0 && (
            <tfoot>
              <tr>
                <td style={{ ...sumCellStyle }} colSpan={7}>
                  Totalt ({poster.length} poster)
                </td>
                <td style={{ ...sumCellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(sumBelop)}
                </td>
                <td style={{ ...sumCellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(sumGjenstaaende)}
                </td>
                <td style={sumCellStyle} />
              </tr>
            </tfoot>
          )}
        </table>
      )}
    </div>
  );
}
