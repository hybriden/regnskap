import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  useBilagSerier,
  useOpprettBilagSerie,
  useOppdaterBilagSerie,
} from '../../hooks/api/useBilag';
import { BilagType } from '../../types/hovedbok';
import type { BilagSerieDto, OpprettBilagSerieRequest } from '../../types/bilag';

const BilagTypeNavn: Record<string, string> = {
  [BilagType.Manuelt]: 'Manuelt',
  [BilagType.InngaendeFaktura]: 'Inngaende faktura',
  [BilagType.UtgaendeFaktura]: 'Utgaende faktura',
  [BilagType.Bank]: 'Bank',
  [BilagType.Lonn]: 'Lonn',
  [BilagType.Avskrivning]: 'Avskrivning',
  [BilagType.MvaOppgjor]: 'MVA-oppgjor',
  [BilagType.Arsavslutning]: 'Arsavslutning',
  [BilagType.Apningsbalanse]: 'Apningsbalanse',
  [BilagType.Kreditnota]: 'Kreditnota',
  [BilagType.Korreksjon]: 'Korreksjon',
};

export default function BilagSeriePage() {
  const { data: serier = [], isLoading, error } = useBilagSerier();
  const opprettMutation = useOpprettBilagSerie();
  const [visNyForm, setVisNyForm] = useState(false);
  const [nyKode, setNyKode] = useState('');
  const [nyNavn, setNyNavn] = useState('');
  const [nyType, setNyType] = useState(BilagType.Manuelt);
  const [nySaftId, setNySaftId] = useState('');
  const [feilmelding, setFeilmelding] = useState('');

  function handleOpprett() {
    setFeilmelding('');
    if (!nyKode.trim() || !nyNavn.trim() || !nySaftId.trim()) {
      setFeilmelding('Alle felter er pakrevd.');
      return;
    }
    if (!/^[A-Z0-9]{1,10}$/.test(nyKode)) {
      setFeilmelding('Kode ma vaere 1-10 tegn, kun store bokstaver og tall.');
      return;
    }

    const request: OpprettBilagSerieRequest = {
      kode: nyKode,
      navn: nyNavn,
      standardType: nyType,
      saftJournalId: nySaftId,
    };

    opprettMutation.mutate(request, {
      onSuccess: () => {
        setVisNyForm(false);
        setNyKode('');
        setNyNavn('');
        setNyType(BilagType.Manuelt);
        setNySaftId('');
      },
      onError: () => {
        setFeilmelding('Kunne ikke opprette bilagserie.');
      },
    });
  }

  if (error) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil</h1>
        <p>Kunne ikke hente bilagserier.</p>
      </div>
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 1000, margin: '0 auto' }}>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 16,
          marginBottom: 8,
          fontSize: 14,
        }}
      >
        <Link to="/bilag" style={{ color: '#0066cc', textDecoration: 'none' }}>
          Bilag
        </Link>
        <span style={{ color: '#999' }}>/</span>
        <span style={{ color: '#666' }}>Bilagserier</span>
      </div>

      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: 24,
        }}
      >
        <h1 style={{ margin: 0 }}>Bilagserier</h1>
        <button
          type="button"
          onClick={() => setVisNyForm(!visNyForm)}
          style={{
            padding: '8px 16px',
            background: '#0066cc',
            color: '#fff',
            border: 'none',
            borderRadius: 4,
            fontSize: 14,
            fontWeight: 600,
            cursor: 'pointer',
          }}
        >
          {visNyForm ? 'Avbryt' : '+ Ny serie'}
        </button>
      </div>

      {/* Opprett-form */}
      {visNyForm && (
        <div
          style={{
            marginBottom: 24,
            padding: 16,
            backgroundColor: '#f8f8f8',
            border: '1px solid #e0e0e0',
            borderRadius: 4,
          }}
        >
          <h3 style={{ margin: '0 0 12px' }}>Ny bilagserie</h3>
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: '120px 1fr 200px 120px',
              gap: 12,
              alignItems: 'end',
            }}
          >
            <div>
              <label style={labelStyle}>
                Kode <span style={{ color: 'red' }}>*</span>
              </label>
              <input
                type="text"
                value={nyKode}
                onChange={(e) => setNyKode(e.target.value.toUpperCase())}
                placeholder="F.eks. DIV"
                maxLength={10}
                style={inputStyle}
              />
            </div>
            <div>
              <label style={labelStyle}>
                Navn <span style={{ color: 'red' }}>*</span>
              </label>
              <input
                type="text"
                value={nyNavn}
                onChange={(e) => setNyNavn(e.target.value)}
                placeholder="Beskrivende navn..."
                style={inputStyle}
              />
            </div>
            <div>
              <label style={labelStyle}>Standard type</label>
              <select
                value={nyType}
                onChange={(e) => setNyType(e.target.value)}
                style={inputStyle}
              >
                {Object.entries(BilagTypeNavn).map(([value, label]) => (
                  <option key={value} value={value}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label style={labelStyle}>
                SAF-T ID <span style={{ color: 'red' }}>*</span>
              </label>
              <input
                type="text"
                value={nySaftId}
                onChange={(e) => setNySaftId(e.target.value.toUpperCase())}
                placeholder="F.eks. DIV"
                maxLength={20}
                style={inputStyle}
              />
            </div>
          </div>
          {feilmelding && (
            <div style={{ marginTop: 8, color: '#c62828', fontSize: 13 }}>{feilmelding}</div>
          )}
          <button
            type="button"
            onClick={handleOpprett}
            disabled={opprettMutation.isPending}
            style={{
              marginTop: 12,
              padding: '8px 20px',
              background: '#0066cc',
              color: '#fff',
              border: 'none',
              borderRadius: 4,
              fontSize: 14,
              cursor: opprettMutation.isPending ? 'not-allowed' : 'pointer',
            }}
          >
            {opprettMutation.isPending ? 'Oppretter...' : 'Opprett serie'}
          </button>
        </div>
      )}

      {/* Liste */}
      {isLoading ? (
        <p>Laster bilagserier...</p>
      ) : serier.length === 0 ? (
        <p style={{ color: '#666', textAlign: 'center' }}>Ingen bilagserier funnet.</p>
      ) : (
        <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
          <thead>
            <tr>
              <th style={thStyle}>Kode</th>
              <th style={{ ...thStyle, textAlign: 'left' }}>Navn</th>
              <th style={thStyle}>Standard type</th>
              <th style={thStyle}>SAF-T ID</th>
              <th style={thStyle}>Status</th>
              <th style={thStyle}>System</th>
              <th style={thStyle}>Handling</th>
            </tr>
          </thead>
          <tbody>
            {serier.map((serie: BilagSerieDto) => (
              <SerieRad key={serie.id} serie={serie} />
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

// --- Serie-rad med inline redigering ---

function SerieRad({ serie }: { serie: BilagSerieDto }) {
  const oppdaterMutation = useOppdaterBilagSerie(serie.kode);

  function toggleAktiv() {
    oppdaterMutation.mutate({
      navn: serie.navn,
      erAktiv: !serie.erAktiv,
    });
  }

  return (
    <tr style={{ backgroundColor: serie.erAktiv ? '#fff' : '#f5f5f5' }}>
      <td style={{ ...tdStyle, fontFamily: 'monospace', fontWeight: 700 }}>{serie.kode}</td>
      <td style={{ ...tdStyle, textAlign: 'left' }}>{serie.navn}</td>
      <td style={tdStyle}>{BilagTypeNavn[serie.standardType] ?? serie.standardType}</td>
      <td style={{ ...tdStyle, fontFamily: 'monospace' }}>{serie.saftJournalId}</td>
      <td style={tdStyle}>
        <span
          style={{
            padding: '2px 8px',
            borderRadius: 12,
            fontSize: 12,
            fontWeight: 600,
            backgroundColor: serie.erAktiv ? '#e8f5e9' : '#f5f5f5',
            color: serie.erAktiv ? '#2e7d32' : '#616161',
          }}
        >
          {serie.erAktiv ? 'Aktiv' : 'Inaktiv'}
        </span>
      </td>
      <td style={tdStyle}>{serie.erSystemserie ? 'Ja' : 'Nei'}</td>
      <td style={tdStyle}>
        {!serie.erSystemserie && (
          <button
            type="button"
            onClick={toggleAktiv}
            disabled={oppdaterMutation.isPending}
            style={{
              padding: '4px 12px',
              border: '1px solid #ccc',
              borderRadius: 4,
              background: '#fff',
              cursor: 'pointer',
              fontSize: 12,
            }}
          >
            {serie.erAktiv ? 'Deaktiver' : 'Aktiver'}
          </button>
        )}
      </td>
    </tr>
  );
}

// --- Styles ---

const labelStyle: React.CSSProperties = {
  display: 'block',
  marginBottom: 4,
  fontWeight: 600,
  fontSize: 12,
};

const inputStyle: React.CSSProperties = {
  width: '100%',
  padding: '8px 10px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  boxSizing: 'border-box',
};

const thStyle: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '2px solid #333',
  textAlign: 'center',
  fontWeight: 700,
  backgroundColor: '#f8f8f8',
  fontSize: 13,
};

const tdStyle: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '1px solid #e0e0e0',
  fontSize: 14,
  textAlign: 'center',
};
