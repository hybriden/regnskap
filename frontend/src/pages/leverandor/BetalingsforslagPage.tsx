import { useState } from 'react';
import { formatBelop, formatDato } from '../../utils/formatering';
import {
  useBetalingsforslag,
  useBetalingsforslagDetaljer,
  useGenererBetalingsforslag,
  useGodkjennBetalingsforslag,
  useEkskluderLinje,
  useInkluderLinje,
  useGenererBetalingsfil,
  useMarkerSendt,
  useKansellerBetalingsforslag,
} from '../../hooks/api/useLeverandor';
import { BetalingsforslagStatus, BetalingsforslagStatusNavn } from '../../types/leverandor';
import type { GenererBetalingsforslagRequest, BetalingsforslagDto } from '../../types/leverandor';

type Visning = 'liste' | 'detaljer' | 'generer';

export default function BetalingsforslagPage() {
  const [visning, setVisning] = useState<Visning>('liste');
  const [valgtForslagId, setValgtForslagId] = useState('');

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h1 style={{ margin: 0 }}>Betalingsforslag</h1>
        {visning === 'liste' && (
          <button onClick={() => setVisning('generer')} style={primaerKnappStil}>
            Generer nytt forslag
          </button>
        )}
        {visning !== 'liste' && (
          <button
            onClick={() => {
              setVisning('liste');
              setValgtForslagId('');
            }}
            style={sekundaerKnappStil}
          >
            Tilbake til liste
          </button>
        )}
      </div>

      {visning === 'liste' && (
        <ForslagListe
          onVelg={(id) => {
            setValgtForslagId(id);
            setVisning('detaljer');
          }}
        />
      )}
      {visning === 'generer' && (
        <GenererForslag
          onOpprettet={(id) => {
            setValgtForslagId(id);
            setVisning('detaljer');
          }}
        />
      )}
      {visning === 'detaljer' && valgtForslagId && (
        <ForslagDetaljer forslagId={valgtForslagId} />
      )}
    </div>
  );
}

// --- Forslagliste ---

function ForslagListe({ onVelg }: { onVelg: (id: string) => void }) {
  const { data: resultat, isLoading, error } = useBetalingsforslag({ side: 1, antall: 50 });

  if (isLoading) return <p>Laster betalingsforslag...</p>;
  if (error)
    return (
      <div style={feilStil}>Feil ved henting: {(error as Error).message}</div>
    );

  return (
    <table style={tabellStil}>
      <thead>
        <tr>
          <th style={headerCelleStil}>Nr</th>
          <th style={{ ...headerCelleStil, textAlign: 'left' }}>Beskrivelse</th>
          <th style={headerCelleStil}>Opprettdato</th>
          <th style={headerCelleStil}>Betalingsdato</th>
          <th style={headerCelleStil}>Forfall t.o.m.</th>
          <th style={headerCelleStil}>Antall</th>
          <th style={headerCelleStil}>Totalbelop</th>
          <th style={headerCelleStil}>Status</th>
        </tr>
      </thead>
      <tbody>
        {resultat?.data.map((f) => (
          <tr
            key={f.id}
            onClick={() => onVelg(f.id)}
            style={{ cursor: 'pointer' }}
            onMouseOver={(e) => { e.currentTarget.style.backgroundColor = '#e8f0fe'; }}
            onMouseOut={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
          >
            <td style={{ ...celleStil, fontFamily: 'monospace', fontWeight: 600 }}>
              {f.forslagsnummer}
            </td>
            <td style={{ ...celleStil, textAlign: 'left' }}>{f.beskrivelse}</td>
            <td style={celleStil}>{formatDato(f.opprettdato)}</td>
            <td style={celleStil}>{formatDato(f.betalingsdato)}</td>
            <td style={celleStil}>{formatDato(f.forfallTilOgMed)}</td>
            <td style={celleStil}>{f.antallBetalinger}</td>
            <td style={{ ...celleStil, fontFamily: 'monospace' }}>{formatBelop(f.totalBelop)}</td>
            <td style={celleStil}>
              <StatusBadge status={f.status} />
            </td>
          </tr>
        ))}
        {resultat?.data.length === 0 && (
          <tr>
            <td colSpan={8} style={{ ...celleStil, textAlign: 'center', color: '#666' }}>
              Ingen betalingsforslag opprettet
            </td>
          </tr>
        )}
      </tbody>
    </table>
  );
}

// --- Generer forslag ---

function GenererForslag({ onOpprettet }: { onOpprettet: (id: string) => void }) {
  const genererMutation = useGenererBetalingsforslag();
  const iDag = new Date().toISOString().slice(0, 10);

  const [forfallTilOgMed, setForfallTilOgMed] = useState(iDag);
  const [betalingsdato, setBetalingsdato] = useState(iDag);
  const [fraKontonummer, setFraKontonummer] = useState('');
  const [inkluderGodkjente, setInkluderGodkjente] = useState(true);

  async function handleGenerer() {
    const request: GenererBetalingsforslagRequest = {
      forfallTilOgMed,
      betalingsdato,
      fraKontonummer: fraKontonummer || null,
      inkluderAlleredeGodkjente: inkluderGodkjente,
    };
    try {
      const forslag = await genererMutation.mutateAsync(request);
      onOpprettet(forslag.id);
    } catch {
      // Vises i feilstil
    }
  }

  return (
    <div style={kortStil}>
      <h2 style={{ marginTop: 0 }}>Generer nytt betalingsforslag</h2>

      {genererMutation.error && (
        <div style={feilStil}>Feil: {(genererMutation.error as Error).message}</div>
      )}

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, maxWidth: 600 }}>
        <div>
          <label style={labelStil}>Forfall t.o.m.</label>
          <input
            type="date"
            value={forfallTilOgMed}
            onChange={(e) => setForfallTilOgMed(e.target.value)}
            style={inputStil}
          />
        </div>
        <div>
          <label style={labelStil}>Betalingsdato</label>
          <input
            type="date"
            value={betalingsdato}
            onChange={(e) => setBetalingsdato(e.target.value)}
            style={inputStil}
          />
        </div>
        <div>
          <label style={labelStil}>Fra bankkonto (valgfritt)</label>
          <input
            type="text"
            value={fraKontonummer}
            onChange={(e) => setFraKontonummer(e.target.value)}
            placeholder="Kontonummer"
            style={inputStil}
          />
        </div>
        <div style={{ display: 'flex', alignItems: 'flex-end', paddingBottom: 4 }}>
          <label style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
            <input
              type="checkbox"
              checked={inkluderGodkjente}
              onChange={(e) => setInkluderGodkjente(e.target.checked)}
            />
            Kun godkjente fakturaer
          </label>
        </div>
      </div>

      <div style={{ marginTop: 24 }}>
        <button
          onClick={handleGenerer}
          disabled={genererMutation.isPending || !forfallTilOgMed || !betalingsdato}
          style={primaerKnappStil}
        >
          {genererMutation.isPending ? 'Genererer...' : 'Generer forslag'}
        </button>
      </div>
    </div>
  );
}

