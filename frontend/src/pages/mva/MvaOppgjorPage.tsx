import { useParams, Link } from 'react-router-dom';
import { useMvaOppgjor, useBeregnOppgjor, useBokforOppgjor } from '../../hooks/api/useMva';
import { formatBelop, formatMvaSats } from '../../utils/formatering';

export default function MvaOppgjorPage() {
  const { terminId } = useParams<{ terminId: string }>();
  const { data: oppgjor, isLoading, error } = useMvaOppgjor(terminId ?? '');
  const beregnOppgjor = useBeregnOppgjor();
  const bokforOppgjor = useBokforOppgjor();

  if (!terminId) {
    return <div style={{ padding: 24 }}>Ugyldig termin-ID.</div>;
  }

  if (error) {
    return (
      <div style={{ padding: 24 }}>
        <Link to="/mva" style={{ color: '#0066cc' }}>
          &larr; Tilbake til MVA-oversikt
        </Link>
        <h1>MVA-oppgj&oslash;r</h1>
        <div
          style={{
            padding: 32,
            textAlign: 'center',
            border: '1px solid #e0e0e0',
            borderRadius: 8,
          }}
        >
          <p style={{ color: '#666', marginBottom: 16 }}>
            Ingen oppgj&oslash;r er beregnet for denne terminen.
          </p>
          <button
            onClick={() => beregnOppgjor.mutate(terminId)}
            disabled={beregnOppgjor.isPending}
            style={{
              padding: '10px 24px',
              background: '#0066cc',
              color: '#fff',
              border: 'none',
              borderRadius: 4,
              fontSize: 14,
              cursor: 'pointer',
            }}
          >
            {beregnOppgjor.isPending ? 'Beregner...' : 'Beregn oppgj\u00F8r'}
          </button>
          {beregnOppgjor.isError && (
            <p style={{ color: 'red', marginTop: 8 }}>Feil ved beregning. Pr&oslash;v igjen.</p>
          )}
        </div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div style={{ padding: 24 }}>
        <p>Laster oppgj&oslash;r...</p>
      </div>
    );
  }

  if (!oppgjor) {
    return null;
  }

  const utgaendeLinjer = oppgjor.linjer.filter(
    (l) => l.retning === 'Utgaende',
  );
  const inngaendeLinjer = oppgjor.linjer.filter(
    (l) => l.retning === 'Inngaende',
  );
  const snuddAvregningLinjer = oppgjor.linjer.filter(
    (l) => l.retning === 'SnuddAvregning',
  );

  const erTilgode = oppgjor.mvaTilBetaling < 0;

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Link to="/mva" style={{ color: '#0066cc', textDecoration: 'none' }}>
        &larr; Tilbake til MVA-oversikt
      </Link>

      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginTop: 16,
          marginBottom: 24,
        }}
      >
        <h1 style={{ margin: 0 }}>MVA-oppgj&oslash;r: {oppgjor.terminnavn}</h1>
        <div style={{ display: 'flex', gap: 8 }}>
          {!oppgjor.erLast && (
            <>
              <button
                onClick={() => beregnOppgjor.mutate(terminId)}
                disabled={beregnOppgjor.isPending}
                style={{
                  padding: '8px 16px',
                  background: '#f0f0f0',
                  border: '1px solid #ccc',
                  borderRadius: 4,
                  fontSize: 14,
                  cursor: 'pointer',
                }}
              >
                {beregnOppgjor.isPending ? 'Beregner...' : 'Beregn p\u00E5 nytt'}
              </button>
              <button
                onClick={() => bokforOppgjor.mutate(terminId)}
                disabled={bokforOppgjor.isPending}
                style={{
                  padding: '8px 16px',
                  background: '#0066cc',
                  color: '#fff',
                  border: 'none',
                  borderRadius: 4,
                  fontSize: 14,
                  cursor: 'pointer',
                }}
              >
                {bokforOppgjor.isPending ? 'Bokf\u00F8rer...' : 'Bokf\u00F8r oppgj\u00F8rsbilag'}
              </button>
            </>
          )}
        </div>
      </div>

      {oppgjor.erLast && (
        <div
          style={{
            padding: '8px 16px',
            backgroundColor: '#fff3e0',
            borderRadius: 4,
            marginBottom: 16,
            color: '#e65100',
          }}
        >
          Oppgj&oslash;ret er l&aring;st og kan ikke endres.
        </div>
      )}

      <div style={{ fontSize: 13, color: '#666', marginBottom: 24 }}>
        Beregnet: {new Date(oppgjor.beregnetTidspunkt).toLocaleString('nb-NO')} av{' '}
        {oppgjor.beregnetAv}
      </div>

      {/* Utgaende MVA */}
      {utgaendeLinjer.length > 0 && (
        <div style={{ marginBottom: 24 }}>
          <h2 style={{ fontSize: 18, marginBottom: 8 }}>Utg&aring;ende MVA (skyldig)</h2>
          <table
            style={{
              width: '100%',
              borderCollapse: 'collapse',
              border: '1px solid #e0e0e0',
            }}
          >
            <thead>
              <tr>
                <th style={thStyle}>MVA-kode</th>
                <th style={thStyle}>Sats</th>
                <th style={thStyle}>RF-post</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Grunnlag</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>MVA-bel&oslash;p</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Ant. posteringer</th>
              </tr>
            </thead>
            <tbody>
              {utgaendeLinjer.map((linje) => (
                <tr key={linje.mvaKode}>
                  <td style={tdStyle}>{linje.mvaKode} ({linje.standardTaxCode})</td>
                  <td style={tdStyle}>{formatMvaSats(linje.sats)}</td>
                  <td style={tdStyle}>{linje.rfPostnummer}</td>
                  <td style={monoRight}>{formatBelop(linje.sumGrunnlag)}</td>
                  <td style={monoRight}>{formatBelop(linje.sumMvaBelop)}</td>
                  <td style={monoRight}>{linje.antallPosteringer}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr style={{ backgroundColor: '#f8f8f8' }}>
                <td colSpan={3} style={{ ...sumStyle, textAlign: 'left' }}>
                  Sum utg&aring;ende MVA
                </td>
                <td style={sumStyle}>
                  {formatBelop(utgaendeLinjer.reduce((s, l) => s + l.sumGrunnlag, 0))}
                </td>
                <td style={sumStyle}>{formatBelop(oppgjor.sumUtgaendeMva)}</td>
                <td style={sumStyle} />
              </tr>
            </tfoot>
          </table>
        </div>
      )}

      {/* Inngaende MVA */}
      {inngaendeLinjer.length > 0 && (
        <div style={{ marginBottom: 24 }}>
          <h2 style={{ fontSize: 18, marginBottom: 8 }}>Inng&aring;ende MVA (fradrag)</h2>
          <table
            style={{
              width: '100%',
              borderCollapse: 'collapse',
              border: '1px solid #e0e0e0',
            }}
          >
            <thead>
              <tr>
                <th style={thStyle}>MVA-kode</th>
                <th style={thStyle}>Sats</th>
                <th style={thStyle}>RF-post</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Grunnlag</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>MVA-bel&oslash;p</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Ant. posteringer</th>
              </tr>
            </thead>
            <tbody>
              {inngaendeLinjer.map((linje) => (
                <tr key={linje.mvaKode}>
                  <td style={tdStyle}>{linje.mvaKode} ({linje.standardTaxCode})</td>
                  <td style={tdStyle}>{formatMvaSats(linje.sats)}</td>
                  <td style={tdStyle}>{linje.rfPostnummer}</td>
                  <td style={monoRight}>{formatBelop(linje.sumGrunnlag)}</td>
                  <td style={monoRight}>{formatBelop(linje.sumMvaBelop)}</td>
                  <td style={monoRight}>{linje.antallPosteringer}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr style={{ backgroundColor: '#f8f8f8' }}>
                <td colSpan={3} style={{ ...sumStyle, textAlign: 'left' }}>
                  Sum inng&aring;ende MVA
                </td>
                <td style={sumStyle}>
                  {formatBelop(inngaendeLinjer.reduce((s, l) => s + l.sumGrunnlag, 0))}
                </td>
                <td style={sumStyle}>{formatBelop(oppgjor.sumInngaendeMva)}</td>
                <td style={sumStyle} />
              </tr>
            </tfoot>
          </table>
        </div>
      )}

      {/* Snudd avregning */}
      {snuddAvregningLinjer.length > 0 && (
        <div style={{ marginBottom: 24 }}>
          <h2 style={{ fontSize: 18, marginBottom: 8 }}>Snudd avregning (reverse charge)</h2>
          <table
            style={{
              width: '100%',
              borderCollapse: 'collapse',
              border: '1px solid #e0e0e0',
            }}
          >
            <thead>
              <tr>
                <th style={thStyle}>MVA-kode</th>
                <th style={thStyle}>Sats</th>
                <th style={thStyle}>RF-post</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Grunnlag</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>MVA-bel&oslash;p</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Ant. posteringer</th>
              </tr>
            </thead>
            <tbody>
              {snuddAvregningLinjer.map((linje) => (
                <tr key={linje.mvaKode}>
                  <td style={tdStyle}>{linje.mvaKode} ({linje.standardTaxCode})</td>
                  <td style={tdStyle}>{formatMvaSats(linje.sats)}</td>
                  <td style={tdStyle}>{linje.rfPostnummer}</td>
                  <td style={monoRight}>{formatBelop(linje.sumGrunnlag)}</td>
                  <td style={monoRight}>{formatBelop(linje.sumMvaBelop)}</td>
                  <td style={monoRight}>{linje.antallPosteringer}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <div style={{ marginTop: 8, fontSize: 13 }}>
            <span>Snudd avregning utg&aring;ende: {formatBelop(oppgjor.sumSnuddAvregningUtgaende)}</span>
            {' | '}
            <span>Snudd avregning inng&aring;ende: {formatBelop(oppgjor.sumSnuddAvregningInngaende)}</span>
          </div>
        </div>
      )}

      {/* Total */}
      <div
        style={{
          padding: 24,
          border: '2px solid #333',
          borderRadius: 8,
          backgroundColor: erTilgode ? '#e8f5e9' : '#ffebee',
        }}
      >
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <div>
            <h2 style={{ margin: 0, fontSize: 20 }}>
              {erTilgode ? 'MVA til gode' : 'MVA \u00E5 betale'}
            </h2>
            <div style={{ fontSize: 13, color: '#666', marginTop: 4 }}>
              Utg&aring;ende ({formatBelop(oppgjor.sumUtgaendeMva)}) + Snudd utg. (
              {formatBelop(oppgjor.sumSnuddAvregningUtgaende)}) - Inng&aring;ende (
              {formatBelop(oppgjor.sumInngaendeMva)}) - Snudd inng. (
              {formatBelop(oppgjor.sumSnuddAvregningInngaende)})
            </div>
          </div>
          <div
            style={{
              fontSize: 28,
              fontWeight: 700,
              fontFamily: 'monospace',
              color: erTilgode ? '#2e7d32' : '#c62828',
            }}
          >
            {formatBelop(Math.abs(oppgjor.mvaTilBetaling))}
          </div>
        </div>
      </div>
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

const monoRight: React.CSSProperties = {
  ...tdStyle,
  textAlign: 'right',
  fontFamily: 'monospace',
};

const sumStyle: React.CSSProperties = {
  padding: '8px 12px',
  fontWeight: 700,
  borderTop: '2px solid #333',
  textAlign: 'right',
  fontFamily: 'monospace',
  fontSize: 14,
};
