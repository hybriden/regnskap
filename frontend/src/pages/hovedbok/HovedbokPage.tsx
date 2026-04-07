import { useState } from 'react';
import { Link } from 'react-router-dom';
import { usePerioder } from '../../hooks/api/useHovedbok';
import { formatBelop } from '../../utils/formatering';
import { PeriodeStatusNavn } from '../../types/hovedbok';

const currentYear = new Date().getFullYear();

export default function HovedbokPage() {
  const [ar, setAr] = useState(currentYear);
  const { data: perioderResponse, isLoading, error } = usePerioder(ar);

  if (error) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil ved lasting av hovedbok</h1>
        <p>Kunne ikke hente perioder fra server. Prøv igjen senere.</p>
      </div>
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h1 style={{ margin: 0 }}>Hovedbok</h1>
        <div style={{ display: 'flex', gap: 8 }}>
          <Link
            to="/hovedbok/kontoutskrift"
            style={{
              padding: '8px 16px',
              background: '#0066cc',
              color: '#fff',
              borderRadius: 4,
              textDecoration: 'none',
              fontSize: 14,
            }}
          >
            Kontoutskrift
          </Link>
          <Link
            to="/hovedbok/saldobalanse"
            style={{
              padding: '8px 16px',
              background: '#0066cc',
              color: '#fff',
              borderRadius: 4,
              textDecoration: 'none',
              fontSize: 14,
            }}
          >
            Saldobalanse
          </Link>
          <Link
            to="/hovedbok/perioder"
            style={{
              padding: '8px 16px',
              background: '#f0f0f0',
              borderRadius: 4,
              textDecoration: 'none',
              fontSize: 14,
              color: '#333',
            }}
          >
            Administrer perioder
          </Link>
        </div>
      </div>

      {/* Årsvelger */}
      <div style={{ marginBottom: 24, display: 'flex', alignItems: 'center', gap: 12 }}>
        <label style={{ fontWeight: 600 }}>Regnskapsår:</label>
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

      {/* Periodeoversikt */}
      {isLoading ? (
        <p>Laster perioder...</p>
      ) : perioderResponse?.perioder.length === 0 ? (
        <div style={{ padding: 24, textAlign: 'center', color: '#666' }}>
          <p>Ingen perioder funnet for {ar}.</p>
          <Link
            to="/hovedbok/perioder"
            style={{ color: '#0066cc' }}
          >
            Opprett perioder for dette året
          </Link>
        </div>
      ) : (
        <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
          <thead>
            <tr>
              <th style={thStyle}>Periode</th>
              <th style={thStyle}>Fra dato</th>
              <th style={thStyle}>Til dato</th>
              <th style={thStyle}>Status</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Antall bilag</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Sum debet</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Sum kredit</th>
            </tr>
          </thead>
          <tbody>
            {perioderResponse?.perioder.map((p) => (
              <tr key={p.id} style={{ backgroundColor: p.status === 'Lukket' ? '#f5f5f5' : '#fff' }}>
                <td style={tdStyle}>{p.periodenavn}</td>
                <td style={tdStyle}>{p.fraDato}</td>
                <td style={tdStyle}>{p.tilDato}</td>
                <td style={tdStyle}>
                  <span
                    style={{
                      padding: '2px 8px',
                      borderRadius: 12,
                      fontSize: 12,
                      fontWeight: 600,
                      backgroundColor:
                        p.status === 'Apen' ? '#e8f5e9' :
                        p.status === 'Sperret' ? '#fff3e0' : '#f5f5f5',
                      color:
                        p.status === 'Apen' ? '#2e7d32' :
                        p.status === 'Sperret' ? '#e65100' : '#616161',
                    }}
                  >
                    {PeriodeStatusNavn[p.status]}
                  </span>
                </td>
                <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>{p.antallBilag}</td>
                <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(p.sumDebet)}
                </td>
                <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(p.sumKredit)}
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