// --- Forslagdetaljer ---

function ForslagDetaljer({ forslagId }: { forslagId: string }) {
  const { data: forslag, isLoading, error } = useBetalingsforslagDetaljer(forslagId);
  const godkjennMutation = useGodkjennBetalingsforslag();
  const ekskluderMutation = useEkskluderLinje();
  const inkluderMutation = useInkluderLinje();
  const genererFilMutation = useGenererBetalingsfil();
  const markerSendtMutation = useMarkerSendt();
  const kansellerMutation = useKansellerBetalingsforslag();

  if (isLoading) return <p>Laster forslag...</p>;
  if (error)
    return <div style={feilStil}>Feil: {(error as Error).message}</div>;
  if (!forslag) return <p>Forslag ikke funnet</p>;

  const erUtkast = forslag.status === BetalingsforslagStatus.Utkast;
  const erGodkjent = forslag.status === BetalingsforslagStatus.Godkjent;
  const erFilGenerert = forslag.status === BetalingsforslagStatus.FilGenerert;
  const inkluderteBelop = forslag.linjer
    .filter((l) => l.erInkludert)
    .reduce((s, l) => s + l.belop, 0);

  async function handleToggleLinje(linjeId: string, erInkludert: boolean) {
    try {
      if (erInkludert) {
        await ekskluderMutation.mutateAsync({ forslagId, linjeId });
      } else {
        await inkluderMutation.mutateAsync({ forslagId, linjeId });
      }
    } catch {
      // Feil handteres av react-query
    }
  }

  async function handleGenererFil() {
    try {
      const blob = await genererFilMutation.mutateAsync(forslagId);
      // Last ned filen
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `pain001_${forslag.forslagsnummer}.xml`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      // Feil handteres
    }
  }

  return (
    <div>
      {/* Forslagshode */}
      <div style={{ ...kortStil, marginBottom: 24 }}>
        <div style={{ display: 'flex', justifyContent: 'space-between' }}>
          <div>
            <h2 style={{ margin: '0 0 8px' }}>
              Forslag #{forslag.forslagsnummer}
              <StatusBadge status={forslag.status} />
            </h2>
            <p style={{ margin: 0, color: '#666' }}>{forslag.beskrivelse}</p>
          </div>
          <div style={{ textAlign: 'right' }}>
            <div style={{ fontSize: 13, color: '#666' }}>Totalbelop (inkluderte)</div>
            <div style={{ fontSize: 24, fontWeight: 700, fontFamily: 'monospace' }}>
              {formatBelop(inkluderteBelop)}
            </div>
          </div>
        </div>

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 16, marginTop: 16 }}>
          <InfoRad label="Opprettdato" verdi={formatDato(forslag.opprettdato)} />
          <InfoRad label="Betalingsdato" verdi={formatDato(forslag.betalingsdato)} />
          <InfoRad label="Forfall t.o.m." verdi={formatDato(forslag.forfallTilOgMed)} />
          <InfoRad label="Antall betalinger" verdi={`${forslag.linjer.filter((l) => l.erInkludert).length} av ${forslag.linjer.length}`} />
        </div>
        {forslag.godkjentAv && (
          <div style={{ marginTop: 8, fontSize: 13, color: '#666' }}>
            Godkjent av {forslag.godkjentAv} {forslag.godkjentTidspunkt ? formatDato(forslag.godkjentTidspunkt) : ''}
          </div>
        )}
      </div>

      {/* Handlingsknapper */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
        {erUtkast && (
          <>
            <button
              onClick={() => godkjennMutation.mutate(forslagId)}
              disabled={godkjennMutation.isPending}
              style={primaerKnappStil}
            >
              {godkjennMutation.isPending ? 'Godkjenner...' : 'Godkjenn forslag'}
            </button>
            <button
              onClick={() => kansellerMutation.mutate(forslagId)}
              disabled={kansellerMutation.isPending}
              style={fareStiKnapp}
            >
              Kanseller
            </button>
          </>
        )}
        {erGodkjent && (
          <button
            onClick={handleGenererFil}
            disabled={genererFilMutation.isPending}
            style={primaerKnappStil}
          >
            {genererFilMutation.isPending ? 'Genererer...' : 'Generer betalingsfil (pain.001)'}
          </button>
        )}
        {erFilGenerert && (
          <button
            onClick={() => markerSendtMutation.mutate(forslagId)}
            disabled={markerSendtMutation.isPending}
            style={primaerKnappStil}
          >
            {markerSendtMutation.isPending ? 'Markerer...' : 'Marker sendt til bank'}
          </button>
        )}
      </div>

      {/* Linjer */}
      <table style={tabellStil}>
        <thead>
          <tr>
            {erUtkast && <th style={{ ...headerCelleStil, width: 50 }}>Inkl.</th>}
            <th style={{ ...headerCelleStil, textAlign: 'left' }}>Leverandor</th>
            <th style={{ ...headerCelleStil, textAlign: 'left' }}>Fakturanr</th>
            <th style={headerCelleStil}>Forfall</th>
            <th style={headerCelleStil}>Belop</th>
            <th style={{ ...headerCelleStil, textAlign: 'left' }}>Konto</th>
            <th style={{ ...headerCelleStil, textAlign: 'left' }}>KID</th>
            <th style={headerCelleStil}>Status</th>
          </tr>
        </thead>
        <tbody>
          {forslag.linjer.map((linje) => (
            <tr
              key={linje.id}
              style={{
                opacity: linje.erInkludert ? 1 : 0.5,
                textDecoration: linje.erInkludert ? 'none' : 'line-through',
              }}
            >
              {erUtkast && (
                <td style={celleStil}>
                  <input
                    type="checkbox"
                    checked={linje.erInkludert}
                    onChange={() => handleToggleLinje(linje.id, linje.erInkludert)}
                  />
                </td>
              )}
              <td style={{ ...celleStil, textAlign: 'left' }}>
                <span style={{ fontFamily: 'monospace', fontWeight: 600 }}>
                  {linje.leverandornummer}
                </span>{' '}
                {linje.leverandornavn}
              </td>
              <td style={{ ...celleStil, textAlign: 'left' }}>{linje.eksternFakturanummer}</td>
              <td style={celleStil}>{formatDato(linje.forfallsdato)}</td>
              <td style={{ ...celleStil, fontFamily: 'monospace' }}>{formatBelop(linje.belop)}</td>
              <td style={{ ...celleStil, textAlign: 'left', fontFamily: 'monospace' }}>
                {linje.mottakerKontonummer ?? linje.mottakerIban ?? '-'}
              </td>
              <td style={{ ...celleStil, textAlign: 'left' }}>{linje.kidNummer ?? '-'}</td>
              <td style={celleStil}>
                {linje.erUtfort === true && (
                  <span style={{ color: '#2e7d32', fontWeight: 600 }}>Utfort</span>
                )}
                {linje.erUtfort === false && (
                  <span style={{ color: '#c62828', fontWeight: 600 }} title={linje.feilmelding ?? ''}>
                    Avvist
                  </span>
                )}
                {linje.erUtfort === null && <span style={{ color: '#666' }}>-</span>}
              </td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr style={{ backgroundColor: '#f8f8f8' }}>
            {erUtkast && <td style={sumCelleStil} />}
            <td colSpan={3} style={{ ...sumCelleStil, textAlign: 'left' }}>
              Sum inkluderte ({forslag.linjer.filter((l) => l.erInkludert).length} linjer)
            </td>
            <td style={{ ...sumCelleStil, fontFamily: 'monospace' }}>
              {formatBelop(inkluderteBelop)}
            </td>
            <td colSpan={3} style={sumCelleStil} />
          </tr>
        </tfoot>
      </table>
    </div>
  );
}

// --- Hjelpekomponenter ---

function StatusBadge({ status }: { status: BetalingsforslagStatus }) {
  const farger: Record<string, { bg: string; color: string }> = {
    Utkast: { bg: '#fff3e0', color: '#e65100' },
    Godkjent: { bg: '#e8f5e9', color: '#2e7d32' },
    FilGenerert: { bg: '#e3f2fd', color: '#1565c0' },
    SendtTilBank: { bg: '#e3f2fd', color: '#0d47a1' },
    Utfort: { bg: '#e8f5e9', color: '#1b5e20' },
    Avvist: { bg: '#ffebee', color: '#c62828' },
    Kansellert: { bg: '#f5f5f5', color: '#666' },
  };
  const farge = farger[status] ?? { bg: '#f5f5f5', color: '#333' };

  return (
    <span
      style={{
        padding: '2px 8px',
        borderRadius: 12,
        fontSize: 12,
        fontWeight: 600,
        backgroundColor: farge.bg,
        color: farge.color,
        marginLeft: 8,
        whiteSpace: 'nowrap',
      }}
    >
      {BetalingsforslagStatusNavn[status]}
    </span>
  );
}

function InfoRad({ label, verdi }: { label: string; verdi: string }) {
  return (
    <div>
      <div style={{ fontSize: 12, color: '#888' }}>{label}</div>
      <div style={{ fontSize: 14, fontWeight: 600 }}>{verdi}</div>
    </div>
  );
}

// --- Stiler ---

const kortStil: React.CSSProperties = {
  padding: 16,
  border: '1px solid #e0e0e0',
  borderRadius: 8,
  backgroundColor: '#fafafa',
};

const labelStil: React.CSSProperties = {
  display: 'block',
  marginBottom: 4,
  fontWeight: 600,
  fontSize: 13,
};

const inputStil: React.CSSProperties = {
  width: '100%',
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  boxSizing: 'border-box',
};

const feilStil: React.CSSProperties = {
  padding: 12,
  background: '#ffebee',
  color: '#c62828',
  borderRadius: 4,
  marginBottom: 16,
};

const tabellStil: React.CSSProperties = {
  width: '100%',
  borderCollapse: 'collapse',
  border: '1px solid #e0e0e0',
};

const headerCelleStil: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '2px solid #333',
  fontWeight: 700,
  backgroundColor: '#f8f8f8',
  textAlign: 'right',
  fontSize: 13,
};

const celleStil: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '1px solid #e0e0e0',
  textAlign: 'right',
  fontSize: 14,
};

const sumCelleStil: React.CSSProperties = {
  padding: '8px 12px',
  fontWeight: 700,
  borderTop: '2px solid #333',
  textAlign: 'right',
};

const primaerKnappStil: React.CSSProperties = {
  padding: '8px 20px',
  backgroundColor: '#1565c0',
  color: '#fff',
  border: 'none',
  borderRadius: 4,
  cursor: 'pointer',
  fontSize: 14,
  fontWeight: 600,
};

const sekundaerKnappStil: React.CSSProperties = {
  padding: '8px 16px',
  backgroundColor: '#f5f5f5',
  color: '#333',
  border: '1px solid #ccc',
  borderRadius: 4,
  cursor: 'pointer',
  fontSize: 14,
};

const fareStiKnapp: React.CSSProperties = {
  padding: '8px 16px',
  backgroundColor: '#ffebee',
  color: '#c62828',
  border: '1px solid #ef9a9a',
  borderRadius: 4,
  cursor: 'pointer',
  fontSize: 14,
  fontWeight: 600,
};
