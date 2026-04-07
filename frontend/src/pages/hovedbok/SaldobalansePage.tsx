import { useState } from 'react';
import { useSaldobalanse } from '../../hooks/api/useHovedbok';
import { formatBelop, formatKontonummer } from '../../utils/formatering';
import PeriodeVelger from '../../components/PeriodeVelger';
import type { SaldobalanseParams } from '../../types/hovedbok';

const currentYear = new Date().getFullYear();

export default function SaldobalansePage() {
  const [ar, setAr] = useState(currentYear);
  const [periode, setPeriode] = useState<number | undefined>(undefined);
  const [inkluderNullsaldo, setInkluderNullsaldo] = useState(false);
  const [kontoklasseFilter, setKontoklasseFilter] = useState<number | undefined>(undefined);

  const params: SaldobalanseParams = {
    inkluderNullsaldo: inkluderNullsaldo || undefined,
    kontoklasse: kontoklasseFilter,
  };

  const { data, isLoading, error } = useSaldobalanse(
    ar,
    periode ?? -1,
    periode !== undefined ? params : undefined,
  );

  return (
    <div style={{ padding: 24, maxWidth: 1400, margin: '0 auto' }}>
      <h1 style={{ marginBottom: 24 }}>Saldobalanse</h1>

      {/* Filtere */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '120px 250px auto auto',
          gap: 16,
          marginBottom: 24,
          padding: 16,
          backgroundColor: '#f8f8f8',
          borderRadius: 8,
          border: '1px solid #e0e0e0',
          alignItems: 'end',
        }}
      >
        <div>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600 }}>År</label>
          <select
            value={ar}
            onChange={(e) => {
              setAr(Number(e.target.value));
              setPeriode(undefined);
            }}
            style={{
              width: '100%',
              padding: '8px 12px',
              border: '1px solid #ccc',
              borderRadius: 4,
              fontSize: 14,
              boxSizing: 'border-box',
            }}
          >
            {Array.from({ length: 5 }, (_, i) => currentYear - 2 + i).map((year) => (
              <option key={year} value={year}>{year}</option>
            ))}
          </select>
        </div>
        <PeriodeVelger
          ar={ar}
          valgtPeriode={periode}
          onChange={(p) => setPeriode(p?.periode)}
        />
        <div>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600 }}>Kontoklasse</label>
          <select
            value={kontoklasseFilter ?? ''}
            onChange={(e) => setKontoklasseFilter(e.target.value ? Number(e.target.value) : undefined)}
            style={{
              width: '100%',
              padding: '8px 12px',
              border: '1px solid #ccc',
              borderRadius: 4,
              fontSize: 14,
              boxSizing: 'border-box',
            }}
          >
            <option value="">Alle klasser</option>
            {[1, 2, 3, 4, 5, 6, 7, 8].map((k) => (
              <option key={k} value={k}>Klasse {k}</option>
            ))}
          </select>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, paddingBottom: 4 }}>
          <input
            type="checkbox"
            id="inkluderNullsaldo"
            checked={inkluderNullsaldo}
            onChange={(e) => setInkluderNullsaldo(e.target.checked)}
          />
          <label htmlFor="inkluderNullsaldo" style={{ fontSize: 14 }}>
            Inkluder kontoer uten bevegelse
          </label>
        </div>
      </div>

      {/* Resultat */}
      {periode === undefined ? (
        <p style={{ color: '#666', textAlign: 'center', padding: 40 }}>
          Velg en periode for å vise saldobalanse.
        </p>
      ) : isLoading ? (
        <p>Laster saldobalanse...</p>
      ) : error ? (
        <p style={{ color: 'red' }}>Feil ved henting av saldobalanse. Prøv igjen.</p>
      ) : data ? (
        <>
          {/* Balansestatus */}
          <div
            style={{
              marginBottom: 16,
              padding: 12,
              backgroundColor: data.erIBalanse ? '#e8f5e9' : '#ffebee',
              borderRadius: 4,
              fontWeight: 600,
              color: data.erIBalanse ? '#2e7d32' : '#c62828',
            }}
          >
            {data.erIBalanse
              ? 'Saldobalansen er i balanse.'
              : 'ADVARSEL: Saldobalansen er IKKE i balanse!'}
          </div>

          <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
            <thead>
              <tr>
                <th style={thStyle}>Konto</th>
                <th style={thStyle}>Kontonavn</th>
                <th style={thStyle}>Type</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Inngående</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Debet</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Kredit</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Endring</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Utgående</th>
              </tr>
            </thead>
            <tbody>
              {data.kontoer.map((konto) => (
                <tr key={konto.kontonummer}>
                  <td style={{ ...tdStyle, fontFamily: 'monospace', fontWeight: 600 }}>
                    {formatKontonummer(konto.kontonummer)}
                  </td>
                  <td style={tdStyle}>{konto.kontonavn}</td>
                  <td style={{ ...tdStyle, fontSize: 12, color: '#666' }}>{konto.kontotype}</td>
                  {belopTd(konto.inngaendeBalanse)}
                  {belopTd(konto.sumDebet)}
                  {belopTd(konto.sumKredit)}
                  {belopTd(konto.endring)}
                  {belopTd(konto.utgaendeBalanse)}
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr style={{ backgroundColor: '#f8f8f8' }}>
                <td style={sumStyle} colSpan={3}>
                  <strong>Totalt ({data.kontoer.length} kontoer)</strong>
                </td>
                <td style={sumStyle} />
                <td style={{ ...sumStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  <strong>{formatBelop(data.totalSumDebet)}</strong>
                </td>
                <td style={{ ...sumStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  <strong>{formatBelop(data.totalSumKredit)}</strong>
                </td>
                <td style={sumStyle} />
                <td style={sumStyle} />
              </tr>
            </tfoot>
          </table>
        </>
      ) : null}
    </div>
  );
}

function belopTd(verdi: number) {
  return (
    <td
      style={{
        ...tdStyle,
        textAlign: 'right',
        fontFamily: 'monospace',
        color: verdi < 0 ? 'red' : 'inherit',
      }}
    >
      {verdi !== 0 ? formatBelop(verdi) : ''}
    </td>
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
  padding: '6px 12px',
  borderBottom: '1px solid #e0e0e0',
  fontSize: 14,
};

const sumStyle: React.CSSProperties = {
  padding: '8px 12px',
  borderTop: '2px solid #333',
  fontSize: 14,
};
