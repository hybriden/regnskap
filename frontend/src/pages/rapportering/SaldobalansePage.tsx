import { useState } from 'react';
import { useSaldobalanseRapport } from '../../hooks/api/useRapportering';
import { formatBelop } from '../../utils/formatering';
import type { SaldobalanseGruppeDto, SaldobalanseRapportLinjeDto } from '../../types/rapportering';

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

const thStyle: React.CSSProperties = { textAlign: 'right', padding: '8px 10px', fontWeight: 700, fontSize: 13 };
const tdStyle: React.CSSProperties = { padding: '4px 10px', textAlign: 'right', fontFamily: 'monospace', fontSize: 13 };

function belopTd(verdi: number, style?: React.CSSProperties) {
  return (
    <td style={{ ...tdStyle, color: verdi < 0 ? 'red' : 'inherit', ...style }}>
      {formatBelop(verdi)}
    </td>
  );
}

function LinjeRad({ linje }: { linje: SaldobalanseRapportLinjeDto }) {
  return (
    <tr>
      <td style={{ padding: '4px 10px 4px 24px', fontSize: 13 }}>{linje.kontonummer}</td>
      <td style={{ padding: '4px 10px', fontSize: 13 }}>{linje.kontonavn}</td>
      <td style={{ padding: '4px 10px', fontSize: 13, textAlign: 'center' }}>{linje.kontotype}</td>
      {belopTd(linje.inngaendeBalanse)}
      {belopTd(linje.sumDebet)}
      {belopTd(linje.sumKredit)}
      {belopTd(linje.endring)}
      {belopTd(linje.utgaendeBalanse)}
    </tr>
  );
}

function GruppeVisning({ gruppe }: { gruppe: SaldobalanseGruppeDto }) {
  return (
    <>
      <tr>
        <td colSpan={8} style={{ padding: '12px 10px 4px', fontWeight: 700, fontSize: 14, borderBottom: '1px solid #ccc' }}>
          {gruppe.gruppekode} – {gruppe.gruppenavn}
        </td>
      </tr>
      {gruppe.linjer.map((linje, idx) => (
        <LinjeRad key={`${linje.kontonummer}-${idx}`} linje={linje} />
      ))}
      <tr style={{ fontWeight: 700, backgroundColor: '#f5f5f5', borderTop: '1px solid #999' }}>
        <td colSpan={3} style={{ padding: '6px 10px', fontSize: 13 }}>Sum {gruppe.gruppenavn}</td>
        {belopTd(gruppe.gruppeIB, { fontWeight: 700 })}
        {belopTd(gruppe.gruppeSumDebet, { fontWeight: 700 })}
        {belopTd(gruppe.gruppeSumKredit, { fontWeight: 700 })}
        <td style={tdStyle} />
        {belopTd(gruppe.gruppeUB, { fontWeight: 700 })}
      </tr>
    </>
  );
}

export default function SaldobalansePage() {
  const innevaerendeAr = new Date().getFullYear();
  const [ar, setAr] = useState(innevaerendeAr);
  const [fraPeriode, setFraPeriode] = useState(1);
  const [tilPeriode, setTilPeriode] = useState(12);
  const [inkluderNullsaldo, setInkluderNullsaldo] = useState(false);
  const [gruppert, setGruppert] = useState(true);

  const { data, isLoading, error } = useSaldobalanseRapport({
    ar,
    fraPeriode,
    tilPeriode,
    inkluderNullsaldo,
    gruppert,
  });

  return (
    <div style={containerStyle}>
      <style>{printStyle}</style>
      <h1 style={{ margin: 0 }}>Saldobalanse</h1>
      <p style={{ color: '#666', marginTop: 4 }}>Utvidet saldobalanse med grupperinger</p>

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
          <label style={{ fontWeight: 600, fontSize: 13 }}>&nbsp;</label>
          <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 14 }}>
            <input type="checkbox" checked={inkluderNullsaldo} onChange={(e) => setInkluderNullsaldo(e.target.checked)} />
            Vis nullsaldo
          </label>
        </div>
        <div style={feltStyle}>
          <label style={{ fontWeight: 600, fontSize: 13 }}>&nbsp;</label>
          <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 14 }}>
            <input type="checkbox" checked={gruppert} onChange={(e) => setGruppert(e.target.checked)} />
            Grupper per kontogruppe
          </label>
        </div>
        <button style={buttonStyle} className="no-print" onClick={() => window.print()}>Skriv ut</button>
      </div>

      {isLoading && <p>Laster saldobalanse...</p>}
      {error && <p style={{ color: 'red' }}>Feil ved lasting av saldobalanse: {String(error)}</p>}

      {data && (
        <div id="rapport-innhold">
          <div style={{ textAlign: 'center', marginBottom: 16 }}>
            <h2 style={{ margin: 0 }}>Saldobalanse</h2>
            <p style={{ margin: '4px 0', color: '#666' }}>
              Periode {data.fraPeriode}–{data.tilPeriode}, {data.ar}
            </p>
          </div>

          {!data.debetKredittStemmer && (
            <div style={{ padding: 12, backgroundColor: '#ffeaa7', border: '1px solid #fdcb6e', borderRadius: 4, marginBottom: 16, fontWeight: 600 }}>
              Advarsel: Debet og kredit stemmer ikke overens.
            </div>
          )}

          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', minWidth: 900 }}>
              <thead>
                <tr style={{ borderBottom: '2px solid #333' }}>
                  <th style={{ ...thStyle, textAlign: 'left', width: 80 }}>Konto</th>
                  <th style={{ ...thStyle, textAlign: 'left', width: 200 }}>Kontonavn</th>
                  <th style={{ ...thStyle, textAlign: 'center', width: 60 }}>Type</th>
                  <th style={{ ...thStyle, width: 120 }}>IB</th>
                  <th style={{ ...thStyle, width: 120 }}>Debet</th>
                  <th style={{ ...thStyle, width: 120 }}>Kredit</th>
                  <th style={{ ...thStyle, width: 120 }}>Endring</th>
                  <th style={{ ...thStyle, width: 120 }}>UB</th>
                </tr>
              </thead>
              <tbody>
                {data.grupper.map((gruppe) => (
                  <GruppeVisning key={gruppe.gruppekode} gruppe={gruppe} />
                ))}

                <tr style={{ fontWeight: 700, fontSize: 15, borderTop: '3px double #333', backgroundColor: '#f0f0f0' }}>
                  <td colSpan={3} style={{ padding: '10px 10px' }}>Totalt</td>
                  {belopTd(data.totaler.totalIB, { fontWeight: 700, fontSize: 15 })}
                  {belopTd(data.totaler.totalDebet, { fontWeight: 700, fontSize: 15 })}
                  {belopTd(data.totaler.totalKredit, { fontWeight: 700, fontSize: 15 })}
                  <td style={tdStyle} />
                  {belopTd(data.totaler.totalUB, { fontWeight: 700, fontSize: 15 })}
                </tr>
                <tr style={{ fontWeight: 600, fontSize: 14 }}>
                  <td colSpan={3} style={{ padding: '6px 10px' }}>Debet-/kreditsaldo</td>
                  <td style={tdStyle} />
                  {belopTd(data.totaler.debetSaldo, { fontWeight: 600 })}
                  {belopTd(data.totaler.kreditSaldo, { fontWeight: 600 })}
                  <td colSpan={2} style={tdStyle} />
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
