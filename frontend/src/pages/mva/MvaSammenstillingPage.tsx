import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useMvaSammenstilling, useMvaSammenstillingDetalj } from '../../hooks/api/useMva';
import { formatBelop, formatMvaSats, formatDato } from '../../utils/formatering';

const currentYear = new Date().getFullYear();

export default function MvaSammenstillingPage() {
  const [ar, setAr] = useState(currentYear);
  const [termin, setTermin] = useState<number | undefined>(undefined);
  const [valgtMvaKode, setValgtMvaKode] = useState<string>('');

  const { data: sammenstilling, isLoading } = useMvaSammenstilling({
    ar,
    termin,
  });

  const { data: detalj, isLoading: detaljLaster } = useMvaSammenstillingDetalj({
    ar,
    termin,
    mvaKode: valgtMvaKode,
  });

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Link to="/mva" style={{ color: '#0066cc', textDecoration: 'none' }}>
        &larr; Tilbake til MVA-oversikt
      </Link>

      <h1 style={{ marginTop: 16, marginBottom: 24 }}>MVA-sammenstilling</h1>

      {/* Filtre */}
      <div style={{ display: 'flex', gap: 16, marginBottom: 24, alignItems: 'flex-end' }}>
        <div>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 14 }}>
            Regnskaps&aring;r
          </label>
          <select
            value={ar}
            onChange={(e) => {
              setAr(Number(e.target.value));
              setValgtMvaKode('');
            }}
            style={selectStyle}
          >
            {Array.from({ length: 5 }, (_, i) => currentYear - 2 + i).map((year) => (
              <option key={year} value={year}>
                {year}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 14 }}>
            Termin
          </label>
          <select
            value={termin ?? ''}
            onChange={(e) => {
              const v = e.target.value;
              setTermin(v === '' ? undefined : Number(v));
              setValgtMvaKode('');
            }}
            style={selectStyle}
          >
            <option value="">Alle terminer</option>
            {[1, 2, 3, 4, 5, 6].map((t) => (
              <option key={t} value={t}>
                Termin {t}
              </option>
            ))}
          </select>
        </div>
      </div>

      {isLoading ? (
        <p>Laster sammenstilling...</p>
      ) : sammenstilling ? (
        <>
          {/* Periodeinfo */}
          <div style={{ marginBottom: 16, fontSize: 13, color: '#666' }}>
            Periode: {formatDato(sammenstilling.fraDato)} &ndash;{' '}
            {formatDato(sammenstilling.tilDato)}
          </div>

          {/* Gruppetabell */}
          <table
            style={{
              width: '100%',
              borderCollapse: 'collapse',
              border: '1px solid #e0e0e0',
              marginBottom: 24,
            }}
          >
            <thead>
              <tr>
                <th style={thStyle}>MVA-kode</th>
                <th style={thStyle}>Beskrivelse</th>
                <th style={thStyle}>SAF-T kode</th>
                <th style={thStyle}>Sats</th>
                <th style={thStyle}>Retning</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Grunnlag</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>MVA-bel&oslash;p</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Ant.</th>
                <th style={thStyle} />
              </tr>
            </thead>
            <tbody>
              {sammenstilling.grupper.map((gruppe) => (
                <tr
                  key={gruppe.mvaKode}
                  style={{
                    backgroundColor:
                      valgtMvaKode === gruppe.mvaKode ? '#e3f2fd' : '#fff',
                  }}
                >
                  <td style={tdStyle}>
                    <strong>{gruppe.mvaKode}</strong>
                  </td>
                  <td style={tdStyle}>{gruppe.beskrivelse}</td>
                  <td style={tdStyle}>{gruppe.standardTaxCode}</td>
                  <td style={tdStyle}>{formatMvaSats(gruppe.sats)}</td>
                  <td style={tdStyle}>{retningNavn(gruppe.retning)}</td>
                  <td style={monoRight}>{formatBelop(gruppe.sumGrunnlag)}</td>
                  <td style={monoRight}>{formatBelop(gruppe.sumMvaBelop)}</td>
                  <td style={monoRight}>{gruppe.antallPosteringer}</td>
                  <td style={tdStyle}>
                    <button
                      onClick={() =>
                        setValgtMvaKode(
                          valgtMvaKode === gruppe.mvaKode ? '' : gruppe.mvaKode,
                        )
                      }
                      style={{
                        padding: '2px 10px',
                        border: '1px solid #ccc',
                        borderRadius: 4,
                        background: '#fff',
                        cursor: 'pointer',
                        fontSize: 12,
                      }}
                    >
                      {valgtMvaKode === gruppe.mvaKode ? 'Skjul' : 'Detaljer'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr style={{ backgroundColor: '#f8f8f8' }}>
                <td colSpan={5} style={{ ...sumFooter, textAlign: 'left' }}>
                  Totalt
                </td>
                <td style={sumFooter}>
                  {formatBelop(sammenstilling.totaltMvaGrunnlag)}
                </td>
                <td style={sumFooter}>
                  {formatBelop(sammenstilling.totaltMvaBelop)}
                </td>
                <td style={sumFooter} />
                <td style={sumFooter} />
              </tr>
            </tfoot>
          </table>

          {/* Detaljer for valgt MVA-kode */}
          {valgtMvaKode && (
            <div style={{ marginTop: 8 }}>
              <h2 style={{ fontSize: 18, marginBottom: 12 }}>
                Posteringer for MVA-kode {valgtMvaKode}
              </h2>
              {detaljLaster ? (
                <p>Laster posteringer...</p>
              ) : detalj ? (
                <>
                  <div style={{ marginBottom: 8, fontSize: 13, color: '#666' }}>
                    {detalj.beskrivelse} &mdash; {detalj.totaltAntall} posteringer
                  </div>
                  <table
                    style={{
                      width: '100%',
                      borderCollapse: 'collapse',
                      border: '1px solid #e0e0e0',
                    }}
                  >
                    <thead>
                      <tr>
                        <th style={thStyle}>Bilag</th>
                        <th style={thStyle}>Dato</th>
                        <th style={thStyle}>Konto</th>
                        <th style={thStyle}>Beskrivelse</th>
                        <th style={thStyle}>Side</th>
                        <th style={{ ...thStyle, textAlign: 'right' }}>Bel&oslash;p</th>
                        <th style={{ ...thStyle, textAlign: 'right' }}>Grunnlag</th>
                        <th style={{ ...thStyle, textAlign: 'right' }}>MVA</th>
                        <th style={thStyle}>Auto</th>
                      </tr>
                    </thead>
                    <tbody>
                      {detalj.posteringer.map((p) => (
                        <tr
                          key={p.posteringId}
                          style={{
                            backgroundColor: p.erAutoGenerertMva ? '#fafafa' : '#fff',
                          }}
                        >
                          <td style={tdStyle}>
                            <Link
                              to={`/bilag/${p.bilagId}`}
                              style={{ color: '#0066cc' }}
                            >
                              {p.bilagsnummer}
                            </Link>
                          </td>
                          <td style={tdStyle}>{formatDato(p.bilagsdato)}</td>
                          <td style={tdStyle}>{p.kontonummer}</td>
                          <td style={tdStyle}>{p.beskrivelse}</td>
                          <td style={tdStyle}>{p.side}</td>
                          <td style={monoRight}>{formatBelop(p.belop)}</td>
                          <td style={monoRight}>{formatBelop(p.mvaGrunnlag)}</td>
                          <td style={monoRight}>{formatBelop(p.mvaBelop)}</td>
                          <td style={{ ...tdStyle, textAlign: 'center' }}>
                            {p.erAutoGenerertMva ? 'Ja' : ''}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                    <tfoot>
                      <tr style={{ backgroundColor: '#f8f8f8' }}>
                        <td colSpan={6} style={{ ...sumFooter, textAlign: 'left' }}>
                          Sum
                        </td>
                        <td style={sumFooter}>{formatBelop(detalj.sumGrunnlag)}</td>
                        <td style={sumFooter}>{formatBelop(detalj.sumMvaBelop)}</td>
                        <td style={sumFooter} />
                      </tr>
                    </tfoot>
                  </table>
                </>
              ) : null}
            </div>
          )}
        </>
      ) : (
        <p style={{ color: '#666' }}>Ingen data for valgt periode.</p>
      )}
    </div>
  );
}

function retningNavn(retning: string): string {
  switch (retning) {
    case 'Utgaende':
      return 'Utg\u00E5ende';
    case 'Inngaende':
      return 'Inng\u00E5ende';
    case 'SnuddAvregning':
      return 'Snudd avregning';
    default:
      return retning;
  }
}

const selectStyle: React.CSSProperties = {
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  backgroundColor: '#fff',
};

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

const monoRight: React.CSSProperties = {
  ...tdStyle,
  textAlign: 'right',
  fontFamily: 'monospace',
};

const sumFooter: React.CSSProperties = {
  padding: '8px 12px',
  fontWeight: 700,
  borderTop: '2px solid #333',
  textAlign: 'right',
  fontFamily: 'monospace',
  fontSize: 14,
};
