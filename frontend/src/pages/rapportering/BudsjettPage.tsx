import { useState } from 'react';
import { useBudsjett, useOpprettBudsjett, useSlettBudsjett } from '../../hooks/api/useRapportering';
import { formatBelop } from '../../utils/formatering';
import type { BudsjettDto } from '../../types/rapportering';

const containerStyle: React.CSSProperties = { maxWidth: 1100, margin: '0 auto', padding: 24 };
const filterStyle: React.CSSProperties = { display: 'flex', gap: 16, alignItems: 'flex-end', flexWrap: 'wrap', marginBottom: 24, padding: 16, backgroundColor: '#f8f8f8', borderRadius: 8 };
const feltStyle: React.CSSProperties = { display: 'flex', flexDirection: 'column', gap: 4 };
const inputStyle: React.CSSProperties = { padding: '8px 12px', border: '1px solid #ccc', borderRadius: 4, fontSize: 14 };
const buttonStyle: React.CSSProperties = { padding: '8px 20px', backgroundColor: '#1a5276', color: '#fff', border: 'none', borderRadius: 4, fontSize: 14, cursor: 'pointer' };
const dangerButtonStyle: React.CSSProperties = { ...buttonStyle, backgroundColor: '#c0392b' };

const thStyle: React.CSSProperties = { textAlign: 'right', padding: '8px 10px', fontWeight: 700, fontSize: 13 };
const tdStyle: React.CSSProperties = { padding: '6px 10px', textAlign: 'right', fontFamily: 'monospace', fontSize: 13 };

function belopTd(verdi: number, style?: React.CSSProperties) {
  return (
    <td style={{ ...tdStyle, color: verdi < 0 ? 'red' : 'inherit', ...style }}>
      {formatBelop(verdi)}
    </td>
  );
}

function grupperPerKonto(budsjetter: BudsjettDto[]): Map<string, BudsjettDto[]> {
  const map = new Map<string, BudsjettDto[]>();
  for (const b of budsjetter) {
    const eksisterende = map.get(b.kontonummer);
    if (eksisterende) {
      eksisterende.push(b);
    } else {
      map.set(b.kontonummer, [b]);
    }
  }
  return map;
}

function NyBudsjettlinje({ ar, versjon, onFerdig }: { ar: number; versjon: string; onFerdig: () => void }) {
  const [kontonummer, setKontonummer] = useState('');
  const [periode, setPeriode] = useState(1);
  const [belop, setBelop] = useState('');
  const [merknad, setMerknad] = useState('');

  const { mutate: opprett, isPending } = useOpprettBudsjett();

  function handleLagre() {
    const parsedBelop = parseFloat(belop.replace(',', '.'));
    if (!kontonummer || isNaN(parsedBelop)) return;

    opprett(
      {
        kontonummer,
        ar,
        periode,
        belop: parsedBelop,
        versjon,
        merknad: merknad || null,
      },
      {
        onSuccess: () => {
          setKontonummer('');
          setBelop('');
          setMerknad('');
          onFerdig();
        },
      },
    );
  }

  return (
    <tr style={{ backgroundColor: '#eaf2f8' }}>
      <td style={{ padding: '6px 10px' }}>
        <input
          type="text"
          value={kontonummer}
          onChange={(e) => setKontonummer(e.target.value)}
          placeholder="Konto"
          style={{ ...inputStyle, width: 80, padding: '4px 8px' }}
        />
      </td>
      <td style={{ padding: '6px 10px' }}>
        <select value={periode} onChange={(e) => setPeriode(Number(e.target.value))} style={{ ...inputStyle, padding: '4px 8px' }}>
          <option value={0}>Hele året</option>
          {Array.from({ length: 12 }, (_, i) => (
            <option key={i + 1} value={i + 1}>{i + 1}</option>
          ))}
        </select>
      </td>
      <td style={{ padding: '6px 10px' }}>
        <input
          type="text"
          value={belop}
          onChange={(e) => setBelop(e.target.value)}
          placeholder="0,00"
          style={{ ...inputStyle, width: 110, padding: '4px 8px', textAlign: 'right' }}
        />
      </td>
      <td style={{ padding: '6px 10px' }}>
        <input
          type="text"
          value={merknad}
          onChange={(e) => setMerknad(e.target.value)}
          placeholder="Merknad (valgfritt)"
          style={{ ...inputStyle, width: '100%', padding: '4px 8px' }}
        />
      </td>
      <td style={{ padding: '6px 10px' }}>
        <button
          style={{ ...buttonStyle, padding: '4px 12px', fontSize: 13 }}
          onClick={handleLagre}
          disabled={isPending || !kontonummer || !belop}
        >
          {isPending ? 'Lagrer...' : 'Legg til'}
        </button>
      </td>
    </tr>
  );
}

