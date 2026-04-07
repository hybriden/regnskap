import { useState } from 'react';
import { useNokkeltall } from '../../hooks/api/useRapportering';
import type { LikviditetDto, SoliditetDto, LonnsomhetDto, NokkeltallDto } from '../../types/rapportering';

const containerStyle: React.CSSProperties = { maxWidth: 1000, margin: '0 auto', padding: 24 };
const filterStyle: React.CSSProperties = { display: 'flex', gap: 16, alignItems: 'flex-end', flexWrap: 'wrap', marginBottom: 24, padding: 16, backgroundColor: '#f8f8f8', borderRadius: 8 };
const feltStyle: React.CSSProperties = { display: 'flex', flexDirection: 'column', gap: 4 };
const inputStyle: React.CSSProperties = { padding: '8px 12px', border: '1px solid #ccc', borderRadius: 4, fontSize: 14 };

const printStyle = `
  @media print {
    body * { visibility: hidden; }
    #rapport-innhold, #rapport-innhold * { visibility: visible; }
    #rapport-innhold { position: absolute; left: 0; top: 0; width: 100%; }
    .no-print { display: none !important; }
  }
`;

const buttonStyle: React.CSSProperties = { padding: '8px 20px', backgroundColor: '#1a5276', color: '#fff', border: 'none', borderRadius: 4, fontSize: 14, cursor: 'pointer' };

function formatProsent(verdi: number): string {
  return `${verdi.toLocaleString('nb-NO', { minimumFractionDigits: 1, maximumFractionDigits: 1 })} %`;
}

