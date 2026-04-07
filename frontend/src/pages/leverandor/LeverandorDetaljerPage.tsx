import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useLeverandor, useLeverandorutskrift, useFakturaSok } from '../../hooks/api/useLeverandor';
import { formatBelop, formatDato } from '../../utils/formatering';
import {
  BetalingsbetingelseNavn,
  FakturaStatusNavn,
} from '../../types/leverandor';
import type { FakturaSokParams } from '../../types/leverandor';
import RegnskapsTabell from '../../components/RegnskapsTabell';
import type { RegnskapsLinje } from '../../components/RegnskapsTabell';

const currentYear = new Date().getFullYear();

export default function LeverandorDetaljerPage() {
  const { id } = useParams<{ id: string }>();
  const leverandorId = id ?? '';

  const { data: leverandor, isLoading: lasterLev, error: levError } = useLeverandor(leverandorId);

  // Utskrift-parametere
  const [fraDato, setFraDato] = useState(`${currentYear}-01-01`);
  const [tilDato, setTilDato] = useState(`${currentYear}-12-31`);
  const { data: utskrift, isLoading: lasterUtskrift } = useLeverandorutskrift(
    leverandorId,
    fraDato,
    tilDato,
  );

  // Fakturahistorikk
  const [fakturaSok, setFakturaSok] = useState<FakturaSokParams>({
    leverandorId,
    side: 1,
    antall: 20,
  });
  const { data: fakturaResultat } = useFakturaSok(fakturaSok);

  if (lasterLev) return <div style={{ padding: 24 }}>Laster leverandor...</div>;
  if (levError)
    return (
      <div style={{ padding: 24, color: 'red' }}>
        Feil ved henting: {(levError as Error).message}
      </div>
    );
  if (!leverandor) return <div style={{ padding: 24 }}>Leverandor ikke funnet</div>;

  // Bygg utskrift-linjer for RegnskapsTabell
  const utskriftLinjer: RegnskapsLinje[] =
    utskrift?.transaksjoner.map((t) => ({
      dato: formatDato(t.dato),
      referanse: t.bilagsId,
      beskrivelse: `${t.beskrivelse}${t.eksternFakturanummer ? ` (${t.eksternFakturanummer})` : ''}`,
      debet: t.debet ?? 0,
      kredit: t.kredit ?? 0,
      saldo: t.saldo,
    })) ?? [];

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      {/* Breadcrumb */}
      <div style={{ marginBottom: 16, fontSize: 13, color: '#666' }}>
        <Link to="/leverandor" style={{ color: '#1565c0' }}>
          Leverandorer
        </Link>{' '}
        / {leverandor.leverandornummer} {leverandor.navn}
      </div>

      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: 24 }}>
        <div>
          <h1 style={{ margin: '0 0 4px' }}>{leverandor.navn}</h1>
          <span style={{ fontSize: 14, color: '#666' }}>
            Leverandornr: {leverandor.leverandornummer}
            {leverandor.organisasjonsnummer && ` | Org.nr: ${leverandor.organisasjonsnummer}`}
          </span>
        </div>
        <div style={{ textAlign: 'right' }}>
          <div style={{ fontSize: 13, color: '#666' }}>Saldo</div>
          <div
            style={{
              fontSize: 24,
              fontWeight: 700,
              fontFamily: 'monospace',
              color: leverandor.saldo < 0 ? 'red' : 'inherit',
            }}
          >
            {formatBelop(leverandor.saldo)}
          </div>
          <div style={{ marginTop: 8, display: 'flex', gap: 6 }}>
            <span
              style={{
                padding: '2px 8px',
                borderRadius: 12,
                fontSize: 12,
                fontWeight: 600,
                backgroundColor: leverandor.erAktiv ? '#e8f5e9' : '#ffebee',
                color: leverandor.erAktiv ? '#2e7d32' : '#c62828',
              }}
            >
              {leverandor.erAktiv ? 'Aktiv' : 'Inaktiv'}
            </span>
            {leverandor.erSperret && (
              <span
                style={{
                  padding: '2px 8px',
                  borderRadius: 12,
                  fontSize: 12,
                  fontWeight: 600,
                  backgroundColor: '#fff3e0',
                  color: '#e65100',
                }}
              >
                Sperret
              </span>
            )}
            {leverandor.erMvaRegistrert && (
              <span
                style={{
                  padding: '2px 8px',
                  borderRadius: 12,
                  fontSize: 12,
                  fontWeight: 600,
                  backgroundColor: '#e3f2fd',
                  color: '#1565c0',
                }}
              >
                MVA-registrert
              </span>
            )}
          </div>
        </div>
      </div>

      {/* Grunndata i 2 kolonner */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24, marginBottom: 32 }}>
        <div style={kortStil}>
          <h3 style={{ marginTop: 0 }}>Kontaktinformasjon</h3>
          <InfoRad label="Adresse" verdi={[leverandor.adresse1, leverandor.adresse2, `${leverandor.postnummer ?? ''} ${leverandor.poststed ?? ''}`].filter(Boolean).join(', ') || '-'} />
          <InfoRad label="Land" verdi={leverandor.landkode} />
          <InfoRad label="Kontaktperson" verdi={leverandor.kontaktperson ?? '-'} />
          <InfoRad label="E-post" verdi={leverandor.epost ?? '-'} />
          <InfoRad label="Telefon" verdi={leverandor.telefon ?? '-'} />
        </div>
        <div style={kortStil}>
          <h3 style={{ marginTop: 0 }}>Betalingsinformasjon</h3>
          <InfoRad label="Betingelse" verdi={BetalingsbetingelseNavn[leverandor.betalingsbetingelse]} />
          {leverandor.egendefinertBetalingsfrist && (
            <InfoRad label="Betalingsfrist" verdi={`${leverandor.egendefinertBetalingsfrist} dager`} />
          )}
          <InfoRad label="Bankkonto" verdi={leverandor.bankkontonummer ?? '-'} />
          <InfoRad label="IBAN" verdi={leverandor.iban ?? '-'} />
          <InfoRad label="BIC" verdi={leverandor.bic ?? '-'} />
          <InfoRad label="Valuta" verdi={leverandor.valutakode} />
          <InfoRad label="Standard MVA-kode" verdi={leverandor.standardMvaKode ?? '-'} />
        </div>
      </div>

      {leverandor.notat && (
        <div style={{ ...kortStil, marginBottom: 24 }}>
          <h3 style={{ marginTop: 0 }}>Notat</h3>
          <p style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{leverandor.notat}</p>
        </div>
      )}

      {/* Leverandorutskrift */}
      <div style={{ marginBottom: 32 }}>
        <h2>Leverandorutskrift</h2>
        <div style={{ display: 'flex', gap: 12, marginBottom: 16, alignItems: 'flex-end' }}>
          <div>
            <label style={labelStil}>Fra dato</label>
            <input
              type="date"
              value={fraDato}
              onChange={(e) => setFraDato(e.target.value)}
              style={inputStil}
            />
          </div>
          <div>
            <label style={labelStil}>Til dato</label>
            <input
              type="date"
              value={tilDato}
              onChange={(e) => setTilDato(e.target.value)}
              style={inputStil}
            />
          </div>
        </div>
        {lasterUtskrift ? (
          <p>Laster utskrift...</p>
        ) : utskrift ? (
          <RegnskapsTabell
            linjer={utskriftLinjer}
            visSaldo
            visDato
            visReferanse
            visSum
            inngaendeBalanse={utskrift.inngaaendeSaldo}
            tittel={`Periode ${formatDato(utskrift.fraDato)} - ${formatDato(utskrift.tilDato)}`}
          />
        ) : null}
      </div>

      {/* Fakturahistorikk */}
      <div>
        <h2>Fakturahistorikk</h2>
        <table style={tabellStil}>
          <thead>
            <tr>
              <th style={headerCelleStil}>Int.nr</th>
              <th style={{ ...headerCelleStil, textAlign: 'left' }}>Ekst.fakturanr</th>
              <th style={headerCelleStil}>Type</th>
              <th style={headerCelleStil}>Fakturadato</th>
              <th style={headerCelleStil}>Forfall</th>
              <th style={{ ...headerCelleStil, textAlign: 'left' }}>Beskrivelse</th>
              <th style={headerCelleStil}>Belop inkl. MVA</th>
              <th style={headerCelleStil}>Gjenstaaende</th>
              <th style={headerCelleStil}>Status</th>
            </tr>
          </thead>
          <tbody>
            {fakturaResultat?.data.map((f) => (
              <tr key={f.id}>
                <td style={{ ...celleStil, fontFamily: 'monospace' }}>{f.internNummer}</td>
                <td style={{ ...celleStil, textAlign: 'left' }}>{f.eksternFakturanummer}</td>
                <td style={celleStil}>{f.type}</td>
                <td style={celleStil}>{formatDato(f.fakturadato)}</td>
                <td style={celleStil}>{formatDato(f.forfallsdato)}</td>
                <td style={{ ...celleStil, textAlign: 'left' }}>{f.beskrivelse}</td>
                <td style={{ ...celleStil, fontFamily: 'monospace' }}>{formatBelop(f.belopInklMva)}</td>
                <td
                  style={{
                    ...celleStil,
                    fontFamily: 'monospace',
                    color: f.gjenstaendeBelop > 0 ? 'inherit' : '#2e7d32',
                  }}
                >
                  {formatBelop(f.gjenstaendeBelop)}
                </td>
                <td style={celleStil}>
                  <FakturaStatusBadge status={f.status} />
                </td>
              </tr>
            ))}
            {fakturaResultat?.data.length === 0 && (
              <tr>
                <td colSpan={9} style={{ ...celleStil, textAlign: 'center', color: '#666' }}>
                  Ingen fakturaer registrert
                </td>
              </tr>
            )}
          </tbody>
        </table>
        {(fakturaResultat?.totaltAntall ?? 0) > (fakturaSok.antall ?? 20) && (
          <div style={{ display: 'flex', justifyContent: 'center', gap: 8, marginTop: 12 }}>
            <button
              onClick={() => setFakturaSok((p) => ({ ...p, side: (p.side ?? 1) - 1 }))}
              disabled={(fakturaSok.side ?? 1) <= 1}
              style={pagKnappStil}
            >
              Forrige
            </button>
            <span style={{ padding: '6px 12px', fontSize: 13 }}>
              Side {fakturaSok.side ?? 1} av{' '}
              {Math.ceil((fakturaResultat?.totaltAntall ?? 0) / (fakturaSok.antall ?? 20))}
            </span>
            <button
              onClick={() => setFakturaSok((p) => ({ ...p, side: (p.side ?? 1) + 1 }))}
              disabled={
                (fakturaSok.side ?? 1) >=
                Math.ceil((fakturaResultat?.totaltAntall ?? 0) / (fakturaSok.antall ?? 20))
              }
              style={pagKnappStil}
            >
              Neste
            </button>
          </div>
        )}
      </div>
    </div>
  );
}

