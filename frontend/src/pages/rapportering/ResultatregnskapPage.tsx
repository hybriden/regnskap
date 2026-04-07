import { useState } from 'react';
import { useResultatregnskap } from '../../hooks/api/useRapportering';
import { formatBelop } from '../../utils/formatering';
import { ResultatregnskapFormat } from '../../types/rapportering';
import type { ResultatregnskapSeksjonDto, ResultatregnskapLinjeDto } from '../../types/rapportering';

const containerStyle: React.CSSProperties = {
  maxWidth: 900,
  margin: '0 auto',
  padding: 24,
};

const printStyle = `
  @media print {
    body * { visibility: hidden; }
    #rapport-innhold, #rapport-innhold * { visibility: visible; }
    #rapport-innhold { position: absolute; left: 0; top: 0; width: 100%; }
    .no-print { display: none !important; }
  }
`;

const filterStyle: React.CSSProperties = {
  display: 'flex',
  gap: 16,
  alignItems: 'flex-end',
  flexWrap: 'wrap',
  marginBottom: 24,
  padding: 16,
  backgroundColor: '#f8f8f8',
  borderRadius: 8,
};

const feltStyle: React.CSSProperties = {
  display: 'flex',
  flexDirection: 'column',
  gap: 4,
};

const inputStyle: React.CSSProperties = {
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
};

const buttonStyle: React.CSSProperties = {
  padding: '8px 20px',
  backgroundColor: '#1a5276',
  color: '#fff',
  border: 'none',
  borderRadius: 4,
  fontSize: 14,
  cursor: 'pointer',
};

function belopTd(verdi: number, style?: React.CSSProperties) {
  return (
    <td
      style={{
        padding: '6px 12px',
        textAlign: 'right',
        fontFamily: 'monospace',
        color: verdi < 0 ? 'red' : 'inherit',
        ...style,
      }}
    >
      {formatBelop(verdi)}
    </td>
  );
}

function endringKolonner(faktisk: number, forrige: number | null) {
  if (forrige === null) return null;
  const avvik = faktisk - forrige;
  const pst = forrige !== 0 ? (avvik / Math.abs(forrige)) * 100 : 0;
  return (
    <>
      {belopTd(forrige)}
      {belopTd(avvik)}
      <td
        style={{
          padding: '6px 12px',
          textAlign: 'right',
          fontFamily: 'monospace',
          color: avvik < 0 ? 'red' : avvik > 0 ? '#27ae60' : 'inherit',
        }}
      >
        {forrige !== 0 ? `${pst >= 0 ? '+' : ''}${pst.toFixed(1)} %` : '-'}
      </td>
    </>
  );
}

function SeksjonVisning({
  seksjon,
  harForrigeAr,
}: {
  seksjon: ResultatregnskapSeksjonDto;
  harForrigeAr: boolean;
}) {
  return (
    <>
      <tr>
        <td
          colSpan={harForrigeAr ? 5 : 2}
          style={{
            padding: '12px 12px 4px',
            fontWeight: 700,
            fontSize: 15,
            borderBottom: '1px solid #ccc',
          }}
        >
          {seksjon.navn}
        </td>
      </tr>
      {seksjon.linjer.map((linje: ResultatregnskapLinjeDto, idx: number) => (
        <tr
          key={`${linje.kontonummer}-${idx}`}
          style={{
            fontWeight: linje.erSummeringslinje ? 700 : 400,
            backgroundColor: linje.erSummeringslinje ? '#f5f5f5' : 'transparent',
          }}
        >
          <td style={{ padding: '4px 12px 4px 24px' }}>
            {linje.erSummeringslinje ? linje.kontonavn : `${linje.kontonummer} ${linje.kontonavn}`}
          </td>
          {belopTd(linje.belop)}
          {harForrigeAr && endringKolonner(linje.belop, linje.forrigeArBelop)}
        </tr>
      ))}
      <tr style={{ fontWeight: 700, borderTop: '2px solid #333' }}>
        <td style={{ padding: '6px 12px' }}>Sum {seksjon.navn.toLowerCase()}</td>
        {belopTd(seksjon.sum, { fontWeight: 700 })}
        {harForrigeAr && endringKolonner(seksjon.sum, seksjon.forrigeArSum)}
      </tr>
    </>
  );
}

