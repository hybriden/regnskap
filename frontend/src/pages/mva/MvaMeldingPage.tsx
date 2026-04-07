import { useParams, Link } from 'react-router-dom';
import { useMvaMelding, useMarkerInnsendt } from '../../hooks/api/useMva';
import { formatBelop, formatDato } from '../../utils/formatering';

export default function MvaMeldingPage() {
  const { terminId } = useParams<{ terminId: string }>();
  const { data: melding, isLoading, error } = useMvaMelding(terminId ?? '');
  const markerInnsendt = useMarkerInnsendt();

  if (!terminId) {
    return <div style={{ padding: 24 }}>Ugyldig termin-ID.</div>;
  }

  if (error) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <Link to="/mva" style={{ color: '#0066cc' }}>
          &larr; Tilbake til MVA-oversikt
        </Link>
        <h1>MVA-melding</h1>
        <p>Kunne ikke hente MVA-meldingsdata. Oppgj&oslash;r m&aring; beregnes f&oslash;rst.</p>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div style={{ padding: 24 }}>
        <p>Laster MVA-melding...</p>
      </div>
    );
  }

  if (!melding) {
    return null;
  }

  const erTilgode = melding.mvaTilBetaling < 0;

  return (
    <div style={{ padding: 24, maxWidth: 1000, margin: '0 auto' }}>
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
        <h1 style={{ margin: 0 }}>MVA-melding (RF-0002)</h1>
        <button
          onClick={() => markerInnsendt.mutate(terminId)}
          disabled={markerInnsendt.isPending}
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
          {markerInnsendt.isPending ? 'Markerer...' : 'Marker som innsendt'}
        </button>
      </div>

      {markerInnsendt.isError && (
        <p style={{ color: 'red', marginBottom: 16 }}>
          Feil ved markering. Sjekk at avstemming er godkjent.
        </p>
      )}

      {/* Meldingsheader */}
      <div
        style={{
          padding: 16,
          backgroundColor: '#f8f8f8',
          border: '1px solid #e0e0e0',
          borderRadius: 8,
          marginBottom: 24,
        }}
      >
        <h2 style={{ margin: '0 0 8px 0', fontSize: 16 }}>
          {melding.terminnavn}
        </h2>
        <div style={{ fontSize: 14, color: '#666' }}>
          Periode: {formatDato(melding.fraDato)} &ndash; {formatDato(melding.tilDato)}
        </div>
      </div>

      {/* RF-0002 poster */}
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
            <th style={{ ...thStyle, width: 60 }}>Post</th>
            <th style={thStyle}>Beskrivelse</th>
            <th style={{ ...thStyle, textAlign: 'right' }}>Grunnlag</th>
            <th style={{ ...thStyle, textAlign: 'right' }}>MVA-bel&oslash;p</th>
          </tr>
        </thead>
        <tbody>
          {melding.poster.map((post) => (
            <tr
              key={post.postnummer}
              style={{
                backgroundColor:
                  post.postnummer <= 6 ? '#fff' : post.postnummer <= 10 ? '#f9fdf9' : '#fafafa',
              }}
            >
              <td style={{ ...tdStyle, fontWeight: 700 }}>{post.postnummer}</td>
              <td style={tdStyle}>
                {post.beskrivelse}
                {post.standardTaxCodes.length > 0 && (
                  <span style={{ fontSize: 12, color: '#999', marginLeft: 8 }}>
                    ({post.standardTaxCodes.join(', ')})
                  </span>
                )}
              </td>
              <td style={monoRight}>
                {post.grunnlag !== 0 ? formatBelop(post.grunnlag) : ''}
              </td>
              <td style={monoRight}>
                {post.mvaBelop !== 0 ? formatBelop(post.mvaBelop) : ''}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {/* Oppsummering */}
      <div
        style={{
          border: '2px solid #333',
          borderRadius: 8,
          overflow: 'hidden',
        }}
      >
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <tbody>
            <tr style={{ backgroundColor: '#f8f8f8' }}>
              <td style={summaryLabel}>Grunnlag h&oslash;y sats (25 %)</td>
              <td style={summaryValue}>{formatBelop(melding.mvaGrunnlagHoySats)}</td>
            </tr>
            <tr>
              <td style={summaryLabel}>Grunnlag middels sats (15 %)</td>
              <td style={summaryValue}>{formatBelop(melding.mvaGrunnlagMiddelsSats)}</td>
            </tr>
            <tr style={{ backgroundColor: '#f8f8f8' }}>
              <td style={summaryLabel}>Grunnlag lav sats (12 %)</td>
              <td style={summaryValue}>{formatBelop(melding.mvaGrunnlagLavSats)}</td>
            </tr>
            <tr>
              <td style={summaryLabel}>Sum utg&aring;ende MVA</td>
              <td style={summaryValue}>{formatBelop(melding.sumUtgaendeMva)}</td>
            </tr>
            <tr style={{ backgroundColor: '#f8f8f8' }}>
              <td style={summaryLabel}>Sum inng&aring;ende MVA (fradrag)</td>
              <td style={summaryValue}>{formatBelop(melding.sumInngaendeMva)}</td>
            </tr>
            <tr
              style={{
                backgroundColor: erTilgode ? '#e8f5e9' : '#ffebee',
              }}
            >
              <td
                style={{
                  ...summaryLabel,
                  fontWeight: 700,
                  fontSize: 16,
                  borderTop: '2px solid #333',
                }}
              >
                {erTilgode ? 'MVA til gode' : 'MVA \u00E5 betale'}
              </td>
              <td
                style={{
                  ...summaryValue,
                  fontWeight: 700,
                  fontSize: 20,
                  borderTop: '2px solid #333',
                  color: erTilgode ? '#2e7d32' : '#c62828',
                }}
              >
                {formatBelop(Math.abs(melding.mvaTilBetaling))}
              </td>
            </tr>
          </tbody>
        </table>
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

const summaryLabel: React.CSSProperties = {
  padding: '10px 16px',
  borderBottom: '1px solid #e0e0e0',
  fontSize: 14,
};

const summaryValue: React.CSSProperties = {
  padding: '10px 16px',
  borderBottom: '1px solid #e0e0e0',
  fontSize: 14,
  textAlign: 'right',
  fontFamily: 'monospace',
};
