import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAldersfordeling } from '../../hooks/api/useLeverandor';
import { formatBelop } from '../../utils/formatering';

export default function AldersfordelingPage() {
  const iDag = new Date().toISOString().slice(0, 10);
  const [dato, setDato] = useState(iDag);

  const { data: rapport, isLoading, error } = useAldersfordeling(dato);

  return (
    <div style={{ padding: 24, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <div>
          <h1 style={{ margin: 0 }}>Aldersfordeling leverandorer</h1>
          <div style={{ marginTop: 4, fontSize: 13, color: '#666' }}>
            <Link to="/leverandor" style={{ color: '#1565c0' }}>
              Leverandorer
            </Link>{' '}
            / Aldersfordeling
          </div>
        </div>
        <div style={{ display: 'flex', gap: 12, alignItems: 'flex-end' }}>
          <div>
            <label style={labelStil}>Rapportdato</label>
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
        <div style={feilStil}>
          Feil ved henting av aldersfordeling: {(error as Error).message}
        </div>
      )}

      {isLoading ? (
        <p>Laster aldersfordeling...</p>
      ) : rapport ? (
        <table style={tabellStil}>
          <thead>
            <tr>
              <th style={{ ...headerCelleStil, textAlign: 'left' }}>Nr</th>
              <th style={{ ...headerCelleStil, textAlign: 'left' }}>Leverandor</th>
              <th style={headerCelleStil}>Ikke forfalt</th>
              <th style={headerCelleStil}>0-30 dager</th>
              <th style={headerCelleStil}>31-60 dager</th>
              <th style={headerCelleStil}>61-90 dager</th>
              <th style={headerCelleStil}>90+ dager</th>
              <th style={headerCelleStil}>Totalt</th>
            </tr>
          </thead>
          <tbody>
            {rapport.leverandorer.map((lev) => (
              <tr key={lev.leverandorId}>
                <td style={{ ...celleStil, textAlign: 'left', fontFamily: 'monospace', fontWeight: 600 }}>
                  <Link to={`/leverandor/${lev.leverandorId}`} style={{ color: '#1565c0' }}>
                    {lev.leverandornummer}
                  </Link>
                </td>
                <td style={{ ...celleStil, textAlign: 'left' }}>{lev.navn}</td>
                <BelopCelle verdi={lev.ikkeForfalt} />
                <BelopCelle verdi={lev.dager0Til30} fargekode="gul" />
                <BelopCelle verdi={lev.dager31Til60} fargekode="oransje" />
                <BelopCelle verdi={lev.dager61Til90} fargekode="rod" />
                <BelopCelle verdi={lev.over90Dager} fargekode="morkerod" />
                <td style={{ ...celleStil, fontFamily: 'monospace', fontWeight: 700 }}>
                  {formatBelop(lev.totalt)}
                </td>
              </tr>
            ))}
            {rapport.leverandorer.length === 0 && (
              <tr>
                <td colSpan={8} style={{ ...celleStil, textAlign: 'center', color: '#666' }}>
                  Ingen apne poster
                </td>
              </tr>
            )}
          </tbody>
          {rapport.leverandorer.length > 0 && (
            <tfoot>
              <tr style={{ backgroundColor: '#f8f8f8' }}>
                <td colSpan={2} style={{ ...sumCelleStil, textAlign: 'left', fontWeight: 700 }}>
                  Totalt ({rapport.leverandorer.length} leverandorer)
                </td>
                <td style={sumCelleStil}>{formatBelop(rapport.totalt.ikkeForfalt)}</td>
                <td style={sumCelleStil}>{formatBelop(rapport.totalt.dager0Til30)}</td>
                <td style={sumCelleStil}>{formatBelop(rapport.totalt.dager31Til60)}</td>
                <td style={sumCelleStil}>{formatBelop(rapport.totalt.dager61Til90)}</td>
                <td style={sumCelleStil}>{formatBelop(rapport.totalt.over90Dager)}</td>
                <td style={{ ...sumCelleStil, fontWeight: 700 }}>
                  {formatBelop(rapport.totalt.totalt)}
                </td>
              </tr>
            </tfoot>
          )}
        </table>
      ) : null}
    </div>
  );
}

// --- Hjelpekomponenter ---

const farger: Record<string, string> = {
  gul: '#fff8e1',
  oransje: '#fff3e0',
  rod: '#ffebee',
  morkerod: '#fce4ec',
};

function BelopCelle({
  verdi,
  fargekode,
}: {
  verdi: number;
  fargekode?: string;
}) {
  const bgFarge = verdi > 0 && fargekode ? farger[fargekode] : 'transparent';
  return (
    <td
      style={{
        ...celleStil,
        fontFamily: 'monospace',
        backgroundColor: bgFarge,
        color: verdi > 0 && fargekode === 'morkerod' ? '#c62828' : 'inherit',
      }}
    >
      {verdi !== 0 ? formatBelop(verdi) : ''}
    </td>
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
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
};

const feilStil: React.CSSProperties = {
  padding: 12,
  background: '#ffebee',
  color: '#c62828',
  borderRadius: 4,
  marginBottom: 16,
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

const sumCelleStil: React.CSSProperties = {
  padding: '8px 12px',
  fontWeight: 700,
  borderTop: '2px solid #333',
  textAlign: 'right',
  fontFamily: 'monospace',
};
