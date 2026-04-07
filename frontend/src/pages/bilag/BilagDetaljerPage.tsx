import { useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useBilagDetaljer, useBokforBilag, useTilbakeforBilag } from '../../hooks/api/useBilag';
import RegnskapsTabell from '../../components/RegnskapsTabell';
import { formatBelop, formatDato } from '../../utils/formatering';
import { BilagStatus, BilagStatusNavn } from '../../types/bilag';
import { BokforingSide } from '../../types/hovedbok';
import type { BilagDto, PosteringLinjeDto } from '../../types/bilag';
import type { RegnskapsLinje } from '../../components/RegnskapsTabell';

function bilagStatus(bilag: BilagDto): BilagStatus {
  if (bilag.erTilbakfort) return BilagStatus.Tilbakfort;
  if (bilag.erBokfort) return BilagStatus.Bokfort;
  return BilagStatus.Kladd;
}

function statusFarge(status: BilagStatus): { bg: string; color: string } {
  switch (status) {
    case BilagStatus.Bokfort:
      return { bg: '#e8f5e9', color: '#2e7d32' };
    case BilagStatus.Tilbakfort:
      return { bg: '#ffebee', color: '#c62828' };
    case BilagStatus.Validert:
      return { bg: '#e3f2fd', color: '#1565c0' };
    case BilagStatus.Kladd:
    default:
      return { bg: '#fff3e0', color: '#e65100' };
  }
}

function posteringTilTabellLinje(p: PosteringLinjeDto): RegnskapsLinje {
  return {
    id: p.id,
    beskrivelse: `${p.kontonummer} ${p.kontonavn}${p.beskrivelse ? ' - ' + p.beskrivelse : ''}${p.erAutoGenerertMva ? ' (auto MVA)' : ''}`,
    debet: p.side === BokforingSide.Debet ? p.belop : 0,
    kredit: p.side === BokforingSide.Kredit ? p.belop : 0,
  };
}

