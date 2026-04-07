import { useState } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import {
  useArsavslutningStatus,
  useKjorArsavslutning,
  useReverserArsavslutning,
} from '../../hooks/api/usePeriodeavslutning';
import { ArsavslutningFaseNavn } from '../../types/periodeavslutning';
import type { ArsavslutningDto, ArsavslutningStegDto } from '../../types/periodeavslutning';
import { formatBelop } from '../../utils/formatering';

type WizardSteg = 'oversikt' | 'konfigurasjon' | 'kjorer' | 'resultat';

const ARSAVSLUTNING_STEG_BESKRIVELSER: Record<string, string> = {
  'VALIDER_FORUTSETNINGER': 'Kontrollerer at alle perioder er lukket',
  'BEREGN_ARSRESULTAT': 'Beregner årsresultat (netto kontoklasse 3-8)',
  'OPPRETT_ARSAVSLUTNINGSBILAG': 'Oppretter bilag for nullstilling av resultatkontoer',
  'BOKFOR_ARSAVSLUTNINGSBILAG': 'Bokfører årsavslutningsbilaget',
  'OPPRETT_APNINGSBALANSE': 'Oppretter åpningsbalanse for neste år',
  'LUKK_PERIODE_13': 'Lukker årsavslutningsperioden',
  'OPPDATER_STATUS': 'Oppdaterer årsavslutningsstatus',
};

function stegIkon(status: string): string {
  switch (status) {
    case 'OK': return '\u2705';
    case 'KJORER': return '\u23F3';
    case 'VENTER': return '\u23F8\uFE0F';
    case 'FEIL': return '\u274C';
    default: return '\u2B1C';
  }
}

function stegFarge(status: string): string {
  switch (status) {
    case 'OK': return '#2e7d32';
    case 'KJORER': return '#1565c0';
    case 'FEIL': return '#c62828';
    default: return '#999';
  }
}