export default function BudsjettPage() {
  const innevaerendeAr = new Date().getFullYear();
  const [ar, setAr] = useState(innevaerendeAr);
  const [versjon, setVersjon] = useState('Opprinnelig');
  const [visNyLinje, setVisNyLinje] = useState(false);

  const { data, isLoading, error } = useBudsjett(ar, versjon);
  const { mutate: slettBudsjett, isPending: sletter } = useSlettBudsjett();

  const gruppert = data ? grupperPerKonto(data) : new Map<string, BudsjettDto[]>();
  const kontoer = Array.from(gruppert.keys()).sort();

  const totalBudsjett = data?.reduce((sum, b) => sum + b.belop, 0) ?? 0;

  function handleSlett() {
    if (window.confirm(`Slett alle budsjettlinjer for ${ar}, versjon "${versjon}"?`)) {
      slettBudsjett({ ar, versjon });
    }
  }

  return (
    <div style={containerStyle}>
      <h1 style={{ margin: 0 }}>Budsjett</h1>
      <p style={{ color: '#666', marginTop: 4 }}>Budsjettregistrering og administrasjon</p>

      <div style={filterStyle}>
        <div style={feltStyle}>
          <label style={{ fontWeight: 600, fontSize: 13 }}>År</label>
          <input type="number" value={ar} onChange={(e) => setAr(Number(e.target.value))} style={{ ...inputStyle, width: 100 }} />
        </div>
        <div style={feltStyle}>
          <label style={{ fontWeight: 600, fontSize: 13 }}>Versjon</label>
          <input
            type="text"
            value={versjon}
            onChange={(e) => setVersjon(e.target.value)}
            style={{ ...inputStyle, width: 180 }}
          />
        </div>
        <button style={buttonStyle} onClick={() => setVisNyLinje(!visNyLinje)}>
          {visNyLinje ? 'Skjul skjema' : '+ Ny budsjettlinje'}
        </button>
        {data && data.length > 0 && (
          <button style={dangerButtonStyle} onClick={handleSlett} disabled={sletter}>
            {sletter ? 'Sletter...' : 'Slett versjon'}
          </button>
        )}
      </div>

      {isLoading && <p>Laster budsjett...</p>}
      {error && <p style={{ color: 'red' }}>Feil ved lasting av budsjett: {String(error)}</p>}

      <div style={{ overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: 700 }}>
          <thead>
            <tr style={{ borderBottom: '2px solid #333' }}>
              <th style={{ ...thStyle, textAlign: 'left', width: 100 }}>Konto</th>
              <th style={{ ...thStyle, textAlign: 'center', width: 80 }}>Periode</th>
              <th style={{ ...thStyle, width: 130 }}>Beløp</th>
              <th style={{ ...thStyle, textAlign: 'left' }}>Merknad</th>
              <th style={{ ...thStyle, width: 60 }} />
            </tr>
          </thead>
          <tbody>
            {visNyLinje && (
              <NyBudsjettlinje ar={ar} versjon={versjon} onFerdig={() => setVisNyLinje(false)} />
            )}

            {kontoer.map((konto) => {
              const linjer = gruppert.get(konto) ?? [];
              return linjer.map((linje, idx) => (
                <tr key={linje.id} style={{ borderTop: idx === 0 ? '1px solid #ddd' : undefined }}>
                  <td style={{ padding: '6px 10px', fontWeight: idx === 0 ? 600 : 400, fontSize: 13 }}>
                    {idx === 0 ? konto : ''}
                  </td>
                  <td style={{ ...tdStyle, textAlign: 'center' }}>
                    {linje.periode === 0 ? 'Årlig' : linje.periode}
                  </td>
                  {belopTd(linje.belop)}
                  <td style={{ padding: '6px 10px', fontSize: 13, color: '#666' }}>
                    {linje.merknad ?? ''}
                  </td>
                  <td style={{ padding: '6px 10px' }} />
                </tr>
              ));
            })}

            {data && data.length === 0 && !visNyLinje && (
              <tr>
                <td colSpan={5} style={{ padding: 24, textAlign: 'center', color: '#999' }}>
                  Ingen budsjettlinjer registrert for {ar}, versjon &quot;{versjon}&quot;.
                  <br />
                  <button
                    style={{ ...buttonStyle, marginTop: 12, fontSize: 13 }}
                    onClick={() => setVisNyLinje(true)}
                  >
                    Legg til første linje
                  </button>
                </td>
              </tr>
            )}

            {data && data.length > 0 && (
              <tr style={{ fontWeight: 700, fontSize: 15, borderTop: '3px double #333', backgroundColor: '#f0f0f0' }}>
                <td colSpan={2} style={{ padding: '10px 10px' }}>Totalt budsjett</td>
                {belopTd(totalBudsjett, { fontWeight: 700, fontSize: 15 })}
                <td colSpan={2} style={{ padding: '10px 10px', fontSize: 13, color: '#666' }}>
                  {data.length} linjer, {kontoer.length} kontoer
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
