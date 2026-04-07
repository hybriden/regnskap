import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  useKunde,
  useKundeSaldo,
  useKundeFakturaer,
  useKundeutskrift,
} from '../../hooks/api/useKunde';
import {
  KundeFakturaStatusNavn,
  KundeBetalingsbetingelseNavn,
  KundeTransaksjonTypeNavn,
} from '../../types/kunde';
import type { KundeFakturaDto, KundeFakturaStatus } from '../../types/kunde';
import { formatBelop, formatDato } from '../../utils/formatering';
import RegnskapsTabell from '../../components/RegnskapsTabell';
import type { RegnskapsLinje } from '../../components/RegnskapsTabell';

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

function statusFarge(status: KundeFakturaStatus): { backgroundColor: string; color: string } {
  switch (status) {
    case 'Utstedt':
      return { backgroundColor: '#e3f2fd', color: '#1565c0' };
    case 'Betalt':
      return { backgroundColor: '#e8f5e9', color: '#2e7d32' };
    case 'DelvisBetalt':
      return { backgroundColor: '#fff3e0', color: '#e65100' };
    case 'Kreditert':
      return { backgroundColor: '#f5f5f5', color: '#616161' };
    case 'Purring1':
    case 'Purring2':
    case 'Purring3':
      return { backgroundColor: '#fff8e1', color: '#f57f17' };
    case 'Inkasso':
      return { backgroundColor: '#fce4ec', color: '#c62828' };
    case 'Tap':
      return { backgroundColor: '#ffebee', color: '#b71c1c' };
    default:
      return { backgroundColor: '#f5f5f5', color: '#616161' };
  }
}