function formatTall(verdi: number): string {
  return verdi.toLocaleString('nb-NO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function formatKroner(verdi: number): string {
  if (verdi < 0) {
    return `(${Math.abs(verdi).toLocaleString('nb-NO', { minimumFractionDigits: 0, maximumFractionDigits: 0 })})`;
  }
  return verdi.toLocaleString('nb-NO', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
}

interface NokkeltallRadProps {
  navn: string;
  verdi: string;
  forrigeAr: string | null;
  beskrivelse: string;
  fargetema?: 'positiv' | 'negativ' | 'noytral';
}

function NokkeltallRad({ navn, verdi, forrigeAr, beskrivelse, fargetema }: NokkeltallRadProps) {
  const verdiFarge = fargetema === 'positiv' ? '#27ae60' : fargetema === 'negativ' ? '#e74c3c' : 'inherit';
  return (
    <tr>
      <td style={{ padding: '10px 12px', fontWeight: 600 }}>{navn}</td>
      <td style={{ padding: '10px 12px', textAlign: 'right', fontFamily: 'monospace', fontSize: 15, fontWeight: 700, color: verdiFarge }}>
        {verdi}
      </td>
      {forrigeAr !== null && (
        <td style={{ padding: '10px 12px', textAlign: 'right', fontFamily: 'monospace', fontSize: 14, color: '#666' }}>
          {forrigeAr}
        </td>
      )}
      <td style={{ padding: '10px 12px', color: '#666', fontSize: 13 }}>{beskrivelse}</td>
    </tr>
  );
}

function LikviditetSeksjon({ data, forrigeAr }: { data: LikviditetDto; forrigeAr: LikviditetDto | null }) {
  return (
    <>
      <NokkeltallRad
        navn="Likviditetsgrad 1"
        verdi={formatTall(data.likviditetsgrad1)}
        forrigeAr={forrigeAr ? formatTall(forrigeAr.likviditetsgrad1) : null}
        beskrivelse="Omlopsmidler / kortsiktig gjeld. Bor vaere > 2"
        fargetema={data.likviditetsgrad1 >= 2 ? 'positiv' : data.likviditetsgrad1 >= 1 ? 'noytral' : 'negativ'}
      />
      <NokkeltallRad
        navn="Likviditetsgrad 2"
        verdi={formatTall(data.likviditetsgrad2)}
        forrigeAr={forrigeAr ? formatTall(forrigeAr.likviditetsgrad2) : null}
        beskrivelse="(Omlopsmidler - varelager) / kortsiktig gjeld. Bor vaere > 1"
        fargetema={data.likviditetsgrad2 >= 1 ? 'positiv' : 'negativ'}
      />
      <NokkeltallRad
        navn="Arbeidskapital"
        verdi={`kr ${formatKroner(data.arbeidskapital)}`}
        forrigeAr={forrigeAr ? `kr ${formatKroner(forrigeAr.arbeidskapital)}` : null}
        beskrivelse="Omlopsmidler - kortsiktig gjeld"
        fargetema={data.arbeidskapital > 0 ? 'positiv' : 'negativ'}
      />
    </>
  );
}

function SoliditetSeksjon({ data, forrigeAr }: { data: SoliditetDto; forrigeAr: SoliditetDto | null }) {
  return (
    <>
      <NokkeltallRad
        navn="Egenkapitalandel"
        verdi={formatProsent(data.egenkapitalandel)}
        forrigeAr={forrigeAr ? formatProsent(forrigeAr.egenkapitalandel) : null}
        beskrivelse="Egenkapital / totalkapital. Bor vaere > 30 %"
        fargetema={data.egenkapitalandel >= 30 ? 'positiv' : data.egenkapitalandel >= 15 ? 'noytral' : 'negativ'}
      />
      <NokkeltallRad
        navn="Gjeldsgrad"
        verdi={formatTall(data.gjeldsgrad)}
        forrigeAr={forrigeAr ? formatTall(forrigeAr.gjeldsgrad) : null}
        beskrivelse="Gjeld / egenkapital. Lavere er bedre"
        fargetema={data.gjeldsgrad <= 2 ? 'positiv' : data.gjeldsgrad <= 4 ? 'noytral' : 'negativ'}
      />
      <NokkeltallRad
        navn="Rentedekningsgrad"
        verdi={formatTall(data.rentedekningsgrad)}
        forrigeAr={forrigeAr ? formatTall(forrigeAr.rentedekningsgrad) : null}
        beskrivelse="(Resultat for skatt + rentekostnader) / rentekostnader. Bor vaere > 3"
        fargetema={data.rentedekningsgrad >= 3 ? 'positiv' : data.rentedekningsgrad >= 1 ? 'noytral' : 'negativ'}
      />
    </>
  );
}

function LonnsomhetSeksjon({ data, forrigeAr }: { data: LonnsomhetDto; forrigeAr: LonnsomhetDto | null }) {
  return (
    <>
      <NokkeltallRad
        navn="Totalkapitalrentabilitet"
        verdi={formatProsent(data.totalkapitalrentabilitet)}
        forrigeAr={forrigeAr ? formatProsent(forrigeAr.totalkapitalrentabilitet) : null}
        beskrivelse="(Resultat for skatt + rentekostnader) / gjennomsnittlig totalkapital"
        fargetema={data.totalkapitalrentabilitet > 0 ? 'positiv' : 'negativ'}
      />
      <NokkeltallRad
        navn="Egenkapitalrentabilitet"
        verdi={formatProsent(data.egenkapitalrentabilitet)}
        forrigeAr={forrigeAr ? formatProsent(forrigeAr.egenkapitalrentabilitet) : null}
        beskrivelse="Arsresultat / gjennomsnittlig egenkapital"
        fargetema={data.egenkapitalrentabilitet > 0 ? 'positiv' : 'negativ'}
      />
      <NokkeltallRad
        navn="Resultatmargin"
        verdi={formatProsent(data.resultatmargin)}
        forrigeAr={forrigeAr ? formatProsent(forrigeAr.resultatmargin) : null}
        beskrivelse="Arsresultat / driftsinntekter"
        fargetema={data.resultatmargin > 0 ? 'positiv' : 'negativ'}
      />
      <NokkeltallRad
        navn="Driftsmargin"
        verdi={formatProsent(data.driftsmargin)}
        forrigeAr={forrigeAr ? formatProsent(forrigeAr.driftsmargin) : null}
        beskrivelse="Driftsresultat / driftsinntekter"
        fargetema={data.driftsmargin > 0 ? 'positiv' : 'negativ'}
      />
    </>
  );
}

function SeksjonTabell({ tittel, data, forrigeAr, children }: { tittel: string; data: NokkeltallDto; forrigeAr: NokkeltallDto | null; children: React.ReactNode }) {
  return (
    <div style={{ marginBottom: 32 }}>
      <h3 style={{ margin: '0 0 8px', fontSize: 17 }}>{tittel}</h3>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead>
          <tr style={{ borderBottom: '2px solid #333' }}>
            <th style={{ textAlign: 'left', padding: '8px 12px', fontWeight: 700 }}>Nøkkeltall</th>
            <th style={{ textAlign: 'right', padding: '8px 12px', fontWeight: 700, width: 140 }}>{data.ar}</th>
            {forrigeAr && (
              <th style={{ textAlign: 'right', padding: '8px 12px', fontWeight: 700, width: 140 }}>{forrigeAr.ar}</th>
            )}
            <th style={{ textAlign: 'left', padding: '8px 12px', fontWeight: 700 }}>Forklaring</th>
          </tr>
        </thead>
        <tbody>
          {children}
        </tbody>
      </table>
    </div>
  );
}

export default function NokkeltallPage() {
  const innevaerendeAr = new Date().getFullYear();
  const [ar, setAr] = useState(innevaerendeAr);
  const [periode, setPeriode] = useState(12);
  const [inkluderForrigeAr, setInkluderForrigeAr] = useState(true);

  const { data, isLoading, error } = useNokkeltall({ ar, periode, inkluderForrigeAr });

  return (
    <div style={containerStyle}>
      <style>{printStyle}</style>
      <h1 style={{ margin: 0 }}>Nøkkeltall</h1>
      <p style={{ color: '#666', marginTop: 4 }}>Likviditet, soliditet og lønnsomhet</p>

      <div style={filterStyle} className="no-print">
        <div style={feltStyle}>
          <label style={{ fontWeight: 600, fontSize: 13 }}>År</label>
          <input type="number" value={ar} onChange={(e) => setAr(Number(e.target.value))} style={{ ...inputStyle, width: 100 }} />
        </div>
        <div style={feltStyle}>
          <label style={{ fontWeight: 600, fontSize: 13 }}>Periode</label>
          <select value={periode} onChange={(e) => setPeriode(Number(e.target.value))} style={inputStyle}>
            {Array.from({ length: 12 }, (_, i) => (
              <option key={i + 1} value={i + 1}>{i + 1}</option>
            ))}
          </select>
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

      {isLoading && <p>Laster nøkkeltall...</p>}
      {error && <p style={{ color: 'red' }}>Feil ved lasting av nøkkeltall: {String(error)}</p>}

      {data && (
        <div id="rapport-innhold">
          <div style={{ textAlign: 'center', marginBottom: 24 }}>
            <h2 style={{ margin: 0 }}>Finansielle nøkkeltall</h2>
            <p style={{ margin: '4px 0', color: '#666' }}>Per periode {data.periode}, {data.ar}</p>
          </div>

          <SeksjonTabell tittel="Likviditet" data={data} forrigeAr={data.forrigeAr}>
            <LikviditetSeksjon data={data.likviditet} forrigeAr={data.forrigeAr?.likviditet ?? null} />
          </SeksjonTabell>

          <SeksjonTabell tittel="Soliditet" data={data} forrigeAr={data.forrigeAr}>
            <SoliditetSeksjon data={data.soliditet} forrigeAr={data.forrigeAr?.soliditet ?? null} />
          </SeksjonTabell>

          <SeksjonTabell tittel="Lønnsomhet" data={data} forrigeAr={data.forrigeAr}>
            <LonnsomhetSeksjon data={data.lonnsomhet} forrigeAr={data.forrigeAr?.lonnsomhet ?? null} />
          </SeksjonTabell>
        </div>
      )}
    </div>
  );
}
