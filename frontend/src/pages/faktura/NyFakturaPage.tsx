import { useState, useMemo, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSokKunder } from '../../hooks/api/useKunde';
import { useOpprettFaktura } from '../../hooks/api/useFaktura';
import {
  Enhet,
  EnhetNavn,
  RabattType,
  FakturaLeveringsformat,
  FakturaLeveringsformatNavn,
  StandardMvaKoder,
} from '../../types/faktura';
import type {
  FakturaLinjeSkjema,
  FakturaLinjeBeregnet,
  OpprettFakturaRequest,
} from '../../types/faktura';
import type { KundeDto } from '../../types/kunde';
import { formatBelop } from '../../utils/formatering';

// --- Linje-beregninger (FR-F02) ---

function beregnLinje(linje: FakturaLinjeSkjema): FakturaLinjeBeregnet {
  const bruttolinjebelop = linje.antall * linje.enhetspris;
  let rabattBelop = 0;
  if (linje.rabattType === RabattType.Prosent && linje.rabattProsent !== null) {
    rabattBelop = bruttolinjebelop * linje.rabattProsent / 100;
  } else if (linje.rabattType === RabattType.Belop && linje.rabattBelop !== null) {
    rabattBelop = linje.rabattBelop;
  }
  const nettobelop = bruttolinjebelop - rabattBelop;
  const mvaBelop = Math.round(nettobelop * linje.mvaSats / 100 * 100) / 100;
  const bruttobelop = nettobelop + mvaBelop;
  return { bruttolinjebelop, rabattBelop, nettobelop, mvaBelop, bruttobelop };
}

let linjeCounter = 0;

function nyLinje(): FakturaLinjeSkjema {
  linjeCounter += 1;
  return {
    key: `linje-${linjeCounter}`,
    beskrivelse: '',
    antall: 1,
    enhet: Enhet.Stykk,
    enhetspris: 0,
    mvaKode: '3',
    mvaSats: 25,
    kontoId: '',
    kontonummer: '3000',
    rabattType: null,
    rabattProsent: null,
    rabattBelop: null,
  };
}

// --- Styles ---

const labelStyle: React.CSSProperties = {
  display: 'block',
  marginBottom: 4,
  fontWeight: 600,
  fontSize: 13,
};

const inputStyle: React.CSSProperties = {
  width: '100%',
  padding: '6px 10px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  boxSizing: 'border-box',
};

const sectionStyle: React.CSSProperties = {
  padding: 16,
  marginBottom: 16,
  border: '1px solid #e0e0e0',
  borderRadius: 4,
  backgroundColor: '#fafafa',
};

const btnPrimary: React.CSSProperties = {
  padding: '10px 24px',
  background: '#2e7d32',
  color: '#fff',
  border: 'none',
  borderRadius: 4,
  cursor: 'pointer',
  fontSize: 14,
  fontWeight: 600,
};

const btnSecondary: React.CSSProperties = {
  padding: '10px 24px',
  background: '#f5f5f5',
  color: '#333',
  border: '1px solid #ccc',
  borderRadius: 4,
  cursor: 'pointer',
  fontSize: 14,
};

