import { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useRegistrerFaktura, useLeverandorOppslag } from '../../hooks/api/useLeverandor';
import { formatBelop } from '../../utils/formatering';
import {
  LeverandorTransaksjonstype,
  Betalingsbetingelse,
} from '../../types/leverandor';
import type {
  RegistrerFakturaRequest,
  FakturaLinjeRequest,
  LeverandorDto,
} from '../../types/leverandor';
import KontoVelger from '../../components/KontoVelger';
import BelopFelt from '../../components/BelopFelt';

interface LinjeTilstand extends FakturaLinjeRequest {
  kontonummer: string;
  mvaBelopBeregnet: number;
}

const tomLinje = (): LinjeTilstand => ({
  kontoId: '',
  kontonummer: '',
  beskrivelse: '',
  belop: 0,
  mvaKode: null,
  avdelingskode: null,
  prosjektkode: null,
  mvaBelopBeregnet: 0,
});

const mvaSatser: Record<string, number> = {
  '1': 25,
  '11': 15,
  '13': 12,
  '14': 25,
  '0': 0,
};

export default function InngaendeFakturaPage() {
  const navigate = useNavigate();
  const registrerMutation = useRegistrerFaktura();

  // Leverandor-sok
  const [leverandorSokTekst, setLeverandorSokTekst] = useState('');
  const [valgtLeverandor, setValgtLeverandor] = useState<LeverandorDto | null>(null);
  const [visLeverandorListe, setVisLeverandorListe] = useState(false);
  const { data: leverandorResultater = [] } = useLeverandorOppslag(leverandorSokTekst);

  // Skjema-tilstand
  const [type, setType] = useState<string>(LeverandorTransaksjonstype.Faktura);
  const [eksternFakturanummer, setEksternFakturanummer] = useState('');
  const [fakturadato, setFakturadato] = useState(new Date().toISOString().slice(0, 10));
  const [forfallsdato, setForfallsdato] = useState('');
  const [beskrivelse, setBeskrivelse] = useState('');
  const [kidNummer, setKidNummer] = useState('');
  const [valutakode, setValutakode] = useState('NOK');
  const [linjer, setLinjer] = useState<LinjeTilstand[]>([tomLinje()]);

  // Beregninger
  const sumEksMva = linjer.reduce((s, l) => s + l.belop, 0);
  const sumMva = linjer.reduce((s, l) => s + l.mvaBelopBeregnet, 0);
  const sumInklMva = sumEksMva + sumMva;

  const velgLeverandor = useCallback((lev: LeverandorDto) => {
    setValgtLeverandor(lev);
    setLeverandorSokTekst(`${lev.leverandornummer} ${lev.navn}`);
    setVisLeverandorListe(false);
    if (lev.betalingsbetingelse !== Betalingsbetingelse.Egendefinert) {
      // Beregn forfallsdato basert pa betingelse
      const dager: Record<string, number> = {
        Netto10: 10, Netto14: 14, Netto20: 20, Netto30: 30,
        Netto45: 45, Netto60: 60, Netto90: 90, Kontant: 0,
      };
      const antallDager = dager[lev.betalingsbetingelse] ?? 30;
      if (fakturadato) {
        const d = new Date(fakturadato);
        d.setDate(d.getDate() + antallDager);
        setForfallsdato(d.toISOString().slice(0, 10));
      }
    }
  }, [fakturadato]);

  function oppdaterLinje(index: number, felt: Partial<LinjeTilstand>) {
    setLinjer((prev) => {
      const nyeLinjer = [...prev];
      nyeLinjer[index] = { ...nyeLinjer[index], ...felt };
      // Beregn MVA
      if (felt.belop !== undefined || felt.mvaKode !== undefined) {
        const linje = nyeLinjer[index];
        const sats = linje.mvaKode ? (mvaSatser[linje.mvaKode] ?? 0) : 0;
        linje.mvaBelopBeregnet = Math.round(linje.belop * sats) / 100;
      }
      return nyeLinjer;
    });
  }

  function leggTilLinje() {
    setLinjer((prev) => [...prev, tomLinje()]);
  }

  function fjernLinje(index: number) {
    if (linjer.length <= 1) return;
    setLinjer((prev) => prev.filter((_, i) => i !== index));
  }

  async function handleRegistrer() {
    if (!valgtLeverandor) return;

    const request: RegistrerFakturaRequest = {
      leverandorId: valgtLeverandor.id,
      eksternFakturanummer,
      type: type as RegistrerFakturaRequest['type'],
      fakturadato,
      forfallsdato: forfallsdato || null,
      beskrivelse,
      kidNummer: kidNummer || null,
      valutakode,
      linjer: linjer.map((l) => ({
        kontoId: l.kontoId,
        beskrivelse: l.beskrivelse,
        belop: l.belop,
        mvaKode: l.mvaKode,
        avdelingskode: l.avdelingskode,
        prosjektkode: l.prosjektkode,
      })),
    };

    try {
      const faktura = await registrerMutation.mutateAsync(request);
      navigate(`/leverandor/${faktura.leverandorId}`);
    } catch {
      // Feil vises via registrerMutation.error
    }
  }

  const kanRegistrere =
    valgtLeverandor &&
    eksternFakturanummer.trim() &&
    fakturadato &&
    beskrivelse.trim() &&
    linjer.length > 0 &&
    linjer.every((l) => l.kontoId && l.belop > 0) &&
    !registrerMutation.isPending;

  return (
    <div style={{ padding: 24, maxWidth: 1100, margin: '0 auto' }}>
      <h1>Registrer inngaende faktura</h1>

      {registrerMutation.error && (
        <div style={{ padding: 12, background: '#ffebee', color: '#c62828', borderRadius: 4, marginBottom: 16 }}>
          Feil ved registrering: {(registrerMutation.error as Error).message}
        </div>
      )}

      {/* Hode-felter */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 16, marginBottom: 24 }}>
        {/* Leverandor-sok */}
        <div style={{ gridColumn: '1 / 3', position: 'relative' }}>
          <label style={labelStil}>
            Leverandor <span style={{ color: 'red' }}>*</span>
          </label>
          <input
            type="text"
            value={leverandorSokTekst}
            onChange={(e) => {
              setLeverandorSokTekst(e.target.value);
              setVisLeverandorListe(e.target.value.length >= 1);
              if (e.target.value === '') setValgtLeverandor(null);
            }}
            onFocus={() => leverandorSokTekst.length >= 1 && setVisLeverandorListe(true)}
            onBlur={() => setTimeout(() => setVisLeverandorListe(false), 200)}
            placeholder="Sok etter leverandor (navn, nr, org.nr)..."
            style={inputStil}
            autoComplete="off"
          />
          {visLeverandorListe && leverandorResultater.length > 0 && (
            <ul style={dropdownStil}>
              {leverandorResultater.map((lev) => (
                <li
                  key={lev.id}
                  onMouseDown={() => velgLeverandor(lev)}
                  style={dropdownElementStil}
                >
                  <span style={{ fontWeight: 600, fontFamily: 'monospace' }}>
                    {lev.leverandornummer}
                  </span>
                  <span>{lev.navn}</span>
                  {lev.organisasjonsnummer && (
                    <span style={{ marginLeft: 'auto', color: '#888', fontSize: 12 }}>
                      {lev.organisasjonsnummer}
                    </span>
                  )}
                </li>
              ))}
            </ul>
          )}
        </div>

        <div>
          <label style={labelStil}>Type</label>
          <select
            value={type}
            onChange={(e) => setType(e.target.value)}
            style={inputStil}
          >
            <option value={LeverandorTransaksjonstype.Faktura}>Faktura</option>
            <option value={LeverandorTransaksjonstype.Kreditnota}>Kreditnota</option>
          </select>
        </div>

        <div>
          <label style={labelStil}>
            Ekstern fakturanummer <span style={{ color: 'red' }}>*</span>
          </label>
          <input
            type="text"
            value={eksternFakturanummer}
            onChange={(e) => setEksternFakturanummer(e.target.value)}
            placeholder="Leverandorens fakturanr"
            style={inputStil}
          />
        </div>

        <div>
          <label style={labelStil}>
            Fakturadato <span style={{ color: 'red' }}>*</span>
          </label>
          <input
            type="date"
            value={fakturadato}
            onChange={(e) => setFakturadato(e.target.value)}
            style={inputStil}
          />
        </div>

        <div>
          <label style={labelStil}>Forfallsdato</label>
          <input
            type="date"
            value={forfallsdato}
            onChange={(e) => setForfallsdato(e.target.value)}
            style={inputStil}
          />
          <span style={{ fontSize: 11, color: '#888' }}>
            Tom = beregnes fra betingelse
          </span>
        </div>

        <div style={{ gridColumn: '1 / 3' }}>
          <label style={labelStil}>
            Beskrivelse <span style={{ color: 'red' }}>*</span>
          </label>
          <input
            type="text"
            value={beskrivelse}
            onChange={(e) => setBeskrivelse(e.target.value)}
            placeholder="Hva gjelder fakturaen?"
            style={inputStil}
          />
        </div>

        <div>
          <label style={labelStil}>KID-nummer</label>
          <input
            type="text"
            value={kidNummer}
            onChange={(e) => setKidNummer(e.target.value)}
            placeholder="KID"
            style={inputStil}
          />
        </div>

        <div>
          <label style={labelStil}>Valuta</label>
          <input
            type="text"
            value={valutakode}
            onChange={(e) => setValutakode(e.target.value.toUpperCase())}
            maxLength={3}
            style={{ ...inputStil, width: 80 }}
          />
        </div>
      </div>

      {/* Fakturalinjer */}
      <h2>Fakturalinjer</h2>
      <table style={tabellStil}>
        <thead>
          <tr>
            <th style={{ ...headerCelleStil, width: 40 }}>#</th>
            <th style={{ ...headerCelleStil, textAlign: 'left', width: 280 }}>Konto</th>
            <th style={{ ...headerCelleStil, textAlign: 'left' }}>Beskrivelse</th>
            <th style={{ ...headerCelleStil, width: 140 }}>Belop eks. MVA</th>
            <th style={{ ...headerCelleStil, width: 100 }}>MVA-kode</th>
            <th style={{ ...headerCelleStil, width: 120 }}>MVA-belop</th>
            <th style={{ ...headerCelleStil, width: 40 }} />
          </tr>
        </thead>
        <tbody>
          {linjer.map((linje, index) => (
            <tr key={index}>
              <td style={celleStil}>{index + 1}</td>
              <td style={{ ...celleStil, textAlign: 'left', padding: 4 }}>
                <KontoVelger
                  verdi={linje.kontonummer}
                  onChange={(kontonummer) => {
                    // TODO: Synk med backend - kontoId burde vaere GUID, bruker kontonummer midlertidig
                    oppdaterLinje(index, {
                      kontoId: kontonummer,
                      kontonummer,
                    });
                  }}
                  placeholder="Velg konto..."
                />
              </td>
              <td style={{ ...celleStil, textAlign: 'left', padding: 4 }}>
                <input
                  type="text"
                  value={linje.beskrivelse}
                  onChange={(e) => oppdaterLinje(index, { beskrivelse: e.target.value })}
                  placeholder="Beskrivelse"
                  style={{ ...inputStil, width: '100%' }}
                />
              </td>
              <td style={{ ...celleStil, padding: 4 }}>
                <BelopFelt
                  verdi={linje.belop}
                  onChange={(v) => oppdaterLinje(index, { belop: v })}
                  paakrevd
                />
              </td>
              <td style={{ ...celleStil, padding: 4 }}>
                <select
                  value={linje.mvaKode ?? ''}
                  onChange={(e) =>
                    oppdaterLinje(index, { mvaKode: e.target.value || null })
                  }
                  style={{ ...inputStil, width: '100%' }}
                >
                  <option value="">Ingen</option>
                  <option value="1">1 - 25% (inn)</option>
                  <option value="11">11 - 15% (naringsmiddel)</option>
                  <option value="13">13 - 12% (transport)</option>
                  <option value="14">14 - 25% (snudd avregn.)</option>
                  <option value="0">0 - Fritatt</option>
                </select>
              </td>
              <td style={{ ...celleStil, fontFamily: 'monospace' }}>
                {formatBelop(linje.mvaBelopBeregnet)}
              </td>
              <td style={{ ...celleStil, padding: 4 }}>
                {linjer.length > 1 && (
                  <button
                    onClick={() => fjernLinje(index)}
                    style={{
                      background: 'none',
                      border: 'none',
                      color: '#c62828',
                      cursor: 'pointer',
                      fontSize: 18,
                      fontWeight: 700,
                    }}
                    title="Fjern linje"
                  >
                    x
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr style={{ backgroundColor: '#f8f8f8' }}>
            <td colSpan={3} style={{ ...sumCelleStil, textAlign: 'left' }}>
              <button onClick={leggTilLinje} style={leggTilKnappStil}>
                + Legg til linje
              </button>
            </td>
            <td style={sumCelleStil}>{formatBelop(sumEksMva)}</td>
            <td style={sumCelleStil} />
            <td style={sumCelleStil}>{formatBelop(sumMva)}</td>
            <td style={sumCelleStil} />
          </tr>
        </tfoot>
      </table>

      {/* Oppsummering */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'flex-end',
          gap: 32,
          marginTop: 16,
          padding: 16,
          backgroundColor: '#f5f5f5',
          borderRadius: 8,
        }}
      >
        <div style={{ textAlign: 'right' }}>
          <div style={{ fontSize: 13, color: '#666' }}>Netto eks. MVA</div>
          <div style={{ fontFamily: 'monospace', fontSize: 16, fontWeight: 600 }}>
            {formatBelop(sumEksMva)}
          </div>
        </div>
        <div style={{ textAlign: 'right' }}>
          <div style={{ fontSize: 13, color: '#666' }}>MVA</div>
          <div style={{ fontFamily: 'monospace', fontSize: 16, fontWeight: 600 }}>
            {formatBelop(sumMva)}
          </div>
        </div>
        <div style={{ textAlign: 'right' }}>
          <div style={{ fontSize: 13, color: '#666' }}>Totalt inkl. MVA</div>
          <div style={{ fontFamily: 'monospace', fontSize: 20, fontWeight: 700 }}>
            {formatBelop(sumInklMva)}
          </div>
        </div>
      </div>

      {/* Bilagsoversikt */}
      <div style={{ marginTop: 16, padding: 12, backgroundColor: '#e3f2fd', borderRadius: 8, fontSize: 13 }}>
        <strong>Bilag som opprettes automatisk:</strong>
        <ul style={{ margin: '4px 0 0', paddingLeft: 20 }}>
          {linjer.filter((l) => l.belop > 0).map((l, i) => (
            <li key={i}>
              Debet {l.kontonummer || '???'}: {formatBelop(l.belop)} ({l.beskrivelse || 'ubeskrevet'})
            </li>
          ))}
          {sumMva > 0 && <li>Debet 2710 Inngaende MVA: {formatBelop(sumMva)}</li>}
          <li>Kredit 2400 Leverandorgjeld: {formatBelop(sumInklMva)}</li>
        </ul>
      </div>

      {/* Handlingsknapper */}
      <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 12, marginTop: 24 }}>
        <button
          onClick={() => navigate('/leverandor')}
          style={sekundaerKnappStil}
        >
          Avbryt
        </button>
        <button
          onClick={handleRegistrer}
          disabled={!kanRegistrere}
          style={{
            ...primaerKnappStil,
            opacity: kanRegistrere ? 1 : 0.5,
            cursor: kanRegistrere ? 'pointer' : 'not-allowed',
          }}
        >
          {registrerMutation.isPending ? 'Registrerer...' : 'Registrer faktura'}
        </button>
      </div>
    </div>
  );
}

// --- Stiler ---

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

const dropdownStil: React.CSSProperties = {
  position: 'absolute',
  top: '100%',
  left: 0,
  right: 0,
  margin: 0,
  padding: 0,
  listStyle: 'none',
  background: '#fff',
  border: '1px solid #ccc',
  borderTop: 'none',
  borderRadius: '0 0 4px 4px',
  maxHeight: 250,
  overflowY: 'auto',
  zIndex: 100,
  boxShadow: '0 4px 8px rgba(0,0,0,0.1)',
};

const dropdownElementStil: React.CSSProperties = {
  padding: '8px 12px',
  cursor: 'pointer',
  borderBottom: '1px solid #f0f0f0',
  display: 'flex',
  gap: 12,
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
  fontFamily: 'monospace',
};

const leggTilKnappStil: React.CSSProperties = {
  background: 'none',
  border: 'none',
  color: '#1565c0',
  cursor: 'pointer',
  fontSize: 13,
  fontWeight: 600,
  padding: 0,
};

const primaerKnappStil: React.CSSProperties = {
  padding: '10px 24px',
  backgroundColor: '#1565c0',
  color: '#fff',
  border: 'none',
  borderRadius: 4,
  cursor: 'pointer',
  fontSize: 14,
  fontWeight: 600,
};

const sekundaerKnappStil: React.CSSProperties = {
  padding: '10px 24px',
  backgroundColor: '#f5f5f5',
  color: '#333',
  border: '1px solid #ccc',
  borderRadius: 4,
  cursor: 'pointer',
  fontSize: 14,
};
