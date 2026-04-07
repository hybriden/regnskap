import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  useRegistrerInnbetaling,
  useMatchKid,
  useUmatchedeInnbetalinger,
  useSokKunder,
  useKundeFakturaer,
} from '../../hooks/api/useKunde';
import type {
  RegistrerInnbetalingRequest,
  MatchKidRequest,
  KundeFakturaDto,
} from '../../types/kunde';
import { KundeFakturaStatusNavn } from '../../types/kunde';
import { formatBelop, formatDato } from '../../utils/formatering';

const cellStyle: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '1px solid #e0e0e0',
  fontSize: 14,
};

const headerStyle: React.CSSProperties = {
  ...cellStyle,
  fontWeight: 700,
  borderBottom: '2px solid #333',
  backgroundColor: '#f8f8f8',
  textAlign: 'left',
};

type Mode = 'manuell' | 'kid';

export default function InnbetalingPage() {
  const [mode, setMode] = useState<Mode>('kid');
  const [melding, setMelding] = useState<{ type: 'ok' | 'feil'; tekst: string } | null>(null);

  // KID-matching
  const [kidSkjema, setKidSkjema] = useState<MatchKidRequest>({
    kidNummer: '',
    belop: 0,
    innbetalingsdato: new Date().toISOString().slice(0, 10),
  });

  // Manuell innbetaling
  const [kundeSok, setKundeSok] = useState('');
  const [valgtKundeId, setValgtKundeId] = useState('');
  const [valgtFaktura, setValgtFaktura] = useState<KundeFakturaDto | null>(null);
  const [manuellSkjema, setManuellSkjema] = useState<RegistrerInnbetalingRequest>({
    kundeFakturaId: '',
    innbetalingsdato: new Date().toISOString().slice(0, 10),
    belop: 0,
    betalingsmetode: 'Bank',
  });

  const { data: sokKunder } = useSokKunder(kundeSok);
  const { data: kundeFakturaer } = useKundeFakturaer(
    valgtKundeId ? { kundeId: valgtKundeId, status: 'Utstedt' } : undefined,
  );
  const { data: umatchede } = useUmatchedeInnbetalinger();

  const registrerInnbetaling = useRegistrerInnbetaling();
  const matchKid = useMatchKid();

  function handleKidMatch(e: React.FormEvent) {
    e.preventDefault();
    setMelding(null);
    matchKid.mutate(kidSkjema, {
      onSuccess: (result) => {
        setMelding({
          type: 'ok',
          tekst: `Innbetaling registrert. Faktura matchet via KID. Beløp: ${formatBelop(result.belop)}`,
        });
        setKidSkjema({ kidNummer: '', belop: 0, innbetalingsdato: new Date().toISOString().slice(0, 10) });
      },
      onError: () => {
        setMelding({ type: 'feil', tekst: 'Feil ved KID-matching. Sjekk KID-nummer og beløp.' });
      },
    });
  }

  function handleManuellInnbetaling(e: React.FormEvent) {
    e.preventDefault();
    if (!valgtFaktura) return;
    setMelding(null);
    const request: RegistrerInnbetalingRequest = {
      ...manuellSkjema,
      kundeFakturaId: valgtFaktura.id,
    };
    registrerInnbetaling.mutate(request, {
      onSuccess: (result) => {
        setMelding({
          type: 'ok',
          tekst: `Innbetaling på ${formatBelop(result.belop)} registrert for faktura ${valgtFaktura.fakturanummer}.`,
        });
        setValgtFaktura(null);
        setManuellSkjema({
          kundeFakturaId: '',
          innbetalingsdato: new Date().toISOString().slice(0, 10),
          belop: 0,
          betalingsmetode: 'Bank',
        });
      },
      onError: () => {
        setMelding({ type: 'feil', tekst: 'Feil ved registrering av innbetaling.' });
      },
    });
  }

  function velgFaktura(faktura: KundeFakturaDto) {
    setValgtFaktura(faktura);
    setManuellSkjema((prev) => ({
      ...prev,
      kundeFakturaId: faktura.id,
      belop: faktura.gjenstaendeBelop,
    }));
  }

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ marginBottom: 16 }}>
        <Link to="/kunde" style={{ color: '#0066cc', textDecoration: 'none', fontSize: 14 }}>
          Tilbake til kundeliste
        </Link>
      </div>

      <h1 style={{ marginBottom: 24 }}>Registrer innbetaling</h1>

      {/* Melding */}
      {melding && (
        <div
          style={{
            padding: 12,
            marginBottom: 16,
            borderRadius: 4,
            backgroundColor: melding.type === 'ok' ? '#e8f5e9' : '#ffebee',
            color: melding.type === 'ok' ? '#2e7d32' : '#c62828',
            border: `1px solid ${melding.type === 'ok' ? '#a5d6a7' : '#ef9a9a'}`,
          }}
        >
          {melding.tekst}
        </div>
      )}

      {/* Modus-velger */}
      <div style={{ marginBottom: 24, display: 'flex', gap: 8 }}>
        <button
          onClick={() => setMode('kid')}
          style={{
            padding: '10px 20px',
            background: mode === 'kid' ? '#0066cc' : '#f5f5f5',
            color: mode === 'kid' ? '#fff' : '#333',
            border: mode === 'kid' ? 'none' : '1px solid #ccc',
            borderRadius: 4,
            cursor: 'pointer',
            fontWeight: mode === 'kid' ? 700 : 400,
          }}
        >
          KID-matching
        </button>
        <button
          onClick={() => setMode('manuell')}
          style={{
            padding: '10px 20px',
            background: mode === 'manuell' ? '#0066cc' : '#f5f5f5',
            color: mode === 'manuell' ? '#fff' : '#333',
            border: mode === 'manuell' ? 'none' : '1px solid #ccc',
            borderRadius: 4,
            cursor: 'pointer',
            fontWeight: mode === 'manuell' ? 700 : 400,
          }}
        >
          Manuell registrering
        </button>
      </div>

      {/* KID-matching */}
      {mode === 'kid' && (
        <form
          onSubmit={handleKidMatch}
          style={{
            padding: 16,
            border: '1px solid #e0e0e0',
            borderRadius: 4,
            backgroundColor: '#fafafa',
            marginBottom: 24,
          }}
        >
          <h3 style={{ marginTop: 0 }}>Match innbetaling via KID-nummer</h3>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr 1fr', gap: 12 }}>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                KID-nummer *
              </label>
              <input
                type="text"
                value={kidSkjema.kidNummer}
                onChange={(e) => setKidSkjema({ ...kidSkjema, kidNummer: e.target.value })}
                required
                maxLength={25}
                style={{ width: '100%', padding: '8px 10px', border: '1px solid #ccc', borderRadius: 4, fontFamily: 'monospace' }}
              />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                Beløp *
              </label>
              <input
                type="number"
                value={kidSkjema.belop || ''}
                onChange={(e) => setKidSkjema({ ...kidSkjema, belop: Number(e.target.value) })}
                required
                min={0.01}
                step={0.01}
                style={{ width: '100%', padding: '8px 10px', border: '1px solid #ccc', borderRadius: 4 }}
              />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                Innbetalingsdato *
              </label>
              <input
                type="date"
                value={kidSkjema.innbetalingsdato}
                onChange={(e) => setKidSkjema({ ...kidSkjema, innbetalingsdato: e.target.value })}
                required
                style={{ width: '100%', padding: '8px 10px', border: '1px solid #ccc', borderRadius: 4 }}
              />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                Bankreferanse
              </label>
              <input
                type="text"
                value={kidSkjema.bankreferanse ?? ''}
                onChange={(e) =>
                  setKidSkjema({ ...kidSkjema, bankreferanse: e.target.value || undefined })
                }
                style={{ width: '100%', padding: '8px 10px', border: '1px solid #ccc', borderRadius: 4 }}
              />
            </div>
          </div>
          <button
            type="submit"
            disabled={matchKid.isPending}
            style={{
              marginTop: 12,
              padding: '10px 24px',
              background: '#2e7d32',
              color: '#fff',
              border: 'none',
              borderRadius: 4,
              cursor: 'pointer',
              fontWeight: 600,
            }}
          >
            {matchKid.isPending ? 'Matcher...' : 'Match og registrer'}
          </button>
        </form>
      )}

      {/* Manuell innbetaling */}
      {mode === 'manuell' && (
        <div
          style={{
            padding: 16,
            border: '1px solid #e0e0e0',
            borderRadius: 4,
            backgroundColor: '#fafafa',
            marginBottom: 24,
          }}
        >
          <h3 style={{ marginTop: 0 }}>Manuell innbetalingsregistrering</h3>

          {/* Kundesok */}
          <div style={{ marginBottom: 16 }}>
            <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
              Søk kunde
            </label>
            <input
              type="text"
              placeholder="Søk etter kundenavn eller kundenummer..."
              value={kundeSok}
              onChange={(e) => {
                setKundeSok(e.target.value);
                setValgtKundeId('');
                setValgtFaktura(null);
              }}
              style={{ width: '100%', padding: '8px 10px', border: '1px solid #ccc', borderRadius: 4 }}
            />
            {sokKunder && sokKunder.length > 0 && !valgtKundeId && (
              <div
                style={{
                  border: '1px solid #e0e0e0',
                  borderRadius: 4,
                  marginTop: 4,
                  maxHeight: 200,
                  overflowY: 'auto',
                  backgroundColor: '#fff',
                }}
              >
                {sokKunder.map((k) => (
                  <div
                    key={k.id}
                    onClick={() => {
                      setValgtKundeId(k.id);
                      setKundeSok(`${k.kundenummer} - ${k.navn}`);
                    }}
                    style={{
                      padding: '8px 12px',
                      cursor: 'pointer',
                      borderBottom: '1px solid #f0f0f0',
                    }}
                    onMouseEnter={(e) => {
                      (e.target as HTMLElement).style.backgroundColor = '#e3f2fd';
                    }}
                    onMouseLeave={(e) => {
                      (e.target as HTMLElement).style.backgroundColor = '#fff';
                    }}
                  >
                    {k.kundenummer} - {k.navn}
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Fakturaliste */}
          {valgtKundeId && (
            <div style={{ marginBottom: 16 }}>
              <h4>Åpne fakturaer</h4>
              <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
                <thead>
                  <tr>
                    <th style={headerStyle}>Fakturanr</th>
                    <th style={headerStyle}>Dato</th>
                    <th style={headerStyle}>Forfall</th>
                    <th style={headerStyle}>KID</th>
                    <th style={{ ...headerStyle, textAlign: 'right' }}>Gjenstående</th>
                    <th style={headerStyle}>Status</th>
                    <th style={headerStyle}></th>
                  </tr>
                </thead>
                <tbody>
                  {(kundeFakturaer?.items ?? [])
                    .filter((f) => f.gjenstaendeBelop > 0)
                    .map((f, i) => (
                      <tr
                        key={f.id}
                        style={{
                          backgroundColor:
                            valgtFaktura?.id === f.id
                              ? '#e3f2fd'
                              : i % 2 === 0
                                ? '#fff'
                                : '#fafafa',
                        }}
                      >
                        <td style={{ ...cellStyle, fontFamily: 'monospace' }}>{f.fakturanummer}</td>
                        <td style={cellStyle}>{formatDato(f.fakturadato)}</td>
                        <td
                          style={{
                            ...cellStyle,
                            color: f.erForfalt ? '#c62828' : 'inherit',
                          }}
                        >
                          {formatDato(f.forfallsdato)}
                        </td>
                        <td style={{ ...cellStyle, fontFamily: 'monospace' }}>{f.kidNummer ?? ''}</td>
                        <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                          {formatBelop(f.gjenstaendeBelop)}
                        </td>
                        <td style={cellStyle}>{KundeFakturaStatusNavn[f.status]}</td>
                        <td style={cellStyle}>
                          <button
                            onClick={() => velgFaktura(f)}
                            style={{
                              padding: '4px 12px',
                              background: '#0066cc',
                              color: '#fff',
                              border: 'none',
                              borderRadius: 4,
                              cursor: 'pointer',
                              fontSize: 12,
                            }}
                          >
                            Velg
                          </button>
                        </td>
                      </tr>
                    ))}
                </tbody>
              </table>
            </div>
          )}

          {/* Innbetalingsskjema */}
          {valgtFaktura && (
            <form onSubmit={handleManuellInnbetaling}>
              <div
                style={{
                  padding: 12,
                  marginBottom: 12,
                  backgroundColor: '#e3f2fd',
                  borderRadius: 4,
                  fontSize: 14,
                }}
              >
                Valgt faktura: <strong>{valgtFaktura.fakturanummer}</strong> -
                Gjenstående: <strong>{formatBelop(valgtFaktura.gjenstaendeBelop)}</strong>
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr 1fr', gap: 12 }}>
                <div>
                  <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                    Beløp *
                  </label>
                  <input
                    type="number"
                    value={manuellSkjema.belop || ''}
                    onChange={(e) =>
                      setManuellSkjema({ ...manuellSkjema, belop: Number(e.target.value) })
                    }
                    required
                    min={0.01}
                    step={0.01}
                    style={{ width: '100%', padding: '8px 10px', border: '1px solid #ccc', borderRadius: 4 }}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                    Dato *
                  </label>
                  <input
                    type="date"
                    value={manuellSkjema.innbetalingsdato}
                    onChange={(e) =>
                      setManuellSkjema({ ...manuellSkjema, innbetalingsdato: e.target.value })
                    }
                    required
                    style={{ width: '100%', padding: '8px 10px', border: '1px solid #ccc', borderRadius: 4 }}
                  />
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                    Betalingsmetode
                  </label>
                  <select
                    value={manuellSkjema.betalingsmetode}
                    onChange={(e) =>
                      setManuellSkjema({ ...manuellSkjema, betalingsmetode: e.target.value })
                    }
                    style={{ width: '100%', padding: '8px 10px', border: '1px solid #ccc', borderRadius: 4 }}
                  >
                    <option value="Bank">Bank</option>
                    <option value="Kontant">Kontant</option>
                    <option value="Kort">Kort</option>
                    <option value="Vipps">Vipps</option>
                  </select>
                </div>
                <div>
                  <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                    Bankreferanse
                  </label>
                  <input
                    type="text"
                    value={manuellSkjema.bankreferanse ?? ''}
                    onChange={(e) =>
                      setManuellSkjema({ ...manuellSkjema, bankreferanse: e.target.value || null })
                    }
                    style={{ width: '100%', padding: '8px 10px', border: '1px solid #ccc', borderRadius: 4 }}
                  />
                </div>
              </div>
              <button
                type="submit"
                disabled={registrerInnbetaling.isPending}
                style={{
                  marginTop: 12,
                  padding: '10px 24px',
                  background: '#2e7d32',
                  color: '#fff',
                  border: 'none',
                  borderRadius: 4,
                  cursor: 'pointer',
                  fontWeight: 600,
                }}
              >
                {registrerInnbetaling.isPending ? 'Registrerer...' : 'Registrer innbetaling'}
              </button>
            </form>
          )}
        </div>
      )}

      {/* Umatchede innbetalinger */}
      {umatchede && umatchede.length > 0 && (
        <div style={{ marginTop: 24 }}>
          <h2>Umatchede innbetalinger</h2>
          <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
            <thead>
              <tr>
                <th style={headerStyle}>Bankreferanse</th>
                <th style={headerStyle}>Dato</th>
                <th style={headerStyle}>KID</th>
                <th style={{ ...headerStyle, textAlign: 'right' }}>Beløp</th>
                <th style={headerStyle}>Status</th>
              </tr>
            </thead>
            <tbody>
              {umatchede.map((u, i) => (
                <tr key={u.bankreferanse} style={{ backgroundColor: i % 2 === 0 ? '#fff' : '#fafafa' }}>
                  <td style={{ ...cellStyle, fontFamily: 'monospace' }}>{u.bankreferanse}</td>
                  <td style={cellStyle}>{formatDato(u.dato)}</td>
                  <td style={{ ...cellStyle, fontFamily: 'monospace' }}>{u.kidNummer ?? '-'}</td>
                  <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                    {formatBelop(u.belop)}
                  </td>
                  <td style={cellStyle}>{u.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
