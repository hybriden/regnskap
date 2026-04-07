import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import {
  useFaktura,
  useUtstedeFaktura,
  useOpprettKreditnota,
  useGenererEhf,
  useGenererPdf,
  useLastNedEhf,
  useLastNedPdf,
  useKansellerFaktura,
} from '../../hooks/api/useFaktura';
import {
  FakturaStatus,
  FakturaStatusNavn,
  FakturaStatusFarge,
  FakturaLeveringsformatNavn,
  EnhetNavn,
} from '../../types/faktura';
import type { OpprettKreditnotaRequest } from '../../types/faktura';
import { formatBelop, formatDato } from '../../utils/formatering';

const sectionStyle: React.CSSProperties = {
  padding: 16,
  marginBottom: 16,
  border: '1px solid #e0e0e0',
  borderRadius: 4,
  backgroundColor: '#fafafa',
};

const cellStyle: React.CSSProperties = {
  padding: '6px 10px',
  borderBottom: '1px solid #e0e0e0',
  fontSize: 13,
};

const headerCellStyle: React.CSSProperties = {
  ...cellStyle,
  fontWeight: 700,
  borderBottom: '2px solid #333',
  backgroundColor: '#f8f8f8',
};

function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

export default function FakturaDetaljerPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: faktura, isLoading, error } = useFaktura(id ?? '');

  const utstedeFaktura = useUtstedeFaktura();
  const opprettKreditnota = useOpprettKreditnota();
  const genererEhf = useGenererEhf();
  const genererPdf = useGenererPdf();
  const lastNedEhf = useLastNedEhf();
  const lastNedPdf = useLastNedPdf();
  const kansellerFaktura = useKansellerFaktura();

  const [visKreditnotaSkjema, setVisKreditnotaSkjema] = useState(false);
  const [krediteringsaarsak, setKrediteringsaarsak] = useState('');
  const [fullKreditering, setFullKreditering] = useState(true);

  if (!id) return <p style={{ padding: 24 }}>Ugyldig faktura-ID.</p>;
  if (isLoading) return <p style={{ padding: 24 }}>Laster faktura...</p>;
  if (error || !faktura) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil ved lasting av faktura</h1>
        <p>Kunne ikke hente faktura.</p>
        <Link to="/faktura">Tilbake til liste</Link>
      </div>
    );
  }

  const erUtkast = faktura.status === FakturaStatus.Utkast;
  const erUtstedt = faktura.status === FakturaStatus.Utstedt;
  const statusFarge = FakturaStatusFarge[faktura.status as keyof typeof FakturaStatusFarge] ?? {
    bg: '#f5f5f5',
    color: '#333',
  };

  function handleUtstede() {
    if (!confirm('Er du sikker på at du vil utstede denne fakturaen? Den kan ikke redigeres etterpå.'))
      return;
    utstedeFaktura.mutate(id!);
  }

  function handleKanseller() {
    if (!confirm('Er du sikker på at du vil kansellere dette utkastet?')) return;
    kansellerFaktura.mutate(id!, {
      onSuccess: () => navigate('/faktura'),
    });
  }

  function handleOpprettKreditnota(e: React.FormEvent) {
    e.preventDefault();
    const request: OpprettKreditnotaRequest = {
      krediteringsaarsak,
      linjer: fullKreditering ? null : undefined,
    };
    opprettKreditnota.mutate(
      { id: id!, request },
      {
        onSuccess: (data) => {
          navigate(`/faktura/${data.id}`);
        },
      },
    );
  }

  function handleGenererEhf() {
    genererEhf.mutate(id!);
  }

  function handleGenererPdf() {
    genererPdf.mutate(id!);
  }

  function handleLastNedEhf() {
    lastNedEhf.mutate(id!, {
      onSuccess: (blob) => {
        downloadBlob(blob, `faktura-${faktura.fakturaId ?? id}.xml`);
      },
    });
  }

  function handleLastNedPdf() {
    lastNedPdf.mutate(id!, {
      onSuccess: (blob) => {
        downloadBlob(blob, `faktura-${faktura.fakturaId ?? id}.pdf`);
      },
    });
  }

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: 24,
        }}
      >
        <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
          <h1 style={{ margin: 0 }}>
            {faktura.dokumenttype === 'Kreditnota' ? 'Kreditnota' : 'Faktura'}{' '}
            {faktura.fakturaId ?? '(utkast)'}
          </h1>
          <span
            style={{
              padding: '4px 12px',
              borderRadius: 4,
              backgroundColor: statusFarge.bg,
              color: statusFarge.color,
              fontSize: 13,
              fontWeight: 600,
            }}
          >
            {FakturaStatusNavn[faktura.status as keyof typeof FakturaStatusNavn] ?? faktura.status}
          </span>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <Link
            to={`/faktura/${id}/forhandsvisning`}
            style={{
              padding: '8px 16px',
              background: '#0066cc',
              color: '#fff',
              borderRadius: 4,
              textDecoration: 'none',
              fontSize: 13,
            }}
          >
            Forhåndsvisning
          </Link>
          <Link
            to="/faktura"
            style={{
              padding: '8px 16px',
              background: '#f5f5f5',
              border: '1px solid #ccc',
              borderRadius: 4,
              textDecoration: 'none',
              color: '#333',
              fontSize: 13,
            }}
          >
            Tilbake
          </Link>
        </div>
      </div>

      {/* Handlingsknapper */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 20 }}>
        {erUtkast && (
          <>
            <button
              onClick={handleUtstede}
              disabled={utstedeFaktura.isPending}
              style={{
                padding: '8px 16px',
                background: '#2e7d32',
                color: '#fff',
                border: 'none',
                borderRadius: 4,
                cursor: 'pointer',
                fontSize: 13,
                fontWeight: 600,
              }}
            >
              {utstedeFaktura.isPending ? 'Utsteder...' : 'Utstede faktura'}
            </button>
            <button
              onClick={handleKanseller}
              disabled={kansellerFaktura.isPending}
              style={{
                padding: '8px 16px',
                background: '#ffebee',
                color: '#c62828',
                border: '1px solid #ef9a9a',
                borderRadius: 4,
                cursor: 'pointer',
                fontSize: 13,
              }}
            >
              Kanseller utkast
            </button>
          </>
        )}
        {erUtstedt && (
          <>
            <button
              onClick={() => setVisKreditnotaSkjema(!visKreditnotaSkjema)}
              style={{
                padding: '8px 16px',
                background: '#fce4ec',
                color: '#c62828',
                border: '1px solid #ef9a9a',
                borderRadius: 4,
                cursor: 'pointer',
                fontSize: 13,
              }}
            >
              Opprett kreditnota
            </button>
            {!faktura.ehfGenerert && (
              <button
                onClick={handleGenererEhf}
                disabled={genererEhf.isPending}
                style={{
                  padding: '8px 16px',
                  background: '#e3f2fd',
                  color: '#1565c0',
                  border: '1px solid #90caf9',
                  borderRadius: 4,
                  cursor: 'pointer',
                  fontSize: 13,
                }}
              >
                {genererEhf.isPending ? 'Genererer...' : 'Generer EHF'}
              </button>
            )}
            {faktura.ehfGenerert && (
              <button
                onClick={handleLastNedEhf}
                disabled={lastNedEhf.isPending}
                style={{
                  padding: '8px 16px',
                  background: '#e3f2fd',
                  color: '#1565c0',
                  border: '1px solid #90caf9',
                  borderRadius: 4,
                  cursor: 'pointer',
                  fontSize: 13,
                }}
              >
                Last ned EHF
              </button>
            )}
            <button
              onClick={faktura.pdfFilsti ? handleLastNedPdf : handleGenererPdf}
              disabled={genererPdf.isPending || lastNedPdf.isPending}
              style={{
                padding: '8px 16px',
                background: '#fff3e0',
                color: '#e65100',
                border: '1px solid #ffcc80',
                borderRadius: 4,
                cursor: 'pointer',
                fontSize: 13,
              }}
            >
              {faktura.pdfFilsti ? 'Last ned PDF' : 'Generer PDF'}
            </button>
          </>
        )}
      </div>

      {utstedeFaktura.isError && (
        <p style={{ color: 'red', marginBottom: 12 }}>
          Feil ved utstedelse. Sjekk at alle påkrevde felt er utfylt.
        </p>
      )}

      {/* Kreditnota-skjema */}
      {visKreditnotaSkjema && (
        <form onSubmit={handleOpprettKreditnota} style={{ ...sectionStyle, borderColor: '#ef9a9a' }}>
          <h3 style={{ marginTop: 0, color: '#c62828' }}>Opprett kreditnota</h3>
          <div style={{ marginBottom: 12 }}>
            <label style={{ display: 'block', fontWeight: 600, fontSize: 13, marginBottom: 4 }}>
              Årsak til kreditering *
            </label>
            <textarea
              value={krediteringsaarsak}
              onChange={(e) => setKrediteringsaarsak(e.target.value)}
              required
              maxLength={500}
              rows={2}
              style={{
                width: '100%',
                padding: '6px 10px',
                border: '1px solid #ccc',
                borderRadius: 4,
                fontSize: 14,
                boxSizing: 'border-box',
              }}
            />
          </div>
          <label
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 8,
              fontSize: 14,
              marginBottom: 12,
            }}
          >
            <input
              type="checkbox"
              checked={fullKreditering}
              onChange={(e) => setFullKreditering(e.target.checked)}
            />
            Full kreditering (alle linjer)
          </label>
          <div style={{ display: 'flex', gap: 8 }}>
            <button
              type="submit"
              disabled={opprettKreditnota.isPending}
              style={{
                padding: '8px 16px',
                background: '#c62828',
                color: '#fff',
                border: 'none',
                borderRadius: 4,
                cursor: 'pointer',
                fontSize: 13,
              }}
            >
              {opprettKreditnota.isPending ? 'Oppretter...' : 'Opprett kreditnota'}
            </button>
            <button
              type="button"
              onClick={() => setVisKreditnotaSkjema(false)}
              style={{
                padding: '8px 16px',
                background: '#f5f5f5',
                border: '1px solid #ccc',
                borderRadius: 4,
                cursor: 'pointer',
                fontSize: 13,
              }}
            >
              Avbryt
            </button>
          </div>
          {opprettKreditnota.isError && (
            <p style={{ color: 'red', marginTop: 8 }}>Feil ved opprettelse av kreditnota.</p>
          )}
        </form>
      )}

      {/* Fakturainformasjon */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        <div style={sectionStyle}>
          <h3 style={{ marginTop: 0, marginBottom: 12 }}>Fakturainformasjon</h3>
          <table style={{ width: '100%', fontSize: 14 }}>
            <tbody>
              <tr>
                <td style={{ padding: '4px 0', fontWeight: 600, width: '40%' }}>Fakturanummer</td>
                <td style={{ padding: '4px 0', fontFamily: 'monospace' }}>
                  {faktura.fakturaId ?? '(ikke tildelt)'}
                </td>
              </tr>
              <tr>
                <td style={{ padding: '4px 0', fontWeight: 600 }}>Type</td>
                <td style={{ padding: '4px 0' }}>{faktura.dokumenttype}</td>
              </tr>
              <tr>
                <td style={{ padding: '4px 0', fontWeight: 600 }}>Fakturadato</td>
                <td style={{ padding: '4px 0', fontFamily: 'monospace' }}>
                  {faktura.fakturadato ? formatDato(faktura.fakturadato) : '-'}
                </td>
              </tr>
              <tr>
                <td style={{ padding: '4px 0', fontWeight: 600 }}>Forfallsdato</td>
                <td style={{ padding: '4px 0', fontFamily: 'monospace' }}>
                  {faktura.forfallsdato ? formatDato(faktura.forfallsdato) : '-'}
                </td>
              </tr>
              <tr>
                <td style={{ padding: '4px 0', fontWeight: 600 }}>Leveringsdato</td>
                <td style={{ padding: '4px 0', fontFamily: 'monospace' }}>
                  {faktura.leveringsdato ? formatDato(faktura.leveringsdato) : '-'}
                </td>
              </tr>
              <tr>
                <td style={{ padding: '4px 0', fontWeight: 600 }}>Leveringsformat</td>
                <td style={{ padding: '4px 0' }}>
                  {FakturaLeveringsformatNavn[faktura.leveringsformat]}
                </td>
              </tr>
              <tr>
                <td style={{ padding: '4px 0', fontWeight: 600 }}>KID</td>
                <td style={{ padding: '4px 0', fontFamily: 'monospace' }}>
                  {faktura.kidNummer ?? '-'}
                </td>
              </tr>
              <tr>
                <td style={{ padding: '4px 0', fontWeight: 600 }}>Valuta</td>
                <td style={{ padding: '4px 0' }}>{faktura.valutakode}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div style={sectionStyle}>
          <h3 style={{ marginTop: 0, marginBottom: 12 }}>Kunde</h3>
          <table style={{ width: '100%', fontSize: 14 }}>
            <tbody>
              <tr>
                <td style={{ padding: '4px 0', fontWeight: 600, width: '40%' }}>Kunde</td>
                <td style={{ padding: '4px 0' }}>
                  <Link
                    to={`/kunde/${faktura.kundeId}`}
                    style={{ color: '#0066cc', textDecoration: 'none' }}
                  >
                    {faktura.kundeNavn}
                  </Link>
                  {faktura.kundenummer && (
                    <span style={{ color: '#888', marginLeft: 6 }}>({faktura.kundenummer})</span>
                  )}
                </td>
              </tr>
              <tr>
                <td style={{ padding: '4px 0', fontWeight: 600 }}>Kjøpers referanse</td>
                <td style={{ padding: '4px 0' }}>{faktura.kjopersReferanse ?? '-'}</td>
              </tr>
              <tr>
                <td style={{ padding: '4px 0', fontWeight: 600 }}>Vår referanse</td>
                <td style={{ padding: '4px 0' }}>{faktura.vaarReferanse ?? '-'}</td>
              </tr>
              <tr>
                <td style={{ padding: '4px 0', fontWeight: 600 }}>Bestillingsnummer</td>
                <td style={{ padding: '4px 0' }}>{faktura.bestillingsnummer ?? '-'}</td>
              </tr>
              {faktura.kreditertFakturaId && (
                <tr>
                  <td style={{ padding: '4px 0', fontWeight: 600 }}>Krediterer faktura</td>
                  <td style={{ padding: '4px 0' }}>
                    <Link
                      to={`/faktura/${faktura.kreditertFakturaId}`}
                      style={{ color: '#c62828', textDecoration: 'none' }}
                    >
                      Se originalfaktura
                    </Link>
                  </td>
                </tr>
              )}
              {faktura.krediteringsaarsak && (
                <tr>
                  <td style={{ padding: '4px 0', fontWeight: 600 }}>Årsak</td>
                  <td style={{ padding: '4px 0' }}>{faktura.krediteringsaarsak}</td>
                </tr>
              )}
            </tbody>
          </table>
          {faktura.merknad && (
            <div
              style={{
                marginTop: 12,
                padding: 8,
                backgroundColor: '#fff9c4',
                borderRadius: 4,
                fontSize: 13,
              }}
            >
              <strong>Merknad:</strong> {faktura.merknad}
            </div>
          )}
        </div>
      </div>

      {/* Fakturalinjer */}
      <div style={sectionStyle}>
        <h3 style={{ marginTop: 0, marginBottom: 12 }}>Fakturalinjer</h3>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr>
              <th style={{ ...headerCellStyle, textAlign: 'center', width: '5%' }}>Nr</th>
              <th style={{ ...headerCellStyle, width: '25%' }}>Beskrivelse</th>
              <th style={{ ...headerCellStyle, textAlign: 'right', width: '8%' }}>Antall</th>
              <th style={{ ...headerCellStyle, width: '6%' }}>Enhet</th>
              <th style={{ ...headerCellStyle, textAlign: 'right', width: '10%' }}>Enhetspris</th>
              <th style={{ ...headerCellStyle, textAlign: 'right', width: '8%' }}>Rabatt</th>
              <th style={{ ...headerCellStyle, textAlign: 'right', width: '10%' }}>Netto</th>
              <th style={{ ...headerCellStyle, textAlign: 'right', width: '6%' }}>MVA %</th>
              <th style={{ ...headerCellStyle, textAlign: 'right', width: '10%' }}>MVA</th>
              <th style={{ ...headerCellStyle, textAlign: 'right', width: '12%' }}>Brutto</th>
            </tr>
          </thead>
          <tbody>
            {faktura.linjer.map((linje) => (
              <tr key={linje.id}>
                <td style={{ ...cellStyle, textAlign: 'center' }}>{linje.linjenummer}</td>
                <td style={cellStyle}>{linje.beskrivelse}</td>
                <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {linje.antall}
                </td>
                <td style={cellStyle}>
                  {EnhetNavn[linje.enhet as keyof typeof EnhetNavn] ?? linje.enhet}
                </td>
                <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(linje.enhetspris)}
                </td>
                <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace', color: '#888' }}>
                  {linje.rabattBelop ? formatBelop(linje.rabattBelop) : '-'}
                  {linje.rabattType === 'Prosent' && linje.rabattProsent
                    ? ` (${linje.rabattProsent}%)`
                    : ''}
                </td>
                <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(linje.nettobelop)}
                </td>
                <td style={{ ...cellStyle, textAlign: 'right' }}>{linje.mvaSats} %</td>
                <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(linje.mvaBelop)}
                </td>
                <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace', fontWeight: 600 }}>
                  {formatBelop(linje.bruttobelop)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* MVA-sammendrag og Totaler */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        <div style={sectionStyle}>
          <h3 style={{ marginTop: 0, marginBottom: 8 }}>MVA-spesifikasjon</h3>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr>
                <th style={{ ...headerCellStyle, fontSize: 12 }}>Kode</th>
                <th style={{ ...headerCellStyle, fontSize: 12, textAlign: 'right' }}>Sats</th>
                <th style={{ ...headerCellStyle, fontSize: 12, textAlign: 'right' }}>Grunnlag</th>
                <th style={{ ...headerCellStyle, fontSize: 12, textAlign: 'right' }}>MVA</th>
              </tr>
            </thead>
            <tbody>
              {faktura.mvaLinjer.map((ml) => (
                <tr key={ml.mvaKode}>
                  <td style={cellStyle}>{ml.mvaKode}</td>
                  <td style={{ ...cellStyle, textAlign: 'right' }}>{ml.mvaSats} %</td>
                  <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                    {formatBelop(ml.grunnlag)}
                  </td>
                  <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                    {formatBelop(ml.mvaBelop)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

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
                  {formatBelop(faktura.belopEksMva)}
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
                  {formatBelop(faktura.mvaBelop)}
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
                    fontSize: 20,
                    fontWeight: 700,
                  }}
                >
                  {formatBelop(faktura.belopInklMva)}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
