import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  usePeriodiseringer,
  useOpprettPeriodisering,
  useBokforPeriodiseringer,
} from '../../hooks/api/usePeriodeavslutning';
import {
  PeriodiseringsType,
  PeriodiseringsTypeNavn,
} from '../../types/periodeavslutning';
import type {
  PeriodiseringDto,
  OpprettPeriodiseringRequest,
  PeriodiseringBokforingDto,
  PeriodiseringLinjeDto,
} from '../../types/periodeavslutning';
import { formatBelop } from '../../utils/formatering';

const currentYear = new Date().getFullYear();
const currentMonth = new Date().getMonth() + 1;

const periodeNavn: Record<number, string> = {
  1: 'Januar', 2: 'Februar', 3: 'Mars', 4: 'April',
  5: 'Mai', 6: 'Juni', 7: 'Juli', 8: 'August',
  9: 'September', 10: 'Oktober', 11: 'November', 12: 'Desember',
};

type Visning = 'liste' | 'opprett' | 'bokfor';

const tomtSkjema: OpprettPeriodiseringRequest = {
  beskrivelse: '',
  type: PeriodiseringsType.ForskuddsbetaltKostnad,
  totalBelop: 0,
  fraAr: currentYear,
  fraPeriode: currentMonth,
  tilAr: currentYear,
  tilPeriode: 12,
  balanseKontonummer: '',
  resultatKontonummer: '',
};

