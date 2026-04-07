import { Link, useParams } from 'react-router-dom';
import {
  useBankkonto,
  useAvstemmingsrapport,
  useGodkjennAvstemming,
  useBankavstemming,
} from '../../hooks/api/useBank';
import { AvstemmingStatusNavn } from '../../types/bank';
import type { AvstemmingspostDto, AvstemmingStatus } from '../../types/bank';
import { formatBelop, formatDato } from '../../utils/formatering';

export default function AvstemmingsrapportPage() {
  const { kontoId } = useParams<{ kontoId: string }>();
  const bankkontoId = kontoId ?? '';

  const { data: konto } = useBankkonto(bankkontoId);
  const { data: rapport, isLoading, error } = useAvstemmingsrapport(bankkontoId);
  const { data: avstemming } = useBankavstemming(bankkontoId);
  const godkjenn = useGodkjennAvstemming();

  function handleGodkjenn() {
    godkjenn.mutate(bankkontoId);
  }

  if (error) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil ved lasting av rapport</h1>
        <p>Kunne ikke hente avstemmingsrapport.</p>
      </div>
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 1000, margin: '0 auto' }}>
      <div style={{ marginBottom: 16 }}>
        <Link to="/bank" style={{ color: '#0066cc', textDecoration: 'none', fontSize: 14 }}>
          &larr; Tilbake til bankkontoer
        </Link>
        {' | '}
        <Link
          to={`/bank/avstemming/${bankkontoId}`}
          style={{ color: '#0066cc', textDecoration: 'none', fontSize: 14 }}
        >
          Tilbake til avstemming
        </Link>
      </div>

      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: 24,
        }}
      >
        <div>
          <h1 style={{ margin: 0 }}>Avstemmingsrapport</h1>
          {konto && (
            <p style={{ margin: '4px 0 0', color: '#666', fontSize: 14 }}>
              {konto.kontonummer} &ndash; {konto.beskrivelse} ({konto.banknavn})
            </p>
          )}
        </div>
        {avstemming && avstemming.status === 'UnderArbeid' && (
          <button
            onClick={handleGodkjenn}
            disabled={godkjenn.isPending}
            style={{
              padding: '8px 24px',
              background:
                avstemming.uforklartDifferanse === 0 ? '#4caf50' : '#ccc',
              color: '#fff',
              border: 'none',
              borderRadius: 4,
              fontSize: 14,
              cursor:
                avstemming.uforklartDifferanse === 0 ? 'pointer' : 'not-allowed',
            }}
            title={
              avstemming.uforklartDifferanse !== 0
                ? 'Uforklart differanse maa vaere 0 for a godkjenne'
                : ''
            }
          >
            {godkjenn.isPending ? 'Godkjenner...' : 'Godkjenn avstemming'}
          </button>
        )}
      </div>

      {godkjenn.isError && (
        <div
          style={{
            padding: 12,
            background: '#ffebee',
            color: '#c62828',
            borderRadius: 4,
            marginBottom: 16,
            fontSize: 13,
          }}
        >
          Feil ved godkjenning. Uforklart differanse maa vaere 0, eller det maa angis en forklaring.
        </div>
      )}

      {godkjenn.isSuccess && (
        <div
          style={{
            padding: 12,
            background: '#e8f5e9',
            color: '#2e7d32',
            borderRadius: 4,
            marginBottom: 16,
            fontSize: 13,
          }}
        >
          Avstemming godkjent.
        </div>
      )}

      {isLoading ? (
        <p>Laster rapport...</p>
      ) : !rapport ? (
        <div
          style={{
            padding: 32,
            textAlign: 'center',
            border: '1px solid #e0e0e0',
            borderRadius: 8,
            color: '#666',
          }}
        >
          Ingen avstemmingsrapport tilgjengelig. Importer kontoutskrifter og kjoer avstemming forst.
        </div>
      ) : (
        <div>
          {/* Status-badge */}
          <div style={{ marginBottom: 24 }}>
            <span
              style={{
                padding: '4px 16px',
                borderRadius: 12,
                fontSize: 14,
                fontWeight: 600,
                ...rapportStatusFarge(rapport.status),
              }}
            >
              {AvstemmingStatusNavn[rapport.status]}
            </span>
            <span style={{ marginLeft: 16, fontSize: 13, color: '#666' }}>
              Dato: {formatDato(rapport.avstemmingsdato)}
            </span>
          </div>

          {/* Saldo-sammenligning */}
          <div
            style={{
              border: '1px solid #e0e0e0',
              borderRadius: 8,
              overflow: 'hidden',
              marginBottom: 24,
            }}
          >
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <tbody>
                <tr>
                  <td style={{ ...rapportTd, fontWeight: 600 }}>
                    Saldo iflg. hovedbok (konto {rapport.hovedbokkontonummer})
                  </td>
                  <td style={{ ...rapportTd, textAlign: 'right', fontFamily: 'monospace', fontWeight: 700 }}>
                    {formatBelop(rapport.saldoHovedbok)}
                  </td>
                </tr>
                <tr>
                  <td style={{ ...rapportTd, fontWeight: 600 }}>
                    Saldo iflg. kontoutskrift fra {rapport.banknavn}
                  </td>
                  <td style={{ ...rapportTd, textAlign: 'right', fontFamily: 'monospace', fontWeight: 700 }}>
                    {formatBelop(rapport.saldoBank)}
                  </td>
                </tr>
                <tr style={{ backgroundColor: rapport.differanse !== 0 ? '#fff8f8' : '#f0fff0' }}>
                  <td style={{ ...rapportTd, fontWeight: 700 }}>Differanse</td>
                  <td
                    style={{
                      ...rapportTd,
                      textAlign: 'right',
                      fontFamily: 'monospace',
                      fontWeight: 700,
                      color: rapport.differanse !== 0 ? '#c62828' : '#2e7d32',
                    }}
                  >
                    {formatBelop(rapport.differanse)}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          {/* Tidsavgrensninger */}
          {rapport.differanse !== 0 && (
            <div
              style={{
                border: '1px solid #e0e0e0',
                borderRadius: 8,
                overflow: 'hidden',
                marginBottom: 24,
              }}
            >
              <div
                style={{
                  padding: '8px 12px',
                  background: '#f8f8f8',
                  fontWeight: 700,
                  fontSize: 14,
                  borderBottom: '1px solid #e0e0e0',
                }}
              >
                Tidsavgrensninger
              </div>

              {/* Utestaaende betalinger */}
              {rapport.utestaaendeBetalinger.length > 0 && (
                <PostListe
                  tittel="Utestaaende betalinger (i regnskap, ikke i bank)"
                  poster={rapport.utestaaendeBetalinger}
                  fortegn="+"
                />
              )}

              {/* Innbetalinger i transitt */}
              {rapport.innbetalingerITransitt.length > 0 && (
                <PostListe
                  tittel="Innbetalinger i transitt (i bank, ikke i regnskap)"
                  poster={rapport.innbetalingerITransitt}
                  fortegn="-"
                />
              )}

              {/* Andre poster */}
              {rapport.andrePoster.length > 0 && (
                <PostListe
                  tittel="Andre poster"
                  poster={rapport.andrePoster}
                  fortegn=""
                />
              )}

              {/* Summer */}
              <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                <tbody>
                  <tr style={{ borderTop: '2px solid #333' }}>
                    <td style={{ ...rapportTd, fontWeight: 700 }}>Sum tidsavgrensninger</td>
                    <td
                      style={{
                        ...rapportTd,
                        textAlign: 'right',
                        fontFamily: 'monospace',
                        fontWeight: 700,
                      }}
                    >
                      {formatBelop(rapport.sumTidsavgrensninger)}
                    </td>
                  </tr>
                  <tr
                    style={{
                      backgroundColor:
                        rapport.uforklartDifferanse !== 0 ? '#ffebee' : '#e8f5e9',
                    }}
                  >
                    <td style={{ ...rapportTd, fontWeight: 700 }}>Uforklart differanse</td>
                    <td
                      style={{
                        ...rapportTd,
                        textAlign: 'right',
                        fontFamily: 'monospace',
                        fontWeight: 700,
                        color: rapport.uforklartDifferanse !== 0 ? '#c62828' : '#2e7d32',
                      }}
                    >
                      {formatBelop(rapport.uforklartDifferanse)}
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          )}

          {/* Differanse = 0 melding */}
          {rapport.differanse === 0 && (
            <div
              style={{
                padding: 24,
                textAlign: 'center',
                background: '#e8f5e9',
                borderRadius: 8,
                border: '1px solid #c8e6c9',
              }}
            >
              <div style={{ fontSize: 18, fontWeight: 700, color: '#2e7d32' }}>
                Ingen differanse
              </div>
              <div style={{ color: '#666', marginTop: 8 }}>
                Saldo i hovedbok stemmer med kontoutskriften fra banken.
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function PostListe({
  tittel,
  poster,
  fortegn,
}: {
  tittel: string;
  poster: AvstemmingspostDto[];
  fortegn: string;
}) {
  const sum = poster.reduce((s, p) => s + p.belop, 0);
  return (
    <div style={{ borderBottom: '1px solid #e0e0e0' }}>
      <div
        style={{
          padding: '6px 12px',
          fontSize: 13,
          fontWeight: 600,
          color: '#555',
          background: '#fafafa',
        }}
      >
        {fortegn} {tittel}
      </div>
      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <tbody>
          {poster.map((p, idx) => (
            <tr key={idx}>
              <td style={{ ...rapportTd, paddingLeft: 24, fontSize: 13 }}>
                {formatDato(p.dato)}
              </td>
              <td style={{ ...rapportTd, fontSize: 13 }}>{p.beskrivelse}</td>
              <td style={{ ...rapportTd, fontSize: 12, color: '#999', fontFamily: 'monospace' }}>
                {p.referanse ?? ''}
              </td>
              <td
                style={{
                  ...rapportTd,
                  textAlign: 'right',
                  fontFamily: 'monospace',
                  fontSize: 13,
                }}
              >
                {formatBelop(p.belop)}
              </td>
            </tr>
          ))}
          <tr style={{ borderTop: '1px solid #ddd' }}>
            <td colSpan={3} style={{ ...rapportTd, fontWeight: 600, fontSize: 13, paddingLeft: 24 }}>
              Sum
            </td>
            <td
              style={{
                ...rapportTd,
                textAlign: 'right',
                fontFamily: 'monospace',
                fontWeight: 600,
                fontSize: 13,
              }}
            >
              {formatBelop(sum)}
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  );
}

function rapportStatusFarge(status: AvstemmingStatus): {
  backgroundColor: string;
  color: string;
} {
  switch (status) {
    case 'Avstemt':
      return { backgroundColor: '#e8f5e9', color: '#2e7d32' };
    case 'AvstemtMedDifferanse':
      return { backgroundColor: '#fff3e0', color: '#e65100' };
    default:
      return { backgroundColor: '#e3f2fd', color: '#1565c0' };
  }
}

const rapportTd: React.CSSProperties = {
  padding: '8px 12px',
  fontSize: 14,
};