export default function ArsavslutningPage() {
  const [searchParams] = useSearchParams();
  const ar = Number(searchParams.get('ar')) || new Date().getFullYear();

  const { data: status, isLoading } = useArsavslutningStatus(ar);
  const kjorArsavslutning = useKjorArsavslutning();
  const reverserArsavslutning = useReverserArsavslutning();

  const [wizardSteg, setWizardSteg] = useState<WizardSteg>('oversikt');
  const [disponeringKonto, setDisponeringKonto] = useState('2050');
  const [utbytte, setUtbytte] = useState<string>('');
  const [utbytteKonto, setUtbytteKonto] = useState('2800');
  const [resultat, setResultat] = useState<ArsavslutningDto | null>(null);
  const [bekreftReverser, setBekreftReverser] = useState(false);

  const erFullfort = status?.fase === 'Fullfort';

  function handleStartArsavslutning() {
    const utbytteVerdi = utbytte ? parseFloat(utbytte.replace(',', '.')) : undefined;
    kjorArsavslutning.mutate(
      {
        ar,
        request: {
          disponeringKontonummer: disponeringKonto,
          utbytte: utbytteVerdi,
          utbytteKontonummer: utbytteVerdi ? utbytteKonto : undefined,
        },
      },
      {
        onSuccess: (data) => {
          setResultat(data);
          setWizardSteg('resultat');
        },
        onError: () => {
          setWizardSteg('konfigurasjon');
        },
      },
    );
    setWizardSteg('kjorer');
  }

  function handleReverser() {
    reverserArsavslutning.mutate(ar, {
      onSuccess: () => {
        setBekreftReverser(false);
        setWizardSteg('oversikt');
        setResultat(null);
      },
    });
  }

  if (isLoading) {
    return (
      <div style={{ padding: 24 }}>
        <p>Laster årsavslutningsstatus...</p>
      </div>
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 900, margin: '0 auto' }}>
      <Link to="/periodeavslutning" style={{ color: '#0066cc', fontSize: 13, textDecoration: 'none' }}>
        &larr; Tilbake til periodeoversikt
      </Link>

      <h1 style={{ marginTop: 12 }}>Årsavslutning {ar}</h1>

      {/* Status-boks */}
      <div style={{
        padding: 16,
        marginBottom: 24,
        borderRadius: 8,
        background: erFullfort ? '#e8f5e9' : '#f5f5f5',
        border: `1px solid ${erFullfort ? '#a5d6a7' : '#e0e0e0'}`,
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
      }}>
        <div>
          <div style={{ fontWeight: 700, fontSize: 16 }}>
            Status: {status ? ArsavslutningFaseNavn[status.fase] : 'Ukjent'}
          </div>
          {status?.arsresultat != null && (
            <div style={{ marginTop: 4, fontSize: 14 }}>
              Årsresultat: <strong style={{ color: status.arsresultat >= 0 ? '#2e7d32' : '#c62828' }}>
                {formatBelop(status.arsresultat)}
              </strong>
            </div>
          )}
          {status?.fullfortTidspunkt && (
            <div style={{ marginTop: 4, fontSize: 13, color: '#666' }}>
              Fullført: {new Date(status.fullfortTidspunkt).toLocaleString('nb-NO')}
              {status.fullfortAv && ` av ${status.fullfortAv}`}
            </div>
          )}
        </div>
        {erFullfort && (
          <button
            onClick={() => setBekreftReverser(true)}
            style={{ padding: '8px 16px', background: '#fff', border: '1px solid #c62828', color: '#c62828', borderRadius: 4, fontSize: 13, cursor: 'pointer' }}
          >
            Reverser
          </button>
        )}
      </div>

      {/* Bekreft reversering dialog */}
      {bekreftReverser && (
        <div style={{
          padding: 16,
          marginBottom: 24,
          background: '#ffebee',
          border: '1px solid #ef9a9a',
          borderRadius: 8,
        }}>
          <p style={{ fontWeight: 600, color: '#c62828' }}>
            Er du sikker på at du vil reversere årsavslutningen for {ar}?
          </p>
          <p style={{ fontSize: 13, color: '#666', marginBottom: 12 }}>
            Dette sletter årsavslutningsbilaget og åpningsbalansen. Kun mulig hvis neste års perioder ikke har posteringer.
          </p>
          <div style={{ display: 'flex', gap: 8 }}>
            <button onClick={handleReverser} disabled={reverserArsavslutning.isPending} style={{ padding: '8px 16px', background: '#c62828', color: '#fff', border: 'none', borderRadius: 4, cursor: 'pointer' }}>
              {reverserArsavslutning.isPending ? 'Reverserer...' : 'Bekreft reversering'}
            </button>
            <button onClick={() => setBekreftReverser(false)} style={{ padding: '8px 16px', background: '#f5f5f5', border: '1px solid #ccc', borderRadius: 4, cursor: 'pointer' }}>
              Avbryt
            </button>
          </div>
          {reverserArsavslutning.isError && (
            <p style={{ color: 'red', marginTop: 8, fontSize: 13 }}>Feil ved reversering.</p>
          )}
        </div>
      )}

      {/* Wizard steg-indikator */}
      {!erFullfort && (
        <div style={{ display: 'flex', gap: 4, marginBottom: 32 }}>
          {(['oversikt', 'konfigurasjon', 'kjorer', 'resultat'] as WizardSteg[]).map((s, i) => {
            const labels = ['1. Forberedelse', '2. Konfigurasjon', '3. Gjennomfør', '4. Resultat'];
            const erAktivt = s === wizardSteg;
            const erFullfortSteg = ['oversikt', 'konfigurasjon', 'kjorer', 'resultat'].indexOf(wizardSteg) > i;
            return (
              <div
                key={s}
                style={{
                  flex: 1,
                  padding: '10px 16px',
                  background: erAktivt ? '#1565c0' : erFullfortSteg ? '#e8f5e9' : '#f5f5f5',
                  color: erAktivt ? '#fff' : erFullfortSteg ? '#2e7d32' : '#666',
                  borderRadius: 4,
                  fontWeight: erAktivt ? 700 : 400,
                  fontSize: 14,
                  textAlign: 'center',
                }}
              >
                {labels[i]}
              </div>
            );
          })}
        </div>
      )}

      {/* Steg 1: Oversikt/forberedelse */}
      {wizardSteg === 'oversikt' && !erFullfort && (
        <div>
          <h2 style={{ fontSize: 18, marginBottom: 16 }}>Forberedelse</h2>
          <div style={{ background: '#f5f5f5', borderRadius: 8, padding: 16, marginBottom: 24 }}>
            <h3 style={{ fontSize: 14, marginTop: 0 }}>Årsavslutningsprosessen gjennomfører:</h3>
            <ol style={{ fontSize: 14, lineHeight: 1.8 }}>
              <li>Validerer at alle perioder 1-12 er lukket</li>
              <li>Beregner årsresultat (netto for kontoklasse 3-8)</li>
              <li>Oppretter årsavslutningsbilag med nullstilling av resultatkontoer</li>
              <li>Disponerer resultatet til egenkapitalkonto</li>
              <li>Bokfører årsavslutningsbilaget i periode 13</li>
              <li>Oppretter åpningsbalanse for {ar + 1}</li>
              <li>Lukker periode 13</li>
            </ol>
          </div>

          <div style={{
            padding: 16,
            background: '#fff8e1',
            border: '1px solid #ffe082',
            borderRadius: 8,
            marginBottom: 24,
            fontSize: 14,
          }}>
            <strong>Viktig:</strong> Årsavslutningen er en omfattende prosess. Sørg for at alle perioder er lukket
            og at bokføringen er komplett før du starter.
          </div>

          <button onClick={() => setWizardSteg('konfigurasjon')} style={primaryButtonStyle}>
            Gå til konfigurasjon
          </button>
        </div>
      )}

      {/* Steg 2: Konfigurasjon */}
      {wizardSteg === 'konfigurasjon' && (
        <div>
          <h2 style={{ fontSize: 18, marginBottom: 16 }}>Konfigurasjon</h2>

          <div style={{ marginBottom: 20 }}>
            <label style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 14 }}>
              Disponeringskonto:
            </label>
            <select
              value={disponeringKonto}
              onChange={(e) => setDisponeringKonto(e.target.value)}
              style={selectStyle}
            >
              <option value="2050">2050 - Annen innskutt egenkapital</option>
              <option value="2100">2100 - Udisponert resultat</option>
            </select>
            <p style={{ fontSize: 12, color: '#666', marginTop: 4 }}>
              Konto der årsresultatet disponeres (overskudd krediteres, underskudd debiteres).
            </p>
          </div>

          <div style={{ marginBottom: 20 }}>
            <label style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 14 }}>
              Utbytte (valgfritt):
            </label>
            <input
              type="text"
              value={utbytte}
              onChange={(e) => setUtbytte(e.target.value)}
              placeholder="0,00"
              style={inputStyle}
            />
            <p style={{ fontSize: 12, color: '#666', marginTop: 4 }}>
              Eventuelt utbytte som trekkes fra disponeringsbeløpet. Sett til 0 eller la feltet stå tomt.
            </p>
          </div>

          {utbytte && parseFloat(utbytte.replace(',', '.')) > 0 && (
            <div style={{ marginBottom: 20 }}>
              <label style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 14 }}>
                Utbyttekonto:
              </label>
              <select
                value={utbytteKonto}
                onChange={(e) => setUtbytteKonto(e.target.value)}
                style={selectStyle}
              >
                <option value="2800">2800 - Avsatt utbytte</option>
              </select>
            </div>
          )}

          <div style={{ display: 'flex', gap: 12, marginTop: 24 }}>
            <button onClick={() => setWizardSteg('oversikt')} style={secondaryButtonStyle}>
              Tilbake
            </button>
            <button onClick={handleStartArsavslutning} style={primaryButtonStyle}>
              Start årsavslutning
            </button>
          </div>
        </div>
      )}

      {/* Steg 3: Kjører */}
      {wizardSteg === 'kjorer' && (
        <div style={{ textAlign: 'center', padding: 48 }}>
          <div style={{ fontSize: 32, marginBottom: 16 }}>{'\u23F3'}</div>
          <h2>Gjennomfører årsavslutning...</h2>
          <p style={{ color: '#666' }}>Vennligst vent. Dette kan ta noen sekunder.</p>
          {kjorArsavslutning.isError && (
            <div style={{ marginTop: 24 }}>
              <p style={{ color: '#c62828', fontWeight: 600 }}>Feil under årsavslutning</p>
              <p style={{ color: '#666', fontSize: 13 }}>Kontroller at alle perioder er lukket og prøv igjen.</p>
              <button onClick={() => setWizardSteg('konfigurasjon')} style={secondaryButtonStyle}>
                Tilbake til konfigurasjon
              </button>
            </div>
          )}
        </div>
      )}

      {/* Steg 4: Resultat */}
      {wizardSteg === 'resultat' && resultat && (
        <div>
          <div style={{
            padding: 16,
            marginBottom: 24,
            borderRadius: 8,
            background: '#e8f5e9',
            border: '1px solid #a5d6a7',
            textAlign: 'center',
          }}>
            <div style={{ fontSize: 32, marginBottom: 8 }}>{'\u2705'}</div>
            <h2 style={{ margin: 0, color: '#2e7d32' }}>Årsavslutning fullført</h2>
          </div>

          {/* Sammendrag */}
          <div style={{ display: 'flex', gap: 16, marginBottom: 24, flexWrap: 'wrap' }}>
            <div style={resultatBoks}>
              <div style={{ fontSize: 12, color: '#666' }}>Årsresultat</div>
              <div style={{ fontSize: 20, fontWeight: 700, color: resultat.arsresultat >= 0 ? '#2e7d32' : '#c62828' }}>
                {formatBelop(resultat.arsresultat)}
              </div>
            </div>
            <div style={resultatBoks}>
              <div style={{ fontSize: 12, color: '#666' }}>Disponert til EK</div>
              <div style={{ fontSize: 20, fontWeight: 700 }}>
                {formatBelop(resultat.disponertTilEgenkapital)}
              </div>
            </div>
            {resultat.utbytte != null && resultat.utbytte > 0 && (
              <div style={resultatBoks}>
                <div style={{ fontSize: 12, color: '#666' }}>Utbytte</div>
                <div style={{ fontSize: 20, fontWeight: 700 }}>
                  {formatBelop(resultat.utbytte)}
                </div>
              </div>
            )}
          </div>

          {/* Steg-logg */}
          <h3 style={{ fontSize: 16, marginBottom: 12 }}>Gjennomførte steg</h3>
          <div style={{ marginBottom: 24 }}>
            {resultat.steg.map((s: ArsavslutningStegDto, i: number) => (
              <div
                key={i}
                style={{
                  display: 'flex',
                  alignItems: 'flex-start',
                  gap: 12,
                  padding: '10px 12px',
                  borderBottom: '1px solid #f0f0f0',
                }}
              >
                <span style={{ fontSize: 18, lineHeight: 1 }}>{stegIkon(s.status)}</span>
                <div style={{ flex: 1 }}>
                  <div style={{ fontWeight: 600, fontSize: 14, color: stegFarge(s.status) }}>
                    {ARSAVSLUTNING_STEG_BESKRIVELSER[s.steg] ?? s.steg}
                  </div>
                  <div style={{ fontSize: 13, color: '#666' }}>{s.beskrivelse}</div>
                  {s.detaljer && (
                    <div style={{ fontSize: 12, color: '#999', marginTop: 2 }}>{s.detaljer}</div>
                  )}
                </div>
                <span style={{ fontSize: 12, color: stegFarge(s.status), fontWeight: 600 }}>
                  {s.status}
                </span>
              </div>
            ))}
          </div>

          {/* Referanser */}
          <div style={{ display: 'flex', gap: 16, marginBottom: 24, flexWrap: 'wrap' }}>
            {resultat.arsavslutningBilagId && (
              <Link
                to={`/bilag/${resultat.arsavslutningBilagId}`}
                style={{ ...actionLinkStyle, background: '#e3f2fd', color: '#1565c0' }}
              >
                Se årsavslutningsbilag
              </Link>
            )}
            {resultat.apningsbalanseBilagId && (
              <Link
                to={`/bilag/${resultat.apningsbalanseBilagId}`}
                style={{ ...actionLinkStyle, background: '#f3e5f5', color: '#7b1fa2' }}
              >
                Se åpningsbalanse {ar + 1}
              </Link>
            )}
          </div>

          <Link to="/periodeavslutning" style={primaryButtonStyle}>
            Tilbake til oversikt
          </Link>
        </div>
      )}

      {/* Fullført: vis eksisterende resultat */}
      {erFullfort && wizardSteg === 'oversikt' && (
        <div>
          <h2 style={{ fontSize: 18, marginBottom: 16 }}>Årsavslutning er fullført</h2>
          <p style={{ color: '#666', marginBottom: 16 }}>
            Årsavslutningen for {ar} er gjennomført. Alle resultatkontoer er nullstilt og
            åpningsbalanse for {ar + 1} er opprettet.
          </p>
          <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap' }}>
            {status?.arsavslutningBilagId && (
              <Link
                to={`/bilag/${status.arsavslutningBilagId}`}
                style={{ ...actionLinkStyle, background: '#e3f2fd', color: '#1565c0' }}
              >
                Se årsavslutningsbilag
              </Link>
            )}
            {status?.apningsbalanseBilagId && (
              <Link
                to={`/bilag/${status.apningsbalanseBilagId}`}
                style={{ ...actionLinkStyle, background: '#f3e5f5', color: '#7b1fa2' }}
              >
                Se åpningsbalanse {ar + 1}
              </Link>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

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

const secondaryButtonStyle: React.CSSProperties = {
  padding: '10px 24px',
  background: '#f5f5f5',
  color: '#333',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  cursor: 'pointer',
};

const selectStyle: React.CSSProperties = {
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  minWidth: 300,
};

const inputStyle: React.CSSProperties = {
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  width: 200,
};

const resultatBoks: React.CSSProperties = {
  padding: 16,
  border: '1px solid #e0e0e0',
  borderRadius: 8,
  minWidth: 160,
  textAlign: 'center',
};

const actionLinkStyle: React.CSSProperties = {
  padding: '8px 16px',
  borderRadius: 4,
  textDecoration: 'none',
  fontSize: 14,
  fontWeight: 600,
};