// --- Hjelpekomponenter ---

function InfoRad({ label, verdi }: { label: string; verdi: string }) {
  return (
    <div style={{ display: 'flex', padding: '4px 0', borderBottom: '1px solid #f0f0f0' }}>
      <span style={{ width: 160, fontWeight: 600, fontSize: 13, color: '#666' }}>{label}</span>
      <span style={{ fontSize: 14 }}>{verdi}</span>
    </div>
  );
}

function FakturaStatusBadge({ status }: { status: string }) {
  const farger: Record<string, { bg: string; color: string }> = {
    Registrert: { bg: '#fff3e0', color: '#e65100' },
    Godkjent: { bg: '#e8f5e9', color: '#2e7d32' },
    IBetalingsforslag: { bg: '#e3f2fd', color: '#1565c0' },
    SendtTilBank: { bg: '#e3f2fd', color: '#1565c0' },
    Betalt: { bg: '#e8f5e9', color: '#1b5e20' },
    DelvisBetalt: { bg: '#fff8e1', color: '#f57f17' },
    Kreditert: { bg: '#fce4ec', color: '#c62828' },
    Sperret: { bg: '#ffebee', color: '#b71c1c' },
  };
  const farge = farger[status] ?? { bg: '#f5f5f5', color: '#333' };
  const navn = FakturaStatusNavn[status as keyof typeof FakturaStatusNavn] ?? status;

  return (
    <span
      style={{
        padding: '2px 8px',
        borderRadius: 12,
        fontSize: 12,
        fontWeight: 600,
        backgroundColor: farge.bg,
        color: farge.color,
        whiteSpace: 'nowrap',
      }}
    >
      {navn}
    </span>
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
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
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

const pagKnappStil: React.CSSProperties = {
  padding: '6px 16px',
  border: '1px solid #ccc',
  borderRadius: 4,
  backgroundColor: '#fff',
  cursor: 'pointer',
  fontSize: 13,
};