export default function PeriodiseringPage() {
  const [visning, setVisning] = useState<Visning>('liste');
  const [visAktive, setVisAktive] = useState(true);
  const [bokforAr, setBokforAr] = useState(currentYear);
  const [bokforPeriode, setBokforPeriode] = useState(currentMonth);
  const [bokforResultat, setBokforResultat] = useState<PeriodiseringBokforingDto | null>(null);
  const [skjema, setSkjema] = useState<OpprettPeriodiseringRequest>({ ...tomtSkjema });

  const { data: periodiseringer, isLoading } = usePeriodiseringer(visAktive ? true : undefined);
  const opprettPeriodisering = useOpprettPeriodisering();
  const bokforPeriodiseringer = useBokforPeriodiseringer();

  function handleOpprett() {
    if (!skjema.beskrivelse || !skjema.balanseKontonummer || !skjema.resultatKontonummer || skjema.totalBelop <= 0) {
      return;
    }
    opprettPeriodisering.mutate(skjema, {
      onSuccess: () => {
        setSkjema({ ...tomtSkjema });
        setVisning('liste');
      },
    });
  }

  function handleBokfor() {
    bokforPeriodiseringer.mutate(
      { ar: bokforAr, periode: bokforPeriode },
      {
        onSuccess: (data) => {
          setBokforResultat(data);
        },
      },
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Link to="/periodeavslutning" style={{ color: '#0066cc', fontSize: 13, textDecoration: 'none' }}>
        &larr; Tilbake til periodeoversikt
      </Link>

      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 12, marginBottom: 24 }}>
        <h1 style={{ margin: 0 }}>Periodiseringer</h1>
        <div style={{ display: 'flex', gap: 8 }}>
          <button
            onClick={() => setVisning(visning === 'opprett' ? 'liste' : 'opprett')}
            style={visning === 'opprett' ? secondaryButtonStyle : primaryButtonStyle}
          >
            {visning === 'opprett' ? 'Avbryt' : 'Ny periodisering'}
          </button>
          <button
            onClick={() => setVisning(visning === 'bokfor' ? 'liste' : 'bokfor')}
            style={visning === 'bokfor' ? secondaryButtonStyle : { ...primaryButtonStyle, background: '#2e7d32' }}
          >
            {visning === 'bokfor' ? 'Avbryt' : 'Bokfor periodiseringer'}
          </button>
        </div>
      </div>

      {/* Opprett periodisering */}
      {visning === 'opprett' && (
        <div style={{ border: '1px solid #e0e0e0', borderRadius: 8, padding: 24, marginBottom: 24, background: '#fafafa' }}>
          <h2 style={{ fontSize: 18, marginTop: 0, marginBottom: 16 }}>Ny periodisering</h2>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <div style={{ gridColumn: '1 / -1' }}>
              <label style={labelStyle}>Beskrivelse</label>
              <input
                type="text"
                value={skjema.beskrivelse}
                onChange={(e) => setSkjema({ ...skjema, beskrivelse: e.target.value })}
                placeholder="F.eks. Forskuddsbetalt forsikring 2026"
                style={{ ...inputStyle, width: '100%' }}
              />
            </div>

            <div>
              <label style={labelStyle}>Type</label>
              <select
                value={skjema.type}
                onChange={(e) => setSkjema({ ...skjema, type: e.target.value as PeriodiseringsType })}
                style={selectStyle}
              >
                {Object.entries(PeriodiseringsTypeNavn).map(([key, navn]) => (
                  <option key={key} value={key}>{navn}</option>
                ))}
              </select>
            </div>

            <div>
              <label style={labelStyle}>Totalbelop</label>
              <input
                type="number"
                value={skjema.totalBelop || ''}
                onChange={(e) => setSkjema({ ...skjema, totalBelop: parseFloat(e.target.value) || 0 })}
                placeholder="0,00"
                style={inputStyle}
                min={0}
                step={0.01}
              />
            </div>

            <div>
              <label style={labelStyle}>Fra periode</label>
              <div style={{ display: 'flex', gap: 8 }}>
                <select value={skjema.fraAr} onChange={(e) => setSkjema({ ...skjema, fraAr: Number(e.target.value) })} style={selectStyle}>
                  {Array.from({ length: 5 }, (_, i) => currentYear - 1 + i).map((y) => (
                    <option key={y} value={y}>{y}</option>
                  ))}
                </select>
                <select value={skjema.fraPeriode} onChange={(e) => setSkjema({ ...skjema, fraPeriode: Number(e.target.value) })} style={selectStyle}>
                  {Array.from({ length: 12 }, (_, i) => i + 1).map((p) => (
                    <option key={p} value={p}>{periodeNavn[p]}</option>
                  ))}
                </select>
              </div>
            </div>

            <div>
              <label style={labelStyle}>Til periode</label>
              <div style={{ display: 'flex', gap: 8 }}>
                <select value={skjema.tilAr} onChange={(e) => setSkjema({ ...skjema, tilAr: Number(e.target.value) })} style={selectStyle}>
                  {Array.from({ length: 5 }, (_, i) => currentYear - 1 + i).map((y) => (
                    <option key={y} value={y}>{y}</option>
                  ))}
                </select>
                <select value={skjema.tilPeriode} onChange={(e) => setSkjema({ ...skjema, tilPeriode: Number(e.target.value) })} style={selectStyle}>
                  {Array.from({ length: 12 }, (_, i) => i + 1).map((p) => (
                    <option key={p} value={p}>{periodeNavn[p]}</option>
                  ))}
                </select>
              </div>
            </div>

            <div>
              <label style={labelStyle}>Balansekonto</label>
              <input
                type="text"
                value={skjema.balanseKontonummer}
                onChange={(e) => setSkjema({ ...skjema, balanseKontonummer: e.target.value })}
                placeholder="F.eks. 1700"
                style={inputStyle}
              />
            </div>

            <div>
              <label style={labelStyle}>Resultatkonto</label>
              <input
                type="text"
                value={skjema.resultatKontonummer}
                onChange={(e) => setSkjema({ ...skjema, resultatKontonummer: e.target.value })}
                placeholder="F.eks. 6300"
                style={inputStyle}
              />
            </div>

            <div>
              <label style={labelStyle}>Avdelingskode (valgfritt)</label>
              <input
                type="text"
                value={skjema.avdelingskode ?? ''}
                onChange={(e) => setSkjema({ ...skjema, avdelingskode: e.target.value || undefined })}
                style={inputStyle}
              />
            </div>

            <div>
              <label style={labelStyle}>Prosjektkode (valgfritt)</label>
              <input
                type="text"
                value={skjema.prosjektkode ?? ''}
                onChange={(e) => setSkjema({ ...skjema, prosjektkode: e.target.value || undefined })}
                style={inputStyle}
              />
            </div>
          </div>

          <div style={{ display: 'flex', gap: 12, marginTop: 24 }}>
            <button onClick={() => setVisning('liste')} style={secondaryButtonStyle}>
              Avbryt
            </button>
            <button
              onClick={handleOpprett}
              disabled={opprettPeriodisering.isPending || !skjema.beskrivelse || skjema.totalBelop <= 0 || !skjema.balanseKontonummer || !skjema.resultatKontonummer}
              style={{
                ...primaryButtonStyle,
                opacity: (!skjema.beskrivelse || skjema.totalBelop <= 0 || !skjema.balanseKontonummer || !skjema.resultatKontonummer) ? 0.5 : 1,
              }}
            >
              {opprettPeriodisering.isPending ? 'Oppretter...' : 'Opprett periodisering'}
            </button>
          </div>

          {opprettPeriodisering.isError && (
            <p style={{ color: 'red', marginTop: 12, fontSize: 14 }}>Feil ved oppretting av periodisering.</p>
          )}
        </div>
      )}

      {/* Bokfor periodiseringer */}
      {visning === 'bokfor' && (
        <div style={{ border: '1px solid #e0e0e0', borderRadius: 8, padding: 24, marginBottom: 24, background: '#fafafa' }}>
          <h2 style={{ fontSize: 18, marginTop: 0, marginBottom: 16 }}>Bokfor periodiseringer for periode</h2>

          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 16 }}>
            <select value={bokforAr} onChange={(e) => setBokforAr(Number(e.target.value))} style={selectStyle}>
              {Array.from({ length: 5 }, (_, i) => currentYear - 2 + i).map((y) => (
                <option key={y} value={y}>{y}</option>
              ))}
            </select>
            <select value={bokforPeriode} onChange={(e) => setBokforPeriode(Number(e.target.value))} style={selectStyle}>
              {Array.from({ length: 12 }, (_, i) => i + 1).map((p) => (
                <option key={p} value={p}>{periodeNavn[p]}</option>
              ))}
            </select>
            <button
              onClick={handleBokfor}
              disabled={bokforPeriodiseringer.isPending}
              style={{ ...primaryButtonStyle, background: '#2e7d32' }}
            >
              {bokforPeriodiseringer.isPending ? 'Bokforer...' : 'Bokfor'}
            </button>
          </div>

          {bokforPeriodiseringer.isError && (
            <p style={{ color: 'red', fontSize: 14 }}>Feil ved bokforing av periodiseringer.</p>
          )}

          {bokforResultat && (
            <div>
              <div style={{
                padding: 12,
                marginBottom: 16,
                borderRadius: 8,
                background: '#e8f5e9',
                border: '1px solid #a5d6a7',
              }}>
                <strong style={{ color: '#2e7d32' }}>
                  Periodiseringer bokfort for {periodeNavn[bokforResultat.periode]} {bokforResultat.ar}
                </strong>
                <span style={{ marginLeft: 12, fontSize: 14 }}>
                  Totalt: {formatBelop(bokforResultat.totalBelop)} | Bilag: {bokforResultat.bilagId}
                </span>
              </div>

              {bokforResultat.linjer.length > 0 && (
                <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
                  <thead>
                    <tr>
                      <th style={thStyle}>Beskrivelse</th>
                      <th style={thStyle}>Type</th>
                      <th style={{ ...thStyle, textAlign: 'right' }}>Belop</th>
                      <th style={{ ...thStyle, textAlign: 'right' }}>Gjenstaar etter</th>
                    </tr>
                  </thead>
                  <tbody>
                    {bokforResultat.linjer.map((linje: PeriodiseringLinjeDto) => (
                      <tr key={linje.periodiseringId}>
                        <td style={tdStyle}>{linje.beskrivelse}</td>
                        <td style={tdStyle}>{PeriodiseringsTypeNavn[linje.type]}</td>
                        <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                          {formatBelop(linje.belop)}
                        </td>
                        <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                          {formatBelop(linje.gjenstaarEtter)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          )}
        </div>
      )}

      {/* Filter */}
      <div style={{ marginBottom: 16, display: 'flex', alignItems: 'center', gap: 12 }}>
        <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 14 }}>
          <input
            type="checkbox"
            checked={visAktive}
            onChange={(e) => setVisAktive(e.target.checked)}
          />
          Vis kun aktive
        </label>
      </div>

      {/* Periodiseringsliste */}
      {isLoading ? (
        <p>Laster periodiseringer...</p>
      ) : !periodiseringer || periodiseringer.length === 0 ? (
        <div style={{ padding: 32, textAlign: 'center', border: '1px solid #e0e0e0', borderRadius: 8 }}>
          <p style={{ color: '#666' }}>Ingen periodiseringer funnet.</p>
        </div>
      ) : (
        <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
          <thead>
            <tr>
              <th style={thStyle}>Beskrivelse</th>
              <th style={thStyle}>Type</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Totalbelop</th>
              <th style={thStyle}>Periode</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Periodisert</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Gjenstaar</th>
              <th style={thStyle}>Status</th>
            </tr>
          </thead>
          <tbody>
            {periodiseringer.map((p: PeriodiseringDto) => (
              <tr key={p.id}>
                <td style={tdStyle}>
                  <strong>{p.beskrivelse}</strong>
                </td>
                <td style={tdStyle}>
                  <span style={{ fontSize: 13 }}>{PeriodiseringsTypeNavn[p.type]}</span>
                </td>
                <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(p.totalBelop)}
                </td>
                <td style={tdStyle}>
                  <span style={{ fontSize: 13 }}>
                    {periodeNavn[p.fraPeriode]} {p.fraAr} - {periodeNavn[p.tilPeriode]} {p.tilAr}
                  </span>
                </td>
                <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(p.sumPeriodisert)}
                </td>
                <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(p.gjenstaaendeBelop)}
                </td>
                <td style={tdStyle}>
                  {p.erAktiv ? (
                    <span style={{ padding: '2px 8px', borderRadius: 12, fontSize: 12, fontWeight: 600, background: '#e8f5e9', color: '#2e7d32' }}>
                      Aktiv
                    </span>
                  ) : (
                    <span style={{ padding: '2px 8px', borderRadius: 12, fontSize: 12, fontWeight: 600, background: '#f5f5f5', color: '#666' }}>
                      Fullfort
                    </span>
                  )}
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

const selectStyle: React.CSSProperties = {
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
};

const inputStyle: React.CSSProperties = {
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  width: 200,
};

const labelStyle: React.CSSProperties = {
  display: 'block',
  fontWeight: 600,
  marginBottom: 4,
  fontSize: 14,
};

const primaryButtonStyle: React.CSSProperties = {
  padding: '10px 24px',
  background: '#0066cc',
  color: '#fff',
  border: 'none',
  borderRadius: 4,
  fontSize: 14,
  cursor: 'pointer',
  textDecoration: 'none',
  display: 'inline-block',
};

const secondaryButtonStyle: React.CSSProperties = {
  padding: '10px 24px',
  background: '#f5f5f5',
  color: '#333',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  cursor: 'pointer',
};
