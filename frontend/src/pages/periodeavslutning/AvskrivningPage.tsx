import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  useAnleggsmidler,
  useBeregnAvskrivninger,
  useBokforAvskrivninger,
} from '../../hooks/api/usePeriodeavslutning';
import type {
  AnleggsmiddelDto,
  AvskrivningBeregningDto,
  AvskrivningLinjeDto,
} from '../../types/periodeavslutning';
import { formatBelop, formatDato } from '../../utils/formatering';

const currentYear = new Date().getFullYear();
const currentMonth = new Date().getMonth() + 1;

const periodeNavn: Record<number, string> = {
  1: 'Januar', 2: 'Februar', 3: 'Mars', 4: 'April',
  5: 'Mai', 6: 'Juni', 7: 'Juli', 8: 'August',
  9: 'September', 10: 'Oktober', 11: 'November', 12: 'Desember',
};

type Visning = 'oversikt' | 'beregning' | 'bokfort';

export default function AvskrivningPage() {
  const [ar, setAr] = useState(currentYear);
  const [periode, setPeriode] = useState(currentMonth);
  const [visning, setVisning] = useState<Visning>('oversikt');
  const [beregning, setBeregning] = useState<AvskrivningBeregningDto | null>(null);

  const { data: anleggsmidler, isLoading } = useAnleggsmidler(true);
  const beregnAvskrivninger = useBeregnAvskrivninger();
  const bokforAvskrivninger = useBokforAvskrivninger();

  function handleBeregn() {
    beregnAvskrivninger.mutate(
      { ar, periode },
      {
        onSuccess: (data) => {
          setBeregning(data);
          setVisning('beregning');
        },
      },
    );
  }

  function handleBokfor() {
    bokforAvskrivninger.mutate(
      { ar, periode },
      {
        onSuccess: (data) => {
          setBeregning(data);
          setVisning('bokfort');
        },
      },
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Link to="/periodeavslutning" style={{ color: '#0066cc', fontSize: 13, textDecoration: 'none' }}>
        &larr; Tilbake til periodeoversikt
      </Link>

      <h1 style={{ marginTop: 12 }}>Avskrivninger</h1>

      {/* Periode-velger og handlinger */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 24, flexWrap: 'wrap' }}>
        <label style={{ fontWeight: 600 }}>Periode:</label>
        <select value={ar} onChange={(e) => setAr(Number(e.target.value))} style={selectStyle}>
          {Array.from({ length: 5 }, (_, i) => currentYear - 2 + i).map((y) => (
            <option key={y} value={y}>{y}</option>
          ))}
        </select>
        <select value={periode} onChange={(e) => setPeriode(Number(e.target.value))} style={selectStyle}>
          {Array.from({ length: 12 }, (_, i) => i + 1).map((p) => (
            <option key={p} value={p}>{periodeNavn[p]}</option>
          ))}
        </select>

        <button
          onClick={handleBeregn}
          disabled={beregnAvskrivninger.isPending}
          style={primaryButtonStyle}
        >
          {beregnAvskrivninger.isPending ? 'Beregner...' : 'Beregn avskrivninger'}
        </button>

        {visning === 'beregning' && beregning && beregning.linjer.length > 0 && (
          <button
            onClick={handleBokfor}
            disabled={bokforAvskrivninger.isPending}
            style={{ ...primaryButtonStyle, background: '#2e7d32' }}
          >
            {bokforAvskrivninger.isPending ? 'Bokforer...' : 'Bokfor avskrivninger'}
          </button>
        )}
      </div>

      {beregnAvskrivninger.isError && (
        <div style={{ padding: 12, background: '#ffebee', border: '1px solid #ef9a9a', borderRadius: 8, marginBottom: 16, fontSize: 14 }}>
          Feil ved beregning av avskrivninger. Kontroller at perioden er apen.
        </div>
      )}

      {bokforAvskrivninger.isError && (
        <div style={{ padding: 12, background: '#ffebee', border: '1px solid #ef9a9a', borderRadius: 8, marginBottom: 16, fontSize: 14 }}>
          Feil ved bokforing av avskrivninger.
        </div>
      )}

      {/* Bokfort-melding */}
      {visning === 'bokfort' && beregning && (
        <div style={{
          padding: 16,
          marginBottom: 24,
          borderRadius: 8,
          background: '#e8f5e9',
          border: '1px solid #a5d6a7',
        }}>
          <strong style={{ color: '#2e7d32' }}>
            Avskrivninger bokfort for {periodeNavn[beregning.periode]} {beregning.ar}
          </strong>
          <div style={{ fontSize: 14, marginTop: 4 }}>
            {beregning.antallAnleggsmidler} anleggsmidler, totalt {formatBelop(beregning.totalAvskrivning)}
          </div>
        </div>
      )}

      {/* Beregningsresultat-tabell */}
      {(visning === 'beregning' || visning === 'bokfort') && beregning && (
        <div style={{ marginBottom: 32 }}>
          <h2 style={{ fontSize: 18, marginBottom: 12 }}>
            {visning === 'bokfort' ? 'Bokforte' : 'Beregnede'} avskrivninger - {periodeNavn[beregning.periode]} {beregning.ar}
          </h2>

          {beregning.linjer.length === 0 ? (
            <div style={{ padding: 24, textAlign: 'center', border: '1px solid #e0e0e0', borderRadius: 8, color: '#666' }}>
              Ingen avskrivninger a beregne for denne perioden.
            </div>
          ) : (
            <>
              <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
                <thead>
                  <tr>
                    <th style={thStyle}>Anleggsmiddel</th>
                    <th style={thStyle}>Avskr.konto</th>
                    <th style={{ ...thStyle, textAlign: 'right' }}>Belop</th>
                    <th style={{ ...thStyle, textAlign: 'right' }}>Akkumulert for</th>
                    <th style={{ ...thStyle, textAlign: 'right' }}>Akkumulert etter</th>
                    <th style={{ ...thStyle, textAlign: 'right' }}>Bokfort verdi etter</th>
                    <th style={thStyle}>Merknad</th>
                  </tr>
                </thead>
                <tbody>
                  {beregning.linjer.map((linje: AvskrivningLinjeDto) => (
                    <tr key={linje.anleggsmiddelId}>
                      <td style={tdStyle}>
                        <Link to={`/periodeavslutning/anleggsmidler/${linje.anleggsmiddelId}`} style={{ color: '#0066cc', textDecoration: 'none' }}>
                          {linje.navn}
                        </Link>
                      </td>
                      <td style={tdStyle}>{linje.avskrivningsKontonummer}</td>
                      <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                        {formatBelop(linje.belop)}
                      </td>
                      <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                        {formatBelop(linje.akkumulertFor)}
                      </td>
                      <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                        {formatBelop(linje.akkumulertEtter)}
                      </td>
                      <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                        {formatBelop(linje.bokfortVerdiEtter)}
                      </td>
                      <td style={tdStyle}>
                        {linje.erSisteAvskrivning && (
                          <span style={{ fontSize: 12, color: '#e65100', fontWeight: 600 }}>
                            Siste avskrivning
                          </span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr style={{ background: '#f8f8f8' }}>
                    <td style={{ ...tdStyle, fontWeight: 700 }} colSpan={2}>Sum</td>
                    <td style={{ ...tdStyle, textAlign: 'right', fontWeight: 700, fontFamily: 'monospace' }}>
                      {formatBelop(beregning.totalAvskrivning)}
                    </td>
                    <td colSpan={4} style={tdStyle} />
                  </tr>
                </tfoot>
              </table>
            </>
          )}
        </div>
      )}

      {/* Anleggsmiddelregister */}
      <h2 style={{ fontSize: 18, marginBottom: 12 }}>Anleggsmiddelregister</h2>

      {isLoading ? (
        <p>Laster anleggsmidler...</p>
      ) : !anleggsmidler || anleggsmidler.length === 0 ? (
        <div style={{ padding: 24, textAlign: 'center', border: '1px solid #e0e0e0', borderRadius: 8 }}>
          <p style={{ color: '#666' }}>Ingen aktive anleggsmidler registrert.</p>
          <Link to="/periodeavslutning/anleggsmidler" style={primaryButtonStyle}>
            Registrer anleggsmiddel
          </Link>
        </div>
      ) : (
        <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
          <thead>
            <tr>
              <th style={thStyle}>Navn</th>
              <th style={thStyle}>Anskaffet</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Anskaffelseskost</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Bokfort verdi</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Mnd. avskrivning</th>
              <th style={thStyle}>Status</th>
            </tr>
          </thead>
          <tbody>
            {anleggsmidler.map((am: AnleggsmiddelDto) => (
              <tr key={am.id}>
                <td style={tdStyle}>
                  <Link to={`/periodeavslutning/anleggsmidler/${am.id}`} style={{ color: '#0066cc', textDecoration: 'none' }}>
                    {am.navn}
                  </Link>
                </td>
                <td style={tdStyle}>{formatDato(am.anskaffelsesdato)}</td>
                <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(am.anskaffelseskostnad)}
                </td>
                <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(am.bokfortVerdi)}
                </td>
                <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(am.manedligAvskrivning)}
                </td>
                <td style={tdStyle}>
                  {am.erFulltAvskrevet ? (
                    <span style={{ padding: '2px 8px', borderRadius: 12, fontSize: 12, fontWeight: 600, background: '#fff3e0', color: '#e65100' }}>
                      Fullt avskrevet
                    </span>
                  ) : (
                    <span style={{ padding: '2px 8px', borderRadius: 12, fontSize: 12, fontWeight: 600, background: '#e8f5e9', color: '#2e7d32' }}>
                      Aktiv
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
