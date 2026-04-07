import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useFakturaer } from '../../hooks/api/useFaktura';
import {
  FakturaStatus,
  FakturaStatusNavn,
  FakturaStatusFarge,
  FakturaLeveringsformatNavn,
} from '../../types/faktura';
import type { FakturaListeResponse } from '../../types/faktura';
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

function statusBadge(status: string) {
  const farge = FakturaStatusFarge[status as keyof typeof FakturaStatusFarge] ?? {
    bg: '#f5f5f5',
    color: '#333',
  };
  return (
    <span
      style={{
        padding: '2px 8px',
        borderRadius: 4,
        backgroundColor: farge.bg,
        color: farge.color,
        fontSize: 12,
        fontWeight: 600,
      }}
    >
      {FakturaStatusNavn[status as keyof typeof FakturaStatusNavn] ?? status}
    </span>
  );
}

const statusFilterAlternativer = [
  { value: '', label: 'Alle statuser' },
  { value: FakturaStatus.Utkast, label: 'Utkast' },
  { value: FakturaStatus.Godkjent, label: 'Godkjent' },
  { value: FakturaStatus.Utstedt, label: 'Utstedt' },
  { value: FakturaStatus.Kreditert, label: 'Kreditert' },
  { value: FakturaStatus.Kansellert, label: 'Kansellert' },
];