export default function ResultatregnskapPage() {
  const innevaerendeAr = new Date().getFullYear();
  const [ar, setAr] = useState(innevaerendeAr);
  const [fraPeriode, setFraPeriode] = useState(1);
  const [tilPeriode, setTilPeriode] = useState(12);
  const [format, setFormat] = useState<ResultatregnskapFormat>(ResultatregnskapFormat.Artsinndelt);
  const [inkluderForrigeAr, setInkluderForrigeAr] = useState(true);

  const { data, isLoading, error } = useResultatregnskap({
    ar,
    fraPeriode,
    tilPeriode,
    format,
    inkluderForrigeAr,
  });

  const harForrigeAr = inkluderForrigeAr && data?.forrigeArArsresultat !== null && data?.forrigeArArsresultat !== undefined;

  return (
    <div style={containerStyle}>
      <style>{printStyle}</style>
      <h1 style={{ margin: 0 }}>Resultatregnskap</h1>
      <p style={{ color: '#666', marginTop: 4 }}>Ihht Regnskapsloven 3-2</p>

      <div style={filterStyle} className="no-print">
        <div style={feltStyle}>
          <label style={{ fontWeight: 600, fontSize: 13 }}>År</label>
          <input
            type="number"
            value={ar}
            onChange={(e) => setAr(Number(e.target.value))}
            style={{ ...inputStyle, width: 100 }}
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
        <div style={feltStyle}>
          <label style={{ fontWeight: 600, fontSize: 13 }}>Format</label>
          <select value={format} onChange={(e) => setFormat(e.target.value as ResultatregnskapFormat)} style={inputStyle}>
            <option value="artsinndelt">Artsinndelt</option>
            <option value="funksjonsinndelt">Funksjonsinndelt</option>
          </select>
        </div>
        <div style={feltStyle}>
          <label style={{ fontWeight: 600, fontSize: 13 }}>&nbsp;</label>
          <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 14 }}>
            <input
              type="checkbox"
              checked={inkluderForrigeAr}
              onChange={(e) => setInkluderForrigeAr(e.target.checked)}
            />
            Vis forrige år
          </label>
        </div>
        <button style={buttonStyle} className="no-print" onClick={() => window.print()}>
          Skriv ut
        </button>
      </div>

      {isLoading && <p>Laster resultatregnskap...</p>}
      {error && <p style={{ color: 'red' }}>Feil ved lasting av resultatregnskap: {String(error)}</p>}

      {data && (
        <div id="rapport-innhold">
          <div style={{ textAlign: 'center', marginBottom: 16 }}>
            <h2 style={{ margin: 0 }}>Resultatregnskap</h2>
            <p style={{ margin: '4px 0', color: '#666' }}>
              Periode {data.fraPeriode}–{data.tilPeriode}, {data.ar}
              {data.format === 'artsinndelt' ? ' (artsinndelt)' : ' (funksjonsinndelt)'}
            </p>
          </div>

          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid #333' }}>
                <th style={{ textAlign: 'left', padding: '8px 12px', fontWeight: 700 }}>Post</th>
                <th style={{ textAlign: 'right', padding: '8px 12px', fontWeight: 700, width: 140 }}>
                  {data.ar}
                </th>
                {harForrigeAr && (
                  <>
                    <th style={{ textAlign: 'right', padding: '8px 12px', fontWeight: 700, width: 140 }}>
                      {data.ar - 1}
                    </th>
                    <th style={{ textAlign: 'right', padding: '8px 12px', fontWeight: 700, width: 120 }}>
                      Avvik
                    </th>
                    <th style={{ textAlign: 'right', padding: '8px 12px', fontWeight: 700, width: 80 }}>
                      %
                    </th>
                  </>
                )}
              </tr>
            </thead>
            <tbody>
              {data.seksjoner.map((seksjon) => (
                <SeksjonVisning
                  key={seksjon.kode}
                  seksjon={seksjon}
                  harForrigeAr={harForrigeAr}
                />
              ))}

              {/* Hovedtall */}
              <tr style={{ borderTop: '3px double #333', fontWeight: 700, fontSize: 15 }}>
                <td style={{ padding: '10px 12px' }}>Driftsresultat</td>
                {belopTd(data.driftsresultat, { fontWeight: 700, fontSize: 15 })}
                {harForrigeAr && endringKolonner(data.driftsresultat, data.forrigeArDriftsresultat)}
              </tr>
              <tr style={{ fontWeight: 700, fontSize: 15 }}>
                <td style={{ padding: '6px 12px' }}>Netto finans</td>
                {belopTd(data.finansresultatNetto, { fontWeight: 700, fontSize: 15 })}
                {harForrigeAr && <td colSpan={3} />}
              </tr>
              <tr style={{ fontWeight: 700, fontSize: 15 }}>
                <td style={{ padding: '6px 12px' }}>Ordinært resultat før skatt</td>
                {belopTd(data.ordnaertResultatForSkatt, { fontWeight: 700, fontSize: 15 })}
                {harForrigeAr && <td colSpan={3} />}
              </tr>
              <tr style={{ fontWeight: 700, fontSize: 15 }}>
                <td style={{ padding: '6px 12px' }}>Skattekostnad</td>
                {belopTd(data.skattekostnad, { fontWeight: 700, fontSize: 15 })}
                {harForrigeAr && <td colSpan={3} />}
              </tr>
              <tr
                style={{
                  borderTop: '3px double #333',
                  fontWeight: 700,
                  fontSize: 16,
                  backgroundColor: '#f0f0f0',
                }}
              >
                <td style={{ padding: '10px 12px' }}>Årsresultat</td>
                {belopTd(data.arsresultat, { fontWeight: 700, fontSize: 16 })}
                {harForrigeAr && endringKolonner(data.arsresultat, data.forrigeArArsresultat)}
              </tr>
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
