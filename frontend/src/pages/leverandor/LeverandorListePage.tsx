import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useLeverandorSok } from '../../hooks/api/useLeverandor';
import { formatBelop } from '../../utils/formatering';
import { BetalingsbetingelseNavn } from '../../types/leverandor';
import type { LeverandorSokParams } from '../../types/leverandor';

export default function LeverandorListePage() {
  const navigate = useNavigate();
  const [sokParams, setSokParams] = useState<LeverandorSokParams>({
    erAktiv: true,
    side: 1,
    antall: 50,
  });
  const [tekstSok, setTekstSok] = useState('');
  const [visInaktive, setVisInaktive] = useState(false);

  const { data: resultat, isLoading, error } = useLeverandorSok(sokParams);

  function handleSok() {
    setSokParams((prev) => ({
      ...prev,
      q: tekstSok || undefined,
      erAktiv: visInaktive ? undefined : true,
      side: 1,
    }));
  }

  function handleSideEndring(nySide: number) {
    setSokParams((prev) => ({ ...prev, side: nySide }));
  }

  const totaleSider = resultat ? Math.ceil(resultat.totaltAntall / (sokParams.antall ?? 50)) : 0;

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h1 style={{ margin: 0 }}>Leverandorer</h1>
        <div style={{ display: 'flex', gap: 8 }}>
          <Link to="/leverandor/faktura/ny" style={{ textDecoration: 'none' }}>
            <button style={sekundaerKnappStil}>Ny faktura</button>
          </Link>
          <Link to="/leverandor/betalingsforslag" style={{ textDecoration: 'none' }}>
            <button style={sekundaerKnappStil}>Betalingsforslag</button>
          </Link>
          <Link to="/leverandor/aldersfordeling" style={{ textDecoration: 'none' }}>
            <button style={sekundaerKnappStil}>Aldersfordeling</button>
          </Link>
          <Link to="/leverandor/apne-poster" style={{ textDecoration: 'none' }}>
            <button style={sekundaerKnappStil}>Apne poster</button>
          </Link>
        </div>
      </div>

      {/* Sokefelt */}
      <div style={{ display: 'flex', gap: 12, marginBottom: 16, alignItems: 'flex-end' }}>
        <div style={{ flex: 1 }}>
          <label style={labelStil}>Sok (navn, org.nr, leverandornummer)</label>
          <input
            type="text"
            value={tekstSok}
            onChange={(e) => setTekstSok(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleSok()}
            placeholder="Sok etter leverandor..."
            style={inputStil}
          />
        </div>
        <label style={{ display: 'flex', alignItems: 'center', gap: 6, paddingBottom: 4 }}>
          <input
            type="checkbox"
            checked={visInaktive}
            onChange={(e) => setVisInaktive(e.target.checked)}
          />
          Vis inaktive
        </label>
        <button onClick={handleSok} style={primaerKnappStil}>
          Sok
        </button>
      </div>

      {/* Feilmelding */}
      {error && (
        <div style={{ padding: 12, background: '#ffebee', color: '#c62828', borderRadius: 4, marginBottom: 16 }}>
          Feil ved henting av leverandorer: {(error as Error).message}
        </div>
      )}

      {/* Tabell */}
      {isLoading ? (
        <p>Laster leverandorer...</p>
      ) : (
        <>
          <table style={tabellStil}>
            <thead>
              <tr>
                <th style={headerCelleStil}>Nr</th>
                <th style={{ ...headerCelleStil, textAlign: 'left' }}>Navn</th>
                <th style={headerCelleStil}>Org.nr</th>
                <th style={{ ...headerCelleStil, textAlign: 'left' }}>Betingelse</th>
                <th style={headerCelleStil}>Bankkonto</th>
                <th style={headerCelleStil}>Saldo</th>
                <th style={headerCelleStil}>Status</th>
              </tr>
            </thead>
            <tbody>
              {resultat?.data.map((lev) => (
                <tr
                  key={lev.id}
                  onClick={() => navigate(`/leverandor/${lev.id}`)}
                  style={{ cursor: 'pointer', backgroundColor: lev.erAktiv ? 'transparent' : '#f5f5f5' }}
                  onMouseOver={(e) => {
                    e.currentTarget.style.backgroundColor = '#e8f0fe';
                  }}
                  onMouseOut={(e) => {
                    e.currentTarget.style.backgroundColor = lev.erAktiv ? 'transparent' : '#f5f5f5';
                  }}
                >
                  <td style={{ ...celleStil, fontFamily: 'monospace', fontWeight: 600 }}>
                    {lev.leverandornummer}
                  </td>
                  <td style={{ ...celleStil, textAlign: 'left' }}>{lev.navn}</td>
                  <td style={{ ...celleStil, fontFamily: 'monospace' }}>
                    {lev.organisasjonsnummer ?? '-'}
                  </td>
                  <td style={{ ...celleStil, textAlign: 'left' }}>
                    {BetalingsbetingelseNavn[lev.betalingsbetingelse]}
                  </td>
                  <td style={{ ...celleStil, fontFamily: 'monospace' }}>
                    {lev.bankkontonummer ?? lev.iban ?? '-'}
                  </td>
                  <td
                    style={{
                      ...celleStil,
                      fontFamily: 'monospace',
                      color: lev.saldo < 0 ? 'red' : 'inherit',
                    }}
                  >
                    {formatBelop(lev.saldo)}
                  </td>
                  <td style={celleStil}>
                    <span
                      style={{
                        padding: '2px 8px',
                        borderRadius: 12,
                        fontSize: 12,
                        fontWeight: 600,
                        backgroundColor: lev.erAktiv ? '#e8f5e9' : '#ffebee',
                        color: lev.erAktiv ? '#2e7d32' : '#c62828',
                      }}
                    >
                      {lev.erAktiv ? 'Aktiv' : 'Inaktiv'}
                    </span>
                  </td>
                </tr>
              ))}
              {resultat?.data.length === 0 && (
                <tr>
                  <td colSpan={7} style={{ ...celleStil, textAlign: 'center', color: '#666' }}>
                    Ingen leverandorer funnet
                  </td>
                </tr>
              )}
            </tbody>
          </table>

          {/* Paginering */}
          {totaleSider > 1 && (
            <div style={{ display: 'flex', justifyContent: 'center', gap: 8, marginTop: 16 }}>
              <button
                onClick={() => handleSideEndring((sokParams.side ?? 1) - 1)}
                disabled={(sokParams.side ?? 1) <= 1}
                style={pagineringsKnappStil}
              >
                Forrige
              </button>
              <span style={{ padding: '8px 12px' }}>
                Side {sokParams.side ?? 1} av {totaleSider} ({resultat?.totaltAntall} leverandorer)
              </span>
              <button
                onClick={() => handleSideEndring((sokParams.side ?? 1) + 1)}
                disabled={(sokParams.side ?? 1) >= totaleSider}
                style={pagineringsKnappStil}
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

// --- Stiler ---

const labelStil: React.CSSProperties = {
  display: 'block',
  marginBottom: 4,
  fontWeight: 600,
  fontSize: 13,
};

const inputStil: React.CSSProperties = {
  width: '100%',
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  boxSizing: 'border-box',
};

const primaerKnappStil: React.CSSProperties = {
  padding: '8px 20px',
  backgroundColor: '#1565c0',
  color: '#fff',
  border: 'none',
  borderRadius: 4,
  cursor: 'pointer',
  fontSize: 14,
  fontWeight: 600,
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

const pagineringsKnappStil: React.CSSProperties = {
  padding: '6px 16px',
  border: '1px solid #ccc',
  borderRadius: 4,
  backgroundColor: '#fff',
  cursor: 'pointer',
  fontSize: 13,
};