export default function NyFakturaPage() {
  const navigate = useNavigate();
  const opprettFaktura = useOpprettFaktura();

  // --- Kundesøk ---
  const [kundeQuery, setKundeQuery] = useState('');
  const [valgtKunde, setValgtKunde] = useState<KundeDto | null>(null);
  const [visKundeListe, setVisKundeListe] = useState(false);
  const { data: kunderSok } = useSokKunder(kundeQuery);

  // --- Faktura-felt ---
  const [leveringsdato, setLeveringsdato] = useState('');
  const [bestillingsnummer, setBestillingsnummer] = useState('');
  const [kjopersReferanse, setKjopersReferanse] = useState('');
  const [vaarReferanse, setVaarReferanse] = useState('');
  const [merknad, setMerknad] = useState('');
  const [leveringsformat, setLeveringsformat] = useState<string>(FakturaLeveringsformat.Epost);
  const [valutakode] = useState('NOK');

  // --- Linjer ---
  const [linjer, setLinjer] = useState<FakturaLinjeSkjema[]>([nyLinje()]);

  // --- Beregnede totaler (FR-F03) ---
  const beregnedLinjer = useMemo(() => linjer.map(beregnLinje), [linjer]);

  const totaler = useMemo(() => {
    let sumEksMva = 0;
    let sumMva = 0;
    for (const b of beregnedLinjer) {
      sumEksMva += b.nettobelop;
      sumMva += b.mvaBelop;
    }
    return {
      eksMva: Math.round(sumEksMva * 100) / 100,
      mva: Math.round(sumMva * 100) / 100,
      inkMva: Math.round((sumEksMva + sumMva) * 100) / 100,
    };
  }, [beregnedLinjer]);

  // MVA-oppsummering per sats
  const mvaOversikt = useMemo(() => {
    const map = new Map<string, { sats: number; grunnlag: number; mva: number }>();
    linjer.forEach((linje, i) => {
      const b = beregnedLinjer[i];
      const existing = map.get(linje.mvaKode);
      if (existing) {
        existing.grunnlag += b.nettobelop;
        existing.mva += b.mvaBelop;
      } else {
        map.set(linje.mvaKode, {
          sats: linje.mvaSats,
          grunnlag: b.nettobelop,
          mva: b.mvaBelop,
        });
      }
    });
    return Array.from(map.entries()).map(([kode, v]) => ({
      kode,
      ...v,
    }));
  }, [linjer, beregnedLinjer]);

  // --- Linje-operasjoner ---

  const oppdaterLinje = useCallback(
    (index: number, felt: Partial<FakturaLinjeSkjema>) => {
      setLinjer((prev) => {
        const kopi = [...prev];
        kopi[index] = { ...kopi[index], ...felt };
        return kopi;
      });
    },
    [],
  );

  const leggTilLinje = useCallback(() => {
    setLinjer((prev) => [...prev, nyLinje()]);
  }, []);

  const fjernLinje = useCallback(
    (index: number) => {
      setLinjer((prev) => {
        if (prev.length <= 1) return prev;
        return prev.filter((_, i) => i !== index);
      });
    },
    [],
  );

  // --- Velg kunde ---

  function velgKunde(kunde: KundeDto) {
    setValgtKunde(kunde);
    setKundeQuery(kunde.navn);
    setVisKundeListe(false);
  }

  // --- Send inn ---

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!valgtKunde) return;

    const request: OpprettFakturaRequest = {
      kundeId: valgtKunde.id,
      leveringsdato: leveringsdato || null,
      bestillingsnummer: bestillingsnummer || null,
      kjopersReferanse: kjopersReferanse || null,
      vaarReferanse: vaarReferanse || null,
      merknad: merknad || null,
      valutakode,
      leveringsformat: leveringsformat as OpprettFakturaRequest['leveringsformat'],
      linjer: linjer.map((l) => ({
        beskrivelse: l.beskrivelse,
        antall: l.antall,
        enhet: l.enhet,
        enhetspris: l.enhetspris,
        mvaKode: l.mvaKode,
        kontoId: l.kontoId || '00000000-0000-0000-0000-000000000000',
        rabattType: l.rabattType,
        rabattProsent: l.rabattProsent,
        rabattBelop: l.rabattBelop,
      })),
    };

    opprettFaktura.mutate(request, {
      onSuccess: (data) => {
        navigate(`/faktura/${data.id}`);
      },
    });
  }

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h1 style={{ margin: 0 }}>Ny faktura</h1>
        <button
          type="button"
          onClick={() => navigate('/faktura')}
          style={btnSecondary}
        >
          Tilbake til liste
        </button>
      </div>

      <form onSubmit={handleSubmit}>
        {/* Kundevalg */}
        <div style={sectionStyle}>
          <h3 style={{ marginTop: 0, marginBottom: 12 }}>Kunde</h3>
          <div style={{ position: 'relative', maxWidth: 500 }}>
            <label style={labelStyle}>Søk og velg kunde *</label>
            <input
              type="text"
              value={kundeQuery}
              onChange={(e) => {
                setKundeQuery(e.target.value);
                setVisKundeListe(true);
                if (valgtKunde && e.target.value !== valgtKunde.navn) {
                  setValgtKunde(null);
                }
              }}
              onFocus={() => {
                if (kundeQuery.length >= 2) setVisKundeListe(true);
              }}
              placeholder="Skriv kundenavn, kundenummer eller org.nr..."
              style={inputStyle}
              required
            />
            {visKundeListe && kunderSok && kunderSok.length > 0 && (
              <div
                style={{
                  position: 'absolute',
                  top: '100%',
                  left: 0,
                  right: 0,
                  background: '#fff',
                  border: '1px solid #ccc',
                  borderRadius: 4,
                  maxHeight: 200,
                  overflowY: 'auto',
                  zIndex: 10,
                  boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
                }}
              >
                {kunderSok.map((k) => (
                  <div
                    key={k.id}
                    onClick={() => velgKunde(k)}
                    style={{
                      padding: '8px 12px',
                      cursor: 'pointer',
                      borderBottom: '1px solid #f0f0f0',
                      fontSize: 14,
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.backgroundColor = '#e3f2fd';
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.backgroundColor = '#fff';
                    }}
                  >
                    <strong>{k.kundenummer}</strong> - {k.navn}
                    {k.organisasjonsnummer && (
                      <span style={{ color: '#888', marginLeft: 8 }}>
                        (Org: {k.organisasjonsnummer})
                      </span>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
          {valgtKunde && (
            <div
              style={{
                marginTop: 12,
                padding: 12,
                backgroundColor: '#e8f5e9',
                borderRadius: 4,
                fontSize: 14,
              }}
            >
              <strong>{valgtKunde.kundenummer} - {valgtKunde.navn}</strong>
              {valgtKunde.adresse1 && <div>{valgtKunde.adresse1}</div>}
              {valgtKunde.postnummer && (
                <div>
                  {valgtKunde.postnummer} {valgtKunde.poststed}
                </div>
              )}
              {valgtKunde.epost && <div>E-post: {valgtKunde.epost}</div>}
              {valgtKunde.kanMottaEhf && (
                <span
                  style={{
                    display: 'inline-block',
                    marginTop: 4,
                    padding: '2px 6px',
                    backgroundColor: '#1565c0',
                    color: '#fff',
                    borderRadius: 4,
                    fontSize: 11,
                  }}
                >
                  EHF
                </span>
              )}
            </div>
          )}
        </div>

        {/* Faktura-detaljer */}
        <div style={sectionStyle}>
          <h3 style={{ marginTop: 0, marginBottom: 12 }}>Detaljer</h3>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr 1fr', gap: 12 }}>
            <div>
              <label style={labelStyle}>Leveringsdato</label>
              <input
                type="date"
                value={leveringsdato}
                onChange={(e) => setLeveringsdato(e.target.value)}
                style={inputStyle}
              />
            </div>
            <div>
              <label style={labelStyle}>Leveringsformat</label>
              <select
                value={leveringsformat}
                onChange={(e) => setLeveringsformat(e.target.value)}
                style={inputStyle}
              >
                {Object.entries(FakturaLeveringsformatNavn).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label style={labelStyle}>Bestillingsnummer</label>
              <input
                type="text"
                value={bestillingsnummer}
                onChange={(e) => setBestillingsnummer(e.target.value)}
                placeholder="PO-nummer"
                style={inputStyle}
              />
            </div>
            <div>
              <label style={labelStyle}>
                Kjøpers referanse
                {leveringsformat === FakturaLeveringsformat.Ehf && ' *'}
              </label>
              <input
                type="text"
                value={kjopersReferanse}
                onChange={(e) => setKjopersReferanse(e.target.value)}
                placeholder="Kontaktperson hos kunde"
                required={
                  leveringsformat === FakturaLeveringsformat.Ehf && !bestillingsnummer
                }
                style={inputStyle}
              />
            </div>
            <div>
              <label style={labelStyle}>Vår referanse</label>
              <input
                type="text"
                value={vaarReferanse}
                onChange={(e) => setVaarReferanse(e.target.value)}
                style={inputStyle}
              />
            </div>
            <div style={{ gridColumn: 'span 3' }}>
              <label style={labelStyle}>Merknad</label>
              <input
                type="text"
                value={merknad}
                onChange={(e) => setMerknad(e.target.value)}
                placeholder="Fritekst på faktura"
                style={inputStyle}
              />
            </div>
          </div>
        </div>

        {/* Fakturalinjer */}
        <div style={sectionStyle}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
            <h3 style={{ margin: 0 }}>Fakturalinjer</h3>
            <button
              type="button"
              onClick={leggTilLinje}
              style={{
                padding: '6px 14px',
                background: '#0066cc',
                color: '#fff',
                border: 'none',
                borderRadius: 4,
                cursor: 'pointer',
                fontSize: 13,
              }}
            >
              + Legg til linje
            </button>
          </div>

          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ backgroundColor: '#f0f0f0' }}>
                <th style={{ padding: '6px 8px', textAlign: 'left', fontSize: 12, fontWeight: 700, width: '25%' }}>
                  Beskrivelse *
                </th>
                <th style={{ padding: '6px 8px', textAlign: 'right', fontSize: 12, fontWeight: 700, width: '8%' }}>
                  Antall
                </th>
                <th style={{ padding: '6px 8px', textAlign: 'left', fontSize: 12, fontWeight: 700, width: '8%' }}>
                  Enhet
                </th>
                <th style={{ padding: '6px 8px', textAlign: 'right', fontSize: 12, fontWeight: 700, width: '10%' }}>
                  Enhetspris
                </th>
                <th style={{ padding: '6px 8px', textAlign: 'left', fontSize: 12, fontWeight: 700, width: '10%' }}>
                  Rabatt
                </th>
                <th style={{ padding: '6px 8px', textAlign: 'left', fontSize: 12, fontWeight: 700, width: '12%' }}>
                  MVA
                </th>
                <th style={{ padding: '6px 8px', textAlign: 'right', fontSize: 12, fontWeight: 700, width: '10%' }}>
                  Netto
                </th>
                <th style={{ padding: '6px 8px', textAlign: 'right', fontSize: 12, fontWeight: 700, width: '8%' }}>
                  MVA-beløp
                </th>
                <th style={{ padding: '6px 8px', textAlign: 'right', fontSize: 12, fontWeight: 700, width: '10%' }}>
                  Brutto
                </th>
                <th style={{ padding: '6px 8px', width: '3%' }}></th>
              </tr>
            </thead>
            <tbody>
              {linjer.map((linje, index) => {
                const beregnet = beregnedLinjer[index];
                return (
                  <tr key={linje.key} style={{ borderBottom: '1px solid #e8e8e8' }}>
                    <td style={{ padding: '4px 4px' }}>
                      <input
                        type="text"
                        value={linje.beskrivelse}
                        onChange={(e) => oppdaterLinje(index, { beskrivelse: e.target.value })}
                        placeholder="Vare/tjeneste"
                        required
                        style={{ ...inputStyle, padding: '4px 6px', fontSize: 13 }}
                      />
                    </td>
                    <td style={{ padding: '4px 4px' }}>
                      <input
                        type="number"
                        value={linje.antall}
                        onChange={(e) =>
                          oppdaterLinje(index, { antall: Number(e.target.value) })
                        }
                        min={0.01}
                        step="any"
                        required
                        style={{ ...inputStyle, padding: '4px 6px', fontSize: 13, textAlign: 'right' }}
                      />
                    </td>
                    <td style={{ padding: '4px 4px' }}>
                      <select
                        value={linje.enhet}
                        onChange={(e) =>
                          oppdaterLinje(index, { enhet: e.target.value as Enhet })
                        }
                        style={{ ...inputStyle, padding: '4px 6px', fontSize: 13 }}
                      >
                        {Object.entries(EnhetNavn).map(([key, label]) => (
                          <option key={key} value={key}>
                            {label}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td style={{ padding: '4px 4px' }}>
                      <input
                        type="number"
                        value={linje.enhetspris}
                        onChange={(e) =>
                          oppdaterLinje(index, { enhetspris: Number(e.target.value) })
                        }
                        min={0}
                        step="any"
                        required
                        style={{ ...inputStyle, padding: '4px 6px', fontSize: 13, textAlign: 'right' }}
                      />
                    </td>
                    <td style={{ padding: '4px 4px' }}>
                      <div style={{ display: 'flex', gap: 4 }}>
                        <select
                          value={linje.rabattType ?? ''}
                          onChange={(e) => {
                            const val = e.target.value;
                            if (!val) {
                              oppdaterLinje(index, {
                                rabattType: null,
                                rabattProsent: null,
                                rabattBelop: null,
                              });
                            } else {
                              oppdaterLinje(index, {
                                rabattType: val as RabattType,
                                rabattProsent: val === RabattType.Prosent ? 0 : null,
                                rabattBelop: val === RabattType.Belop ? 0 : null,
                              });
                            }
                          }}
                          style={{ ...inputStyle, padding: '4px 2px', fontSize: 12, width: '50%' }}
                        >
                          <option value="">Ingen</option>
                          <option value={RabattType.Prosent}>%</option>
                          <option value={RabattType.Belop}>kr</option>
                        </select>
                        {linje.rabattType === RabattType.Prosent && (
                          <input
                            type="number"
                            value={linje.rabattProsent ?? 0}
                            onChange={(e) =>
                              oppdaterLinje(index, { rabattProsent: Number(e.target.value) })
                            }
                            min={0}
                            max={100}
                            step="any"
                            style={{ ...inputStyle, padding: '4px 4px', fontSize: 12, width: '50%', textAlign: 'right' }}
                          />
                        )}
                        {linje.rabattType === RabattType.Belop && (
                          <input
                            type="number"
                            value={linje.rabattBelop ?? 0}
                            onChange={(e) =>
                              oppdaterLinje(index, { rabattBelop: Number(e.target.value) })
                            }
                            min={0}
                            step="any"
                            style={{ ...inputStyle, padding: '4px 4px', fontSize: 12, width: '50%', textAlign: 'right' }}
                          />
                        )}
                      </div>
                    </td>
                    <td style={{ padding: '4px 4px' }}>
                      <select
                        value={linje.mvaKode}
                        onChange={(e) => {
                          const kode = StandardMvaKoder.find(
                            (m) => m.kode === e.target.value,
                          );
                          oppdaterLinje(index, {
                            mvaKode: e.target.value,
                            mvaSats: kode?.sats ?? 25,
                          });
                        }}
                        style={{ ...inputStyle, padding: '4px 6px', fontSize: 12 }}
                      >
                        {StandardMvaKoder.map((m) => (
                          <option key={m.kode} value={m.kode}>
                            {m.sats} % - {m.beskrivelse}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td
                      style={{
                        padding: '4px 8px',
                        textAlign: 'right',
                        fontFamily: 'monospace',
                        fontSize: 13,
                      }}
                    >
                      {formatBelop(beregnet.nettobelop)}
                    </td>
                    <td
                      style={{
                        padding: '4px 8px',
                        textAlign: 'right',
                        fontFamily: 'monospace',
                        fontSize: 13,
                        color: '#666',
                      }}
                    >
                      {formatBelop(beregnet.mvaBelop)}
                    </td>
                    <td
                      style={{
                        padding: '4px 8px',
                        textAlign: 'right',
                        fontFamily: 'monospace',
                        fontSize: 13,
                        fontWeight: 600,
                      }}
                    >
                      {formatBelop(beregnet.bruttobelop)}
                    </td>
                    <td style={{ padding: '4px 4px', textAlign: 'center' }}>
                      {linjer.length > 1 && (
                        <button
                          type="button"
                          onClick={() => fjernLinje(index)}
                          title="Fjern linje"
                          style={{
                            background: 'none',
                            border: 'none',
                            color: '#c62828',
                            cursor: 'pointer',
                            fontSize: 16,
                            fontWeight: 700,
                            padding: '2px 6px',
                          }}
                        >
                          X
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        {/* Totalsektor og MVA-oversikt */}
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16, marginBottom: 24 }}>
          {/* MVA-spesifikasjon */}
          <div style={sectionStyle}>
            <h3 style={{ marginTop: 0, marginBottom: 8 }}>MVA-spesifikasjon</h3>
            <table style={{ width: '100%', borderCollapse: 'collapse' }}>
              <thead>
                <tr>
                  <th style={{ textAlign: 'left', padding: '4px 8px', fontSize: 12, borderBottom: '1px solid #ccc' }}>
                    Kode
                  </th>
                  <th style={{ textAlign: 'right', padding: '4px 8px', fontSize: 12, borderBottom: '1px solid #ccc' }}>
                    Sats
                  </th>
                  <th style={{ textAlign: 'right', padding: '4px 8px', fontSize: 12, borderBottom: '1px solid #ccc' }}>
                    Grunnlag
                  </th>
                  <th style={{ textAlign: 'right', padding: '4px 8px', fontSize: 12, borderBottom: '1px solid #ccc' }}>
                    MVA
                  </th>
                </tr>
              </thead>
              <tbody>
                {mvaOversikt.map((m) => (
                  <tr key={m.kode}>
                    <td style={{ padding: '4px 8px', fontSize: 13 }}>{m.kode}</td>
                    <td style={{ padding: '4px 8px', fontSize: 13, textAlign: 'right' }}>
                      {m.sats} %
                    </td>
                    <td style={{ padding: '4px 8px', fontSize: 13, textAlign: 'right', fontFamily: 'monospace' }}>
                      {formatBelop(m.grunnlag)}
                    </td>
                    <td style={{ padding: '4px 8px', fontSize: 13, textAlign: 'right', fontFamily: 'monospace' }}>
                      {formatBelop(m.mva)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Totaler */}
          <div
            style={{
              ...sectionStyle,
              backgroundColor: '#f0f4f8',
              borderColor: '#90caf9',
            }}
          >
            <h3 style={{ marginTop: 0, marginBottom: 12 }}>Totaler</h3>
            <table style={{ width: '100%' }}>
              <tbody>
                <tr>
                  <td style={{ padding: '6px 0', fontSize: 14 }}>Sum eks. MVA</td>
                  <td
                    style={{
                      padding: '6px 0',
                      textAlign: 'right',
                      fontFamily: 'monospace',
                      fontSize: 14,
                    }}
                  >
                    {formatBelop(totaler.eksMva)}
                  </td>
                </tr>
                <tr>
                  <td style={{ padding: '6px 0', fontSize: 14 }}>MVA</td>
                  <td
                    style={{
                      padding: '6px 0',
                      textAlign: 'right',
                      fontFamily: 'monospace',
                      fontSize: 14,
                    }}
                  >
                    {formatBelop(totaler.mva)}
                  </td>
                </tr>
                <tr style={{ borderTop: '2px solid #333' }}>
                  <td style={{ padding: '8px 0', fontSize: 16, fontWeight: 700 }}>
                    Totalt inkl. MVA
                  </td>
                  <td
                    style={{
                      padding: '8px 0',
                      textAlign: 'right',
                      fontFamily: 'monospace',
                      fontSize: 18,
                      fontWeight: 700,
                    }}
                  >
                    {formatBelop(totaler.inkMva)}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        {/* Handlinger */}
        <div style={{ display: 'flex', gap: 12 }}>
          <button
            type="submit"
            disabled={!valgtKunde || opprettFaktura.isPending}
            style={{
              ...btnPrimary,
              opacity: !valgtKunde || opprettFaktura.isPending ? 0.5 : 1,
            }}
          >
            {opprettFaktura.isPending ? 'Oppretter...' : 'Opprett faktura (utkast)'}
          </button>
          <button
            type="button"
            onClick={() => navigate('/faktura')}
            style={btnSecondary}
          >
            Avbryt
          </button>
        </div>

        {opprettFaktura.isError && (
          <p style={{ color: 'red', marginTop: 12 }}>
            Feil ved opprettelse av faktura. Sjekk at alle felt er riktig utfylt.
          </p>
        )}
      </form>
    </div>
  );
}