export default function BilagDetaljerPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: bilag, isLoading, error } = useBilagDetaljer(id ?? '');
  const bokforMutation = useBokforBilag();
  const tilbakeforMutation = useTilbakeforBilag();
  const [visTilbakeforDialog, setVisTilbakeforDialog] = useState(false);
  const [tilbakeforBeskrivelse, setTilbakeforBeskrivelse] = useState('');
  const [feilmelding, setFeilmelding] = useState('');

  if (isLoading) {
    return <div style={{ padding: 24 }}>Laster bilag...</div>;
  }

  if (error || !bilag) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Bilag ikke funnet</h1>
        <p>Bilaget finnes ikke eller kunne ikke lastes.</p>
        <Link to="/bilag" style={{ color: '#0066cc' }}>
          Tilbake til bilagsliste
        </Link>
      </div>
    );
  }

  const status = bilagStatus(bilag);
  const farge = statusFarge(status);
  const tabellLinjer = bilag.posteringer.map(posteringTilTabellLinje);

  function handleBokfor() {
    if (!bilag) return;
    setFeilmelding('');
    bokforMutation.mutate(bilag.id, {
      onError: () => {
        setFeilmelding('Kunne ikke bokfore bilaget. Prov igjen.');
      },
    });
  }

  function handleTilbakefor() {
    if (!bilag) return;
    if (!tilbakeforBeskrivelse.trim()) {
      setFeilmelding('Beskrivelse er pakrevd for tilbakeforing.');
      return;
    }
    setFeilmelding('');
    tilbakeforMutation.mutate(
      {
        originalBilagId: bilag.id,
        tilbakeforingsdato: new Date().toISOString().slice(0, 10),
        beskrivelse: tilbakeforBeskrivelse,
      },
      {
        onSuccess: (nyttBilag) => {
          navigate(`/bilag/${nyttBilag.id}`);
        },
        onError: () => {
          setFeilmelding('Kunne ikke tilbakefore bilaget. Prov igjen.');
        },
      },
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      {/* Brodsmulesti */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 8,
          marginBottom: 8,
          fontSize: 14,
        }}
      >
        <Link to="/bilag" style={{ color: '#0066cc', textDecoration: 'none' }}>
          Bilag
        </Link>
        <span style={{ color: '#999' }}>/</span>
        <span style={{ color: '#666' }}>{bilag.serieBilagsId ?? bilag.bilagsId}</span>
      </div>

      {/* Header */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'flex-start',
          marginBottom: 24,
        }}
      >
        <div>
          <h1 style={{ margin: 0 }}>
            Bilag {bilag.bilagsnummer}
            {bilag.serieBilagsId && (
              <span style={{ fontSize: 16, color: '#666', marginLeft: 12 }}>
                ({bilag.serieBilagsId})
              </span>
            )}
          </h1>
          <p style={{ margin: '4px 0 0', color: '#666' }}>{bilag.beskrivelse}</p>
        </div>
        <span
          style={{
            padding: '4px 12px',
            borderRadius: 12,
            fontSize: 14,
            fontWeight: 600,
            backgroundColor: farge.bg,
            color: farge.color,
          }}
        >
          {BilagStatusNavn[status]}
        </span>
      </div>

      {/* Metadata */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
          gap: 16,
          marginBottom: 24,
          padding: 16,
          backgroundColor: '#f8f8f8',
          borderRadius: 4,
          border: '1px solid #e0e0e0',
        }}
      >
        <MetadataFelt label="Bilagsdato" verdi={formatDato(bilag.bilagsdato)} />
        <MetadataFelt label="Registrert" verdi={formatDato(bilag.registreringsdato)} />
        <MetadataFelt label="Type" verdi={bilag.type} />
        <MetadataFelt label="Serie" verdi={bilag.serieKode ?? '-'} />
        <MetadataFelt
          label="Periode"
          verdi={`${bilag.periode.ar} / ${bilag.periode.periode}`}
        />
        {bilag.eksternReferanse && (
          <MetadataFelt label="Ekstern ref." verdi={bilag.eksternReferanse} />
        )}
        {bilag.bokfortTidspunkt && (
          <MetadataFelt label="Bokfort" verdi={formatDato(bilag.bokfortTidspunkt)} />
        )}
        {bilag.tilbakefortFraBilagId && (
          <MetadataFelt
            label="Tilbakeforing av"
            verdi={
              <Link
                to={`/bilag/${bilag.tilbakefortFraBilagId}`}
                style={{ color: '#0066cc' }}
              >
                Se original
              </Link>
            }
          />
        )}
        {bilag.tilbakefortAvBilagId && (
          <MetadataFelt
            label="Tilbakeforing"
            verdi={
              <Link
                to={`/bilag/${bilag.tilbakefortAvBilagId}`}
                style={{ color: '#0066cc' }}
              >
                Se tilbakeforing
              </Link>
            }
          />
        )}
      </div>

      {/* Posteringer */}
      <RegnskapsTabell
        linjer={tabellLinjer}
        visSum={true}
        tittel="Posteringer"
      />

      {/* MVA-detaljer */}
      {bilag.posteringer.some((p) => p.mvaKode) && (
        <div style={{ marginTop: 16 }}>
          <h3 style={{ marginBottom: 8 }}>MVA-detaljer</h3>
          <table
            style={{ borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}
          >
            <thead>
              <tr>
                <th style={mvaThStyle}>Linje</th>
                <th style={mvaThStyle}>MVA-kode</th>
                <th style={{ ...mvaThStyle, textAlign: 'right' }}>Sats</th>
                <th style={{ ...mvaThStyle, textAlign: 'right' }}>Grunnlag</th>
                <th style={{ ...mvaThStyle, textAlign: 'right' }}>MVA-belop</th>
              </tr>
            </thead>
            <tbody>
              {bilag.posteringer
                .filter((p) => p.mvaKode)
                .map((p) => (
                  <tr key={p.id}>
                    <td style={mvaTdStyle}>{p.linjenummer}</td>
                    <td style={mvaTdStyle}>{p.mvaKode}</td>
                    <td style={{ ...mvaTdStyle, textAlign: 'right' }}>
                      {p.mvaSats != null ? `${p.mvaSats} %` : '-'}
                    </td>
                    <td
                      style={{
                        ...mvaTdStyle,
                        textAlign: 'right',
                        fontFamily: 'monospace',
                      }}
                    >
                      {p.mvaGrunnlag != null ? formatBelop(p.mvaGrunnlag) : '-'}
                    </td>
                    <td
                      style={{
                        ...mvaTdStyle,
                        textAlign: 'right',
                        fontFamily: 'monospace',
                      }}
                    >
                      {p.mvaBelop != null ? formatBelop(p.mvaBelop) : '-'}
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Vedlegg */}
      {bilag.vedlegg.length > 0 && (
        <div style={{ marginTop: 16 }}>
          <h3 style={{ marginBottom: 8 }}>Vedlegg ({bilag.vedlegg.length})</h3>
          <ul style={{ margin: 0, paddingLeft: 20 }}>
            {bilag.vedlegg.map((v) => (
              <li key={v.id} style={{ marginBottom: 4, fontSize: 14 }}>
                <strong>{v.filnavn}</strong>
                <span style={{ color: '#666', marginLeft: 8 }}>
                  ({v.mimeType}, {Math.round(v.storrelse / 1024)} KB)
                </span>
                {v.beskrivelse && (
                  <span style={{ color: '#888', marginLeft: 8 }}>- {v.beskrivelse}</span>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Feilmelding */}
      {feilmelding && (
        <div
          style={{
            marginTop: 16,
            padding: 12,
            backgroundColor: '#ffebee',
            border: '1px solid #ef9a9a',
            borderRadius: 4,
            color: '#c62828',
          }}
        >
          {feilmelding}
        </div>
      )}

      {/* Handlingsknapper */}
      <div style={{ marginTop: 24, display: 'flex', gap: 12 }}>
        {/* Bokfor-knapp (kun for kladder) */}
        {!bilag.erBokfort && !bilag.erTilbakfort && (
          <button
            type="button"
            onClick={handleBokfor}
            disabled={bokforMutation.isPending}
            style={{
              padding: '10px 24px',
              background: '#2e7d32',
              color: '#fff',
              border: 'none',
              borderRadius: 4,
              fontSize: 14,
              fontWeight: 600,
              cursor: bokforMutation.isPending ? 'not-allowed' : 'pointer',
            }}
          >
            {bokforMutation.isPending ? 'Bokforer...' : 'Bokfor bilag'}
          </button>
        )}

        {/* Tilbakefor-knapp (kun for bokforte, ikke-tilbakeforte) */}
        {bilag.erBokfort && !bilag.erTilbakfort && (
          <button
            type="button"
            onClick={() => setVisTilbakeforDialog(true)}
            style={{
              padding: '10px 24px',
              background: '#c62828',
              color: '#fff',
              border: 'none',
              borderRadius: 4,
              fontSize: 14,
              fontWeight: 600,
              cursor: 'pointer',
            }}
          >
            Tilbakefor
          </button>
        )}

        <Link
          to="/bilag"
          style={{
            padding: '10px 24px',
            background: '#f0f0f0',
            color: '#333',
            border: '1px solid #ccc',
            borderRadius: 4,
            fontSize: 14,
            textDecoration: 'none',
          }}
        >
          Tilbake til liste
        </Link>
      </div>

      {/* Tilbakefor-dialog */}
      {visTilbakeforDialog && (
        <div
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(0,0,0,0.5)',
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            zIndex: 1000,
          }}
        >
          <div
            style={{
              background: '#fff',
              padding: 24,
              borderRadius: 8,
              maxWidth: 500,
              width: '90%',
              boxShadow: '0 4px 16px rgba(0,0,0,0.2)',
            }}
          >
            <h2 style={{ margin: '0 0 16px' }}>Tilbakefor bilag</h2>
            <p style={{ color: '#666', marginBottom: 16 }}>
              Dette oppretter et nytt bilag som reverserer alle posteringene i bilag{' '}
              {bilag.bilagsnummer}. Handlingen kan ikke angres.
            </p>
            <div style={{ marginBottom: 16 }}>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 600 }}>
                Beskrivelse <span style={{ color: 'red' }}>*</span>
              </label>
              <input
                type="text"
                value={tilbakeforBeskrivelse}
                onChange={(e) => setTilbakeforBeskrivelse(e.target.value)}
                placeholder="Arsak til tilbakeforing..."
                style={{
                  width: '100%',
                  padding: '8px 12px',
                  border: '1px solid #ccc',
                  borderRadius: 4,
                  fontSize: 14,
                  boxSizing: 'border-box',
                }}
              />
            </div>
            <div style={{ display: 'flex', gap: 12, justifyContent: 'flex-end' }}>
              <button
                type="button"
                onClick={() => {
                  setVisTilbakeforDialog(false);
                  setTilbakeforBeskrivelse('');
                }}
                style={{
                  padding: '8px 20px',
                  background: '#f0f0f0',
                  border: '1px solid #ccc',
                  borderRadius: 4,
                  fontSize: 14,
                  cursor: 'pointer',
                }}
              >
                Avbryt
              </button>
              <button
                type="button"
                onClick={handleTilbakefor}
                disabled={tilbakeforMutation.isPending}
                style={{
                  padding: '8px 20px',
                  background: '#c62828',
                  color: '#fff',
                  border: 'none',
                  borderRadius: 4,
                  fontSize: 14,
                  fontWeight: 600,
                  cursor: tilbakeforMutation.isPending ? 'not-allowed' : 'pointer',
                }}
              >
                {tilbakeforMutation.isPending ? 'Tilbakeforer...' : 'Bekreft tilbakeforing'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// --- Hjelpkomponent ---

function MetadataFelt({
  label,
  verdi,
}: {
  label: string;
  verdi: React.ReactNode;
}) {
  return (
    <div>
      <div style={{ fontSize: 12, color: '#666', marginBottom: 2 }}>{label}</div>
      <div style={{ fontSize: 14, fontWeight: 600 }}>{verdi}</div>
    </div>
  );
}

// --- Styles ---

const mvaThStyle: React.CSSProperties = {
  padding: '6px 12px',
  borderBottom: '2px solid #333',
  textAlign: 'left',
  fontWeight: 700,
  backgroundColor: '#f8f8f8',
  fontSize: 13,
};

const mvaTdStyle: React.CSSProperties = {
  padding: '6px 12px',
  borderBottom: '1px solid #e0e0e0',
  fontSize: 14,
};
