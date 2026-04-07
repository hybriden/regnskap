import { useState } from 'react';
import { useSaftEksport } from '../../hooks/api/useRapportering';

const containerStyle: React.CSSProperties = { maxWidth: 700, margin: '0 auto', padding: 24 };
const feltStyle: React.CSSProperties = { display: 'flex', flexDirection: 'column', gap: 4, marginBottom: 16 };
const inputStyle: React.CSSProperties = { padding: '8px 12px', border: '1px solid #ccc', borderRadius: 4, fontSize: 14 };
const buttonStyle: React.CSSProperties = { padding: '12px 32px', backgroundColor: '#1a5276', color: '#fff', border: 'none', borderRadius: 4, fontSize: 16, cursor: 'pointer', fontWeight: 600 };

export default function SaftEksportPage() {
  const innevaerendeAr = new Date().getFullYear();
  const [ar, setAr] = useState(innevaerendeAr);
  const [fraPeriode, setFraPeriode] = useState(1);
  const [tilPeriode, setTilPeriode] = useState(12);

  const { mutate: eksporter, isPending, isSuccess, isError, error } = useSaftEksport();

  function handleEksport() {
    eksporter({
      ar,
      fraPeriode,
      tilPeriode,
    });
  }

  return (
    <div style={containerStyle}>
      <h1 style={{ margin: 0 }}>SAF-T Eksport</h1>
      <p style={{ color: '#666', marginTop: 4 }}>
        Generer SAF-T Financial XML v1.30 for innsending til Skatteetaten
      </p>

      <div style={{ marginTop: 24, padding: 24, border: '1px solid #e0e0e0', borderRadius: 8, backgroundColor: '#fafafa' }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 16 }}>
          <div style={feltStyle}>
            <label style={{ fontWeight: 600, fontSize: 13 }}>Regnskapsår</label>
            <input
              type="number"
              value={ar}
              onChange={(e) => setAr(Number(e.target.value))}
              style={inputStyle}
            />
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
        </div>

        <div style={{ marginTop: 24, display: 'flex', alignItems: 'center', gap: 16 }}>
          <button
            style={{ ...buttonStyle, opacity: isPending ? 0.6 : 1 }}
            onClick={handleEksport}
            disabled={isPending}
          >
            {isPending ? 'Genererer...' : 'Last ned SAF-T XML'}
          </button>

          {isSuccess && (
            <span style={{ color: '#27ae60', fontWeight: 600, fontSize: 14 }}>
              Eksport fullfort! Filen lastes ned.
            </span>
          )}
          {isError && (
            <span style={{ color: 'red', fontWeight: 600, fontSize: 14 }}>
              Feil ved eksport: {String(error)}
            </span>
          )}
        </div>
      </div>

      <div style={{ marginTop: 32, padding: 20, backgroundColor: '#eaf2f8', borderRadius: 8 }}>
        <h3 style={{ margin: '0 0 12px' }}>Om SAF-T</h3>
        <ul style={{ margin: 0, paddingLeft: 20, lineHeight: 1.8, fontSize: 14, color: '#333' }}>
          <li>SAF-T (Standard Audit File - Tax) er et standardformat for utveksling av regnskapsdata.</li>
          <li>Alle bokforingspliktige virksomheter i Norge skal kunne levere SAF-T-fil ved forespørsel fra Skatteetaten.</li>
          <li>Filen inneholder kontoplan, kunder, leverandører, hovedbokstransaksjoner og MVA-koder.</li>
          <li>Filnavnet følger mønsteret: SAF-T_{'{'}år{'}'}_P{'{'}fra{'}'}-{'{'}til{'}'}.xml</li>
        </ul>
      </div>
    </div>
  );
}