export default function FakturaListePage() {
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('');
  const [sok, setSok] = useState('');

  const { data: paginert, isLoading, error } = useFakturaer({
    page,
    pageSize: 50,
    status: statusFilter || undefined,
    sok: sok.length >= 2 ? sok : undefined,
  });

  const fakturaer: FakturaListeResponse[] = paginert?.items ?? [];

  if (error) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil ved lasting av fakturaer</h1>
        <p>Kunne ikke hente fakturaer fra server.</p>
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
        <h1 style={{ margin: 0 }}>Fakturering</h1>
        <Link
          to="/faktura/ny"
          style={{
            padding: '8px 20px',
            background: '#2e7d32',
            color: '#fff',
            border: 'none',
            borderRadius: 4,
            textDecoration: 'none',
            fontSize: 14,
            fontWeight: 600,
          }}
        >
          + Ny faktura
        </Link>
      </div>

      {/* Filtre */}
      <div style={{ display: 'flex', gap: 12, marginBottom: 16 }}>
        <input
          type="text"
          placeholder="Søk etter fakturanummer, kundenavn..."
          value={sok}
          onChange={(e) => {
            setSok(e.target.value);
            setPage(1);
          }}
          style={{
            flex: 1,
            padding: '10px 14px',
            border: '1px solid #ccc',
            borderRadius: 4,
            fontSize: 14,
          }}
        />
        <select
          value={statusFilter}
          onChange={(e) => {
            setStatusFilter(e.target.value);
            setPage(1);
          }}
          style={{
            padding: '10px 14px',
            border: '1px solid #ccc',
            borderRadius: 4,
            fontSize: 14,
            minWidth: 180,
          }}
        >
          {statusFilterAlternativer.map((alt) => (
            <option key={alt.value} value={alt.value}>
              {alt.label}
            </option>
          ))}
        </select>
      </div>

      {/* Tabell */}
      {isLoading ? (
        <p>Laster fakturaer...</p>
      ) : (
        <>
          <table
            style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}
          >
            <thead>
              <tr>
                <th style={headerStyle}>Fakturanr</th>
                <th style={headerStyle}>Type</th>
                <th style={headerStyle}>Kunde</th>
                <th style={headerStyle}>Dato</th>
                <th style={headerStyle}>Forfall</th>
                <th style={{ ...headerStyle, textAlign: 'right' }}>Eks. MVA</th>
                <th style={{ ...headerStyle, textAlign: 'right' }}>MVA</th>
                <th style={{ ...headerStyle, textAlign: 'right' }}>Inkl. MVA</th>
                <th style={headerStyle}>Levering</th>
                <th style={{ ...headerStyle, textAlign: 'center' }}>Status</th>
              </tr>
            </thead>
            <tbody>
              {fakturaer.length === 0 && (
                <tr>
                  <td
                    colSpan={10}
                    style={{ ...cellStyle, textAlign: 'center', fontStyle: 'italic' }}
                  >
                    {sok.length >= 2 || statusFilter
                      ? 'Ingen fakturaer funnet med valgte filtre'
                      : 'Ingen fakturaer registrert'}
                  </td>
                </tr>
              )}
              {fakturaer.map((faktura, index) => (
                <tr
                  key={faktura.id}
                  style={{ backgroundColor: index % 2 === 0 ? '#fff' : '#fafafa' }}
                >
                  <td style={{ ...cellStyle, fontFamily: 'monospace' }}>
                    <Link
                      to={`/faktura/${faktura.id}`}
                      style={{ color: '#0066cc', textDecoration: 'none' }}
                    >
                      {faktura.fakturaId ?? 'Utkast'}
                    </Link>
                  </td>
                  <td style={cellStyle}>
                    {faktura.dokumenttype === 'Kreditnota' ? (
                      <span style={{ color: '#c62828', fontWeight: 600 }}>Kreditnota</span>
                    ) : (
                      'Faktura'
                    )}
                  </td>
                  <td style={cellStyle}>
                    <span style={{ fontWeight: 500 }}>{faktura.kundeNavn}</span>
                    {faktura.kundenummer && (
                      <span style={{ color: '#888', fontSize: 12, marginLeft: 6 }}>
                        ({faktura.kundenummer})
                      </span>
                    )}
                  </td>
                  <td style={{ ...cellStyle, fontFamily: 'monospace' }}>
                    {faktura.fakturadato ? formatDato(faktura.fakturadato) : '-'}
                  </td>
                  <td style={{ ...cellStyle, fontFamily: 'monospace' }}>
                    {faktura.forfallsdato ? formatDato(faktura.forfallsdato) : '-'}
                  </td>
                  <td
                    style={{
                      ...cellStyle,
                      textAlign: 'right',
                      fontFamily: 'monospace',
                    }}
                  >
                    {formatBelop(faktura.belopEksMva)}
                  </td>
                  <td
                    style={{
                      ...cellStyle,
                      textAlign: 'right',
                      fontFamily: 'monospace',
                    }}
                  >
                    {formatBelop(faktura.mvaBelop)}
                  </td>
                  <td
                    style={{
                      ...cellStyle,
                      textAlign: 'right',
                      fontFamily: 'monospace',
                      fontWeight: 600,
                    }}
                  >
                    {formatBelop(faktura.belopInklMva)}
                  </td>
                  <td style={{ ...cellStyle, fontSize: 12 }}>
                    {FakturaLeveringsformatNavn[faktura.leveringsformat]}
                  </td>
                  <td style={{ ...cellStyle, textAlign: 'center' }}>
                    {statusBadge(faktura.status)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {/* Paginering */}
          {paginert && paginert.totalPages > 1 && (
            <div
              style={{
                marginTop: 16,
                display: 'flex',
                justifyContent: 'center',
                gap: 8,
                alignItems: 'center',
              }}
            >
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                style={{
                  padding: '6px 12px',
                  border: '1px solid #ccc',
                  borderRadius: 4,
                  cursor: page <= 1 ? 'default' : 'pointer',
                  opacity: page <= 1 ? 0.5 : 1,
                }}
              >
                Forrige
              </button>
              <span style={{ fontSize: 14 }}>
                Side {paginert.page} av {paginert.totalPages} ({paginert.totalCount} fakturaer)
              </span>
              <button
                onClick={() => setPage((p) => Math.min(paginert.totalPages, p + 1))}
                disabled={page >= paginert.totalPages}
                style={{
                  padding: '6px 12px',
                  border: '1px solid #ccc',
                  borderRadius: 4,
                  cursor: page >= paginert.totalPages ? 'default' : 'pointer',
                  opacity: page >= paginert.totalPages ? 0.5 : 1,
                }}
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
