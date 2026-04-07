import { useState } from 'react';
import { useKontantstrom } from '../../hooks/api/useRapportering';
import { formatBelop } from '../../utils/formatering';
import type { KontantstromSeksjonDto, KontantstromLinjeDto } from '../../types/rapportering';

const containerStyle: React.CSSProperties = { maxWidth: 900, margin: '0 auto', padding: 24 };
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

function belopTd(verdi: number, style?: React.CSSProperties) {
  return (
    <td style={{ padding: '6px 12px', textAlign: 'right', fontFamily: 'monospace', color: verdi < 0 ? 'red' : 'inherit', ...style }}>
      {formatBelop(verdi)}
    </td>
  );
}

function SeksjonVisning({ seksjon, harForrigeAr, ar }: { seksjon: KontantstromSeksjonDto; harForrigeAr: boolean; ar: number }) {
  return (
    <>
      <tr>
        <td colSpan={harForrigeAr ? 3 : 2} style={{ padding: '16px 12px 4px', fontWeight: 700, fontSize: 15, borderBottom: '1px solid #ccc' }}>
          {seksjon.navn}
        </td>
      </tr>
      {seksjon.linjer.map((linje: KontantstromLinjeDto, idx: number) => (
        <tr key={idx}>
          <td style={{ padding: '4px 12px 4px 24px' }}>{linje.beskrivelse}</td>
          {belopTd(linje.belop)}
          {harForrigeAr && (linje.forrigeArBelop !== null ? belopTd(linje.forrigeArBelop) : <td />)}
        </tr>
      ))}
      <tr style={{ fontWeight: 700, borderTop: '2px solid #333', backgroundColor: '#f5f5f5' }}>
        <td style={{ padding: '8px 12px' }}>Netto kontantstrøm fra {seksjon.navn.toLowerCase()}</td>
        {belopTd(seksjon.sum, { fontWeight: 700 })}
        {harForrigeAr && (seksjon.forrigeArSum !== null ? belopTd(seksjon.forrigeArSum, { fontWeight: 700 }) : <td />)}
      </tr>
    </>
  );
}

export default function KontantstrommPage() {
  const innevaerendeAr = new Date().getFullYear();
  const [ar, setAr] = useState(innevaerendeAr);
  const [inkluderForrigeAr, setInkluderForrigeAr] = useState(true);

  const { data, isLoading, error } = useKontantstrom({ ar, inkluderForrigeAr });
  const harForrigeAr = inkluderForrigeAr && data?.forrigeArNettoEndring !== null && data?.forrigeArNettoEndring !== undefined;

  return (
    <div style={containerStyle}>
      <style>{printStyle}</style>
      <h1 style={{ margin: 0 }}>Kontantstrømoppstilling</h1>
      <p style={{ color: '#666', marginTop: 4 }}>Indirekte metode</p>

      <div style={filterStyle} className="no-print">
        <div style={feltStyle}>
          <label style={{ fontWeight: 600, fontSize: 13 }}>År</label>
          <input type="number" value={ar} onChange={(e) => setAr(Number(e.target.value))} style={{ ...inputStyle, width: 100 }} />
        </div>
        <div style={feltStyle}>
          <label style={{ fontWeight: 600, fontSize: 13 }}>&nbsp;</label>
          <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 14 }}>
            <input type="checkbox" checked={inkluderForrigeAr} onChange={(e) => setInkluderForrigeAr(e.target.checked)} />
            Vis forrige år
          </label>
        </div>
        <button style={buttonStyle} className="no-print" onClick={() => window.print()}>Skriv ut</button>
      </div>

      {isLoading && <p>Laster kontantstrømoppstilling...</p>}
      {error && <p style={{ color: 'red' }}>Feil ved lasting: {String(error)}</p>}

      {data && (
        <div id="rapport-innhold">
          <div style={{ textAlign: 'center', marginBottom: 16 }}>
            <h2 style={{ margin: 0 }}>Kontantstrømoppstilling</h2>
            <p style={{ margin: '4px 0', color: '#666' }}>{data.ar} (indirekte metode)</p>
          </div>

          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid #333' }}>
                <th style={{ textAlign: 'left', padding: '8px 12px' }}>Post</th>
                <th style={{ textAlign: 'right', padding: '8px 12px', width: 150 }}>{ar}</th>
                {harForrigeAr && <th style={{ textAlign: 'right', padding: '8px 12px', width: 150 }}>{ar - 1}</th>}
              </tr>
            </thead>
            <tbody>
              <SeksjonVisning seksjon={data.drift} harForrigeAr={harForrigeAr} ar={ar} />
              <SeksjonVisning seksjon={data.investering} harForrigeAr={harForrigeAr} ar={ar} />
              <SeksjonVisning seksjon={data.finansiering} harForrigeAr={harForrigeAr} ar={ar} />

              <tr><td colSpan={harForrigeAr ? 3 : 2} style={{ padding: 8 }} /></tr>

              <tr style={{ fontWeight: 700, fontSize: 15, borderTop: '3px double #333' }}>
                <td style={{ padding: '10px 12px' }}>Netto endring i likvider</td>
                {belopTd(data.nettoEndringLikvider, { fontWeight: 700, fontSize: 15 })}
                {harForrigeAr && (data.forrigeArNettoEndring !== null ? belopTd(data.forrigeArNettoEndring, { fontWeight: 700, fontSize: 15 }) : <td />)}
              </tr>
              <tr>
                <td style={{ padding: '6px 12px' }}>Likvider IB</td>
                {belopTd(data.likviderIB)}
                {harForrigeAr && <td />}
              </tr>
              <tr style={{ fontWeight: 700, fontSize: 16, borderTop: '3px double #333', backgroundColor: '#f0f0f0' }}>
                <td style={{ padding: '10px 12px' }}>Likvider UB</td>
                {belopTd(data.likviderUB, { fontWeight: 700, fontSize: 16 })}
                {harForrigeAr && <td />}
              </tr>
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
