import { useParams, Link } from 'react-router-dom';
import {
  useMvaAvstemming,
  useKjorAvstemming,
  useGodkjennAvstemming,
} from '../../hooks/api/useMva';
import { formatBelop } from '../../utils/formatering';

export default function MvaAvstemmingPage() {
  const { terminId } = useParams<{ terminId: string }>();
  const { data: avstemming, isLoading, error } = useMvaAvstemming(terminId ?? '');
  const kjorAvstemming = useKjorAvstemming();
  const godkjennAvstemming = useGodkjennAvstemming();

  if (!terminId) {
    return <div style={{ padding: 24 }}>Ugyldig termin-ID.</div>;
  }

  if (error) {
    return (
      <div style={{ padding: 24 }}>
        <Link to="/mva" style={{ color: '#0066cc' }}>
          &larr; Tilbake til MVA-oversikt
        </Link>
        <h1>MVA-avstemming</h1>
        <div
          style={{
            padding: 32,
            textAlign: 'center',
            border: '1px solid #e0e0e0',
            borderRadius: 8,
          }}
        >
          <p style={{ color: '#666', marginBottom: 16 }}>
            Ingen avstemming er kj&oslash;rt for denne terminen.
          </p>
          <button
            onClick={() => kjorAvstemming.mutate(terminId)}
            disabled={kjorAvstemming.isPending}
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
            {kjorAvstemming.isPending ? 'Kj\u00F8rer...' : 'Kj\u00F8r avstemming'}
          </button>
          {kjorAvstemming.isError && (
            <p style={{ color: 'red', marginTop: 8 }}>
              Feil ved avstemming. Pr&oslash;v igjen.
            </p>
          )}
        </div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div style={{ padding: 24 }}>
        <p>Laster avstemming...</p>
      </div>
    );
  }

  if (!avstemming) {
    return null;
  }

  function handleGodkjenn() {
    if (!terminId || !avstemming) return;
    godkjennAvstemming.mutate({
      terminId,
      avstemmingId: avstemming.id,
    });
  }

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
        <h1 style={{ margin: 0 }}>MVA-avstemming: {avstemming.terminnavn}</h1>
        <div style={{ display: 'flex', gap: 8 }}>
          <button
            onClick={() => kjorAvstemming.mutate(terminId)}
            disabled={kjorAvstemming.isPending}
            style={{
              padding: '8px 16px',
              background: '#f0f0f0',
              border: '1px solid #ccc',
              borderRadius: 4,
              fontSize: 14,
              cursor: 'pointer',
            }}
          >
            {kjorAvstemming.isPending ? 'Kj\u00F8rer...' : 'Kj\u00F8r p\u00E5 nytt'}
          </button>
          {!avstemming.erGodkjent && (
            <button
              onClick={handleGodkjenn}
              disabled={godkjennAvstemming.isPending}
              style={{
                padding: '8px 16px',
                background: '#2e7d32',
                color: '#fff',
                border: 'none',
                borderRadius: 4,
                fontSize: 14,
                cursor: 'pointer',
              }}
            >
              {godkjennAvstemming.isPending ? 'Godkjenner...' : 'Godkjenn avstemming'}
            </button>
          )}
        </div>
      </div>

      {/* Status-boks */}
      <div
        style={{
          padding: 16,
          marginBottom: 24,
          borderRadius: 8,
          border: '1px solid',
          borderColor: avstemming.erGodkjent
            ? '#2e7d32'
            : avstemming.harAvvik
              ? '#c62828'
              : '#2e7d32',
          backgroundColor: avstemming.erGodkjent
            ? '#e8f5e9'
            : avstemming.harAvvik
              ? '#ffebee'
              : '#e8f5e9',
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <div>
            <strong>
              {avstemming.erGodkjent
                ? 'Avstemming godkjent'
                : avstemming.harAvvik
                  ? 'Avvik funnet'
                  : 'Ingen avvik'}
            </strong>
            <div style={{ fontSize: 13, color: '#666', marginTop: 4 }}>
              Kj&oslash;rt: {new Date(avstemming.avstemmingTidspunkt).toLocaleString('nb-NO')}{' '}
              av {avstemming.avstemmingAv}
            </div>
            {avstemming.merknad && (
              <div style={{ fontSize: 13, marginTop: 4 }}>
                Merknad: {avstemming.merknad}
              </div>
            )}
          </div>
          {avstemming.harAvvik && (
            <div
              style={{
                fontSize: 20,
                fontWeight: 700,
                fontFamily: 'monospace',
                color: '#c62828',
              }}
            >
              Avvik: {formatBelop(avstemming.totaltAvvik)}
            </div>
          )}
        </div>
      </div>

      {/* Avstemmingslinjer */}
      <table
        style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}
      >
        <thead>
          <tr>
            <th style={thStyle}>Konto</th>
            <th style={thStyle}>Kontonavn</th>
            <th style={{ ...thStyle, textAlign: 'right' }}>Saldo iflg. hovedbok</th>
            <th style={{ ...thStyle, textAlign: 'right' }}>Beregnet fra posteringer</th>
            <th style={{ ...thStyle, textAlign: 'right' }}>Avvik</th>
            <th style={thStyle}>Status</th>
          </tr>
        </thead>
        <tbody>
          {avstemming.linjer.map((linje) => (
            <tr
              key={linje.kontonummer}
              style={{
                backgroundColor: linje.harAvvik ? '#fff8f8' : '#fff',
              }}
            >
              <td style={tdStyle}>
                <strong>{linje.kontonummer}</strong>
              </td>
              <td style={tdStyle}>{linje.kontonavn}</td>
              <td style={monoRight}>{formatBelop(linje.saldoIflgHovedbok)}</td>
              <td style={monoRight}>{formatBelop(linje.beregnetFraPosteringer)}</td>
              <td
                style={{
                  ...monoRight,
                  color: linje.harAvvik ? '#c62828' : 'inherit',
                  fontWeight: linje.harAvvik ? 700 : 400,
                }}
              >
                {formatBelop(linje.avvik)}
              </td>
              <td style={tdStyle}>
                <span
                  style={{
                    padding: '2px 8px',
                    borderRadius: 12,
                    fontSize: 12,
                    fontWeight: 600,
                    backgroundColor: linje.harAvvik ? '#ffebee' : '#e8f5e9',
                    color: linje.harAvvik ? '#c62828' : '#2e7d32',
                  }}
                >
                  {linje.harAvvik ? 'Avvik' : 'OK'}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
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