export default function KundeDetaljerPage() {
  const { id } = useParams<{ id: string }>();
  const kundeId = id ?? '';

  const { data: kunde, isLoading: kundeLoading, error: kundeError } = useKunde(kundeId);
  const { data: saldo } = useKundeSaldo(kundeId);
  const { data: fakturaer } = useKundeFakturaer({ kundeId, pageSize: 100 });

  // Utskrift-periode
  const currentYear = new Date().getFullYear();
  const [fraDato, setFraDato] = useState(`${currentYear}-01-01`);
  const [tilDato, setTilDato] = useState(`${currentYear}-12-31`);
  const [visUtskrift, setVisUtskrift] = useState(false);
  const { data: utskrift } = useKundeutskrift(
    visUtskrift ? kundeId : '',
    fraDato,
    tilDato,
  );

  if (kundeError) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil</h1>
        <p>Kunne ikke hente kundedetaljer.</p>
        <Link to="/kunde">Tilbake til kundeliste</Link>
      </div>
    );
  }

  if (kundeLoading || !kunde) {
    return <div style={{ padding: 24 }}>Laster kundedetaljer...</div>;
  }

  const utskriftLinjer: RegnskapsLinje[] = (utskrift?.transaksjoner ?? []).map((t) => ({
    id: t.bilagsId,
    dato: formatDato(t.dato),
    referanse: t.fakturanummer ? `Fakt. ${t.fakturanummer}` : t.bilagsId,
    beskrivelse: `${KundeTransaksjonTypeNavn[t.type]} - ${t.beskrivelse}`,
    debet: t.debet ?? 0,
    kredit: t.kredit ?? 0,
    saldo: t.saldo,
  }));

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ marginBottom: 16 }}>
        <Link to="/kunde" style={{ color: '#0066cc', textDecoration: 'none', fontSize: 14 }}>
          Tilbake til kundeliste
        </Link>
      </div>

      {/* Kundeinfo */}
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
            {kunde.kundenummer} - {kunde.navn}
          </h1>
          <div style={{ marginTop: 8, color: '#666', fontSize: 14 }}>
            {kunde.erBedrift ? 'Bedrift' : 'Privatperson'}
            {kunde.organisasjonsnummer && ` | Org.nr: ${kunde.organisasjonsnummer}`}
            {kunde.epost && ` | ${kunde.epost}`}
            {kunde.telefon && ` | ${kunde.telefon}`}
          </div>
          {kunde.adresse1 && (
            <div style={{ marginTop: 4, color: '#666', fontSize: 14 }}>
              {kunde.adresse1}
              {kunde.adresse2 && `, ${kunde.adresse2}`}
              {kunde.postnummer && ` - ${kunde.postnummer}`}
              {kunde.poststed && ` ${kunde.poststed}`}
            </div>
          )}
        </div>
        <div style={{ textAlign: 'right' }}>
          {kunde.erSperret && (
            <div
              style={{
                padding: '4px 12px',
                backgroundColor: '#ffebee',
                color: '#c62828',
                borderRadius: 4,
                marginBottom: 8,
                fontWeight: 600,
              }}
            >
              Kunde er sperret
            </div>
          )}
          {saldo && (
            <div style={{ fontSize: 24, fontWeight: 700, fontFamily: 'monospace' }}>
              {formatBelop(saldo.saldo)}
            </div>
          )}
          <div style={{ fontSize: 13, color: '#666' }}>
            {saldo ? `${saldo.antallApnePoster} åpne poster` : ''}
          </div>
        </div>
      </div>

      {/* Kundeopplysninger */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '1fr 1fr 1fr',
          gap: 16,
          marginBottom: 24,
          padding: 16,
          border: '1px solid #e0e0e0',
          borderRadius: 4,
          backgroundColor: '#fafafa',
        }}
      >
        <div>
          <div style={{ fontWeight: 600, fontSize: 13, marginBottom: 2 }}>Betalingsbetingelse</div>
          <div>{KundeBetalingsbetingelseNavn[kunde.betalingsbetingelse]}</div>
        </div>
        <div>
          <div style={{ fontWeight: 600, fontSize: 13, marginBottom: 2 }}>Kredittgrense</div>
          <div>{kunde.kredittgrense > 0 ? formatBelop(kunde.kredittgrense) : 'Ingen'}</div>
        </div>
        <div>
          <div style={{ fontWeight: 600, fontSize: 13, marginBottom: 2 }}>Valuta</div>
          <div>{kunde.valutakode}</div>
        </div>
        <div>
          <div style={{ fontWeight: 600, fontSize: 13, marginBottom: 2 }}>Kontaktperson</div>
          <div>{kunde.kontaktperson ?? '-'}</div>
        </div>
        <div>
          <div style={{ fontWeight: 600, fontSize: 13, marginBottom: 2 }}>EHF</div>
          <div>{kunde.kanMottaEhf ? 'Ja' : 'Nei'}</div>
        </div>
        <div>
          <div style={{ fontWeight: 600, fontSize: 13, marginBottom: 2 }}>KID-algoritme</div>
          <div>{kunde.kidAlgoritme ?? 'Systemstandard'}</div>
        </div>
        {kunde.notat && (
          <div style={{ gridColumn: '1 / -1' }}>
            <div style={{ fontWeight: 600, fontSize: 13, marginBottom: 2 }}>Notat</div>
            <div>{kunde.notat}</div>
          </div>
        )}
      </div>

      {/* Fakturaer */}
      <h2 style={{ marginBottom: 12 }}>Fakturaer</h2>
      <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0', marginBottom: 24 }}>
        <thead>
          <tr>
            <th style={headerStyle}>Fakturanr</th>
            <th style={headerStyle}>Type</th>
            <th style={headerStyle}>Dato</th>
            <th style={headerStyle}>Forfall</th>
            <th style={headerStyle}>Beskrivelse</th>
            <th style={{ ...headerStyle, textAlign: 'right' }}>Bel&oslash;p inkl. MVA</th>
            <th style={{ ...headerStyle, textAlign: 'right' }}>Gjenstående</th>
            <th style={{ ...headerStyle, textAlign: 'center' }}>Status</th>
          </tr>
        </thead>
        <tbody>
          {(fakturaer?.items ?? []).length === 0 && (
            <tr>
              <td colSpan={8} style={{ ...cellStyle, textAlign: 'center', fontStyle: 'italic' }}>
                Ingen fakturaer registrert
              </td>
            </tr>
          )}
          {(fakturaer?.items ?? []).map((f: KundeFakturaDto, index: number) => (
            <tr key={f.id} style={{ backgroundColor: index % 2 === 0 ? '#fff' : '#fafafa' }}>
              <td style={{ ...cellStyle, fontFamily: 'monospace' }}>{f.fakturanummer}</td>
              <td style={cellStyle}>{KundeTransaksjonTypeNavn[f.type]}</td>
              <td style={cellStyle}>{formatDato(f.fakturadato)}</td>
              <td
                style={{
                  ...cellStyle,
                  color: f.erForfalt ? '#c62828' : 'inherit',
                  fontWeight: f.erForfalt ? 600 : 400,
                }}
              >
                {formatDato(f.forfallsdato)}
                {f.erForfalt && ` (${f.dagerForfalt}d)`}
              </td>
              <td style={cellStyle}>{f.beskrivelse}</td>
              <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                {formatBelop(f.belopInklMva)}
              </td>
              <td
                style={{
                  ...cellStyle,
                  textAlign: 'right',
                  fontFamily: 'monospace',
                  fontWeight: f.gjenstaendeBelop > 0 ? 600 : 400,
                  color: f.gjenstaendeBelop > 0 ? '#c62828' : '#2e7d32',
                }}
              >
                {formatBelop(f.gjenstaendeBelop)}
              </td>
              <td style={{ ...cellStyle, textAlign: 'center' }}>
                <span
                  style={{
                    ...statusFarge(f.status),
                    padding: '2px 8px',
                    borderRadius: 4,
                    fontSize: 12,
                  }}
                >
                  {KundeFakturaStatusNavn[f.status]}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {/* Kundeutskrift */}
      <h2 style={{ marginBottom: 12 }}>Kundeutskrift</h2>
      <div style={{ display: 'flex', gap: 12, alignItems: 'center', marginBottom: 16 }}>
        <div>
          <label style={{ fontSize: 13, fontWeight: 600, marginRight: 4 }}>Fra:</label>
          <input
            type="date"
            value={fraDato}
            onChange={(e) => setFraDato(e.target.value)}
            style={{ padding: '6px 10px', border: '1px solid #ccc', borderRadius: 4 }}
          />
        </div>
        <div>
          <label style={{ fontSize: 13, fontWeight: 600, marginRight: 4 }}>Til:</label>
          <input
            type="date"
            value={tilDato}
            onChange={(e) => setTilDato(e.target.value)}
            style={{ padding: '6px 10px', border: '1px solid #ccc', borderRadius: 4 }}
          />
        </div>
        <button
          onClick={() => setVisUtskrift(true)}
          style={{
            padding: '8px 16px',
            background: '#0066cc',
            color: '#fff',
            border: 'none',
            borderRadius: 4,
            cursor: 'pointer',
          }}
        >
          Vis utskrift
        </button>
      </div>

      {visUtskrift && utskrift && (
        <RegnskapsTabell
          tittel={`Kundeutskrift ${kunde.kundenummer} - ${kunde.navn}`}
          linjer={utskriftLinjer}
          visDato
          visReferanse
          visSaldo
          visSum
          inngaendeBalanse={utskrift.inngaaendeSaldo}
        />
      )}
    </div>
  );
}
