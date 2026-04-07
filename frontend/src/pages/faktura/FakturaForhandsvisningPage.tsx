import { useParams, Link } from 'react-router-dom';
import { useFaktura } from '../../hooks/api/useFaktura';
import { EnhetNavn, FakturaLeveringsformatNavn } from '../../types/faktura';
import { formatBelop, formatDato } from '../../utils/formatering';

/**
 * PDF-style forhåndsvisning av faktura.
 * Simulerer et A4-ark med fakturaens layout.
 */
export default function FakturaForhandsvisningPage() {
  const { id } = useParams<{ id: string }>();
  const { data: faktura, isLoading, error } = useFaktura(id ?? '');

  if (!id) return <p style={{ padding: 24 }}>Ugyldig faktura-ID.</p>;
  if (isLoading) return <p style={{ padding: 24 }}>Laster forhåndsvisning...</p>;
  if (error || !faktura) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil ved lasting</h1>
        <Link to="/faktura">Tilbake</Link>
      </div>
    );
  }

  const erKreditnota = faktura.dokumenttype === 'Kreditnota';

  return (
    <div style={{ padding: 24, backgroundColor: '#e0e0e0', minHeight: '100vh' }}>
      {/* Toppbar */}
      <div
        style={{
          maxWidth: 800,
          margin: '0 auto 16px',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
        }}
      >
        <span style={{ fontWeight: 600, color: '#333' }}>
          Forhåndsvisning - {erKreditnota ? 'Kreditnota' : 'Faktura'}{' '}
          {faktura.fakturaId ?? '(utkast)'}
        </span>
        <div style={{ display: 'flex', gap: 8 }}>
          <Link
            to={`/faktura/${id}`}
            style={{
              padding: '6px 14px',
              background: '#fff',
              border: '1px solid #ccc',
              borderRadius: 4,
              textDecoration: 'none',
              color: '#333',
              fontSize: 13,
            }}
          >
            Tilbake til detaljer
          </Link>
          <button
            onClick={() => window.print()}
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
            Skriv ut
          </button>
        </div>
      </div>

      {/* A4-ark */}
      <div
        style={{
          maxWidth: 800,
          margin: '0 auto',
          backgroundColor: '#fff',
          padding: '48px 56px',
          boxShadow: '0 2px 8px rgba(0,0,0,0.2)',
          fontFamily: 'Georgia, "Times New Roman", serif',
          fontSize: 13,
          lineHeight: 1.5,
          color: '#222',
        }}
      >
        {/* Toppsektor: Firma + Fakturatittel */}
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            marginBottom: 32,
            paddingBottom: 16,
            borderBottom: '2px solid #333',
          }}
        >
          <div>
            <div
              style={{
                fontSize: 22,
                fontWeight: 700,
                color: erKreditnota ? '#c62828' : '#1a237e',
                marginBottom: 4,
              }}
            >
              {erKreditnota ? 'KREDITNOTA' : 'FAKTURA'}
            </div>
            <div style={{ fontSize: 11, color: '#666' }}>
              Ditt Firma AS
              <br />
              Org.nr: 999 999 999 MVA
            </div>
          </div>
          <div style={{ textAlign: 'right' }}>
            <div style={{ fontSize: 16, fontWeight: 700, fontFamily: 'monospace' }}>
              {faktura.fakturaId ?? 'UTKAST'}
            </div>
            <div style={{ fontSize: 11, color: '#666', marginTop: 4 }}>
              {faktura.fakturadato ? formatDato(faktura.fakturadato) : 'Dato ikke satt'}
            </div>
          </div>
        </div>

        {/* Kundeinfo + Fakturadetaljer */}
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: '1fr 1fr',
            gap: 32,
            marginBottom: 32,
          }}
        >
          <div>
            <div style={{ fontSize: 10, color: '#888', textTransform: 'uppercase', marginBottom: 4 }}>
              Faktureres til
            </div>
            <div style={{ fontWeight: 700, fontSize: 14 }}>{faktura.kundeNavn}</div>
            {faktura.kundenummer && (
              <div style={{ fontSize: 11, color: '#666' }}>
                Kundenr: {faktura.kundenummer}
              </div>
            )}
          </div>
          <div>
            <table style={{ width: '100%', fontSize: 12 }}>
              <tbody>
                <tr>
                  <td style={{ padding: '2px 0', color: '#888' }}>Fakturadato</td>
                  <td style={{ padding: '2px 0', textAlign: 'right', fontFamily: 'monospace' }}>
                    {faktura.fakturadato ? formatDato(faktura.fakturadato) : '-'}
                  </td>
                </tr>
                <tr>
                  <td style={{ padding: '2px 0', color: '#888' }}>Forfallsdato</td>
                  <td style={{ padding: '2px 0', textAlign: 'right', fontFamily: 'monospace' }}>
                    {faktura.forfallsdato ? formatDato(faktura.forfallsdato) : '-'}
                  </td>
                </tr>
                {faktura.leveringsdato && (
                  <tr>
                    <td style={{ padding: '2px 0', color: '#888' }}>Leveringsdato</td>
                    <td style={{ padding: '2px 0', textAlign: 'right', fontFamily: 'monospace' }}>
                      {formatDato(faktura.leveringsdato)}
                    </td>
                  </tr>
                )}
                {faktura.kidNummer && (
                  <tr>
                    <td style={{ padding: '2px 0', color: '#888' }}>KID</td>
                    <td style={{ padding: '2px 0', textAlign: 'right', fontFamily: 'monospace' }}>
                      {faktura.kidNummer}
                    </td>
                  </tr>
                )}
                {faktura.bestillingsnummer && (
                  <tr>
                    <td style={{ padding: '2px 0', color: '#888' }}>Bestilling</td>
                    <td style={{ padding: '2px 0', textAlign: 'right' }}>
                      {faktura.bestillingsnummer}
                    </td>
                  </tr>
                )}
                {faktura.kjopersReferanse && (
                  <tr>
                    <td style={{ padding: '2px 0', color: '#888' }}>Deres ref.</td>
                    <td style={{ padding: '2px 0', textAlign: 'right' }}>
                      {faktura.kjopersReferanse}
                    </td>
                  </tr>
                )}
                {faktura.vaarReferanse && (
                  <tr>
                    <td style={{ padding: '2px 0', color: '#888' }}>Vår ref.</td>
                    <td style={{ padding: '2px 0', textAlign: 'right' }}>
                      {faktura.vaarReferanse}
                    </td>
                  </tr>
                )}
                <tr>
                  <td style={{ padding: '2px 0', color: '#888' }}>Leveringsformat</td>
                  <td style={{ padding: '2px 0', textAlign: 'right' }}>
                    {FakturaLeveringsformatNavn[faktura.leveringsformat]}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        {faktura.kreditertFakturaId && (
          <div
            style={{
              padding: '8px 12px',
              backgroundColor: '#fce4ec',
              borderRadius: 4,
              marginBottom: 16,
              fontSize: 12,
            }}
          >
            Krediterer faktura: {faktura.kreditertFakturaId}
            {faktura.krediteringsaarsak && (
              <span> - Årsak: {faktura.krediteringsaarsak}</span>
            )}
          </div>
        )}

        {/* Linjer */}
        <table
          style={{
            width: '100%',
            borderCollapse: 'collapse',
            marginBottom: 24,
          }}
        >
          <thead>
            <tr style={{ borderBottom: '2px solid #333' }}>
              <th style={{ padding: '6px 4px', textAlign: 'left', fontSize: 10, textTransform: 'uppercase', color: '#666' }}>
                Beskrivelse
              </th>
              <th style={{ padding: '6px 4px', textAlign: 'right', fontSize: 10, textTransform: 'uppercase', color: '#666' }}>
                Antall
              </th>
              <th style={{ padding: '6px 4px', textAlign: 'left', fontSize: 10, textTransform: 'uppercase', color: '#666' }}>
                Enhet
              </th>
              <th style={{ padding: '6px 4px', textAlign: 'right', fontSize: 10, textTransform: 'uppercase', color: '#666' }}>
                Pris
              </th>
              <th style={{ padding: '6px 4px', textAlign: 'right', fontSize: 10, textTransform: 'uppercase', color: '#666' }}>
                Rabatt
              </th>
              <th style={{ padding: '6px 4px', textAlign: 'right', fontSize: 10, textTransform: 'uppercase', color: '#666' }}>
                Netto
              </th>
              <th style={{ padding: '6px 4px', textAlign: 'right', fontSize: 10, textTransform: 'uppercase', color: '#666' }}>
                MVA
              </th>
              <th style={{ padding: '6px 4px', textAlign: 'right', fontSize: 10, textTransform: 'uppercase', color: '#666' }}>
                Beløp
              </th>
            </tr>
          </thead>
          <tbody>
            {faktura.linjer.map((linje, index) => (
              <tr
                key={linje.id}
                style={{
                  borderBottom: '1px solid #e0e0e0',
                  backgroundColor: index % 2 === 0 ? '#fff' : '#f9f9f9',
                }}
              >
                <td style={{ padding: '6px 4px' }}>{linje.beskrivelse}</td>
                <td style={{ padding: '6px 4px', textAlign: 'right', fontFamily: 'monospace' }}>
                  {linje.antall}
                </td>
                <td style={{ padding: '6px 4px', fontSize: 11 }}>
                  {EnhetNavn[linje.enhet as keyof typeof EnhetNavn] ?? linje.enhet}
                </td>
                <td style={{ padding: '6px 4px', textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(linje.enhetspris)}
                </td>
                <td style={{ padding: '6px 4px', textAlign: 'right', fontFamily: 'monospace', color: '#888' }}>
                  {linje.rabattBelop ? formatBelop(linje.rabattBelop) : ''}
                </td>
                <td style={{ padding: '6px 4px', textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(linje.nettobelop)}
                </td>
                <td style={{ padding: '6px 4px', textAlign: 'right', fontFamily: 'monospace', fontSize: 11 }}>
                  {linje.mvaSats} %
                </td>
                <td style={{ padding: '6px 4px', textAlign: 'right', fontFamily: 'monospace', fontWeight: 600 }}>
                  {formatBelop(linje.bruttobelop)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {/* Totaler og MVA */}
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 32 }}>
          {/* MVA-spesifikasjon */}
          <div>
            <div style={{ fontSize: 10, color: '#888', textTransform: 'uppercase', marginBottom: 4 }}>
              MVA-spesifikasjon
            </div>
            <table style={{ width: '100%', fontSize: 12 }}>
              <thead>
                <tr style={{ borderBottom: '1px solid #ccc' }}>
                  <th style={{ textAlign: 'left', padding: '2px 4px', fontSize: 10, color: '#888' }}>Sats</th>
                  <th style={{ textAlign: 'right', padding: '2px 4px', fontSize: 10, color: '#888' }}>Grunnlag</th>
                  <th style={{ textAlign: 'right', padding: '2px 4px', fontSize: 10, color: '#888' }}>MVA</th>
                </tr>
              </thead>
              <tbody>
                {faktura.mvaLinjer.map((ml) => (
                  <tr key={ml.mvaKode} style={{ borderBottom: '1px solid #eee' }}>
                    <td style={{ padding: '2px 4px' }}>{ml.mvaSats} %</td>
                    <td style={{ padding: '2px 4px', textAlign: 'right', fontFamily: 'monospace' }}>
                      {formatBelop(ml.grunnlag)}
                    </td>
                    <td style={{ padding: '2px 4px', textAlign: 'right', fontFamily: 'monospace' }}>
                      {formatBelop(ml.mvaBelop)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Totaler */}
          <div>
            <table style={{ width: '100%', fontSize: 13 }}>
              <tbody>
                <tr>
                  <td style={{ padding: '4px 0' }}>Sum eks. MVA</td>
                  <td style={{ padding: '4px 0', textAlign: 'right', fontFamily: 'monospace' }}>
                    {formatBelop(faktura.belopEksMva)}
                  </td>
                </tr>
                <tr>
                  <td style={{ padding: '4px 0' }}>MVA</td>
                  <td style={{ padding: '4px 0', textAlign: 'right', fontFamily: 'monospace' }}>
                    {formatBelop(faktura.mvaBelop)}
                  </td>
                </tr>
                <tr
                  style={{
                    borderTop: '2px solid #333',
                  }}
                >
                  <td
                    style={{
                      padding: '8px 0',
                      fontSize: 16,
                      fontWeight: 700,
                    }}
                  >
                    {erKreditnota ? 'Krediteres' : 'Å betale'}
                  </td>
                  <td
                    style={{
                      padding: '8px 0',
                      textAlign: 'right',
                      fontFamily: 'monospace',
                      fontSize: 18,
                      fontWeight: 700,
                      color: erKreditnota ? '#c62828' : '#1a237e',
                    }}
                  >
                    {faktura.valutakode} {formatBelop(faktura.belopInklMva)}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        {/* Betalingsinformasjon */}
        {!erKreditnota && (
          <div
            style={{
              marginTop: 32,
              padding: '12px 16px',
              backgroundColor: '#f5f5f5',
              borderRadius: 4,
              fontSize: 12,
            }}
          >
            <div style={{ fontWeight: 700, marginBottom: 4 }}>Betalingsinformasjon</div>
            <div>
              Beløp: <strong>{formatBelop(faktura.belopInklMva)}</strong> |{' '}
              KID: <strong>{faktura.kidNummer ?? '(genereres ved utstedelse)'}</strong> |{' '}
              Forfallsdato:{' '}
              <strong>
                {faktura.forfallsdato ? formatDato(faktura.forfallsdato) : '(settes ved utstedelse)'}
              </strong>
            </div>
          </div>
        )}

        {/* Merknad */}
        {faktura.merknad && (
          <div style={{ marginTop: 16, fontSize: 12, color: '#666', fontStyle: 'italic' }}>
            {faktura.merknad}
          </div>
        )}

        {/* Bunntekst */}
        <div
          style={{
            marginTop: 40,
            paddingTop: 12,
            borderTop: '1px solid #ccc',
            fontSize: 10,
            color: '#888',
            textAlign: 'center',
          }}
        >
          Ditt Firma AS | Org.nr: 999 999 999 MVA | Foretaksregisteret
        </div>
      </div>
    </div>
  );
}
