import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useKunder, useSokKunder, useOpprettKunde } from '../../hooks/api/useKunde';
import {
  KundeBetalingsbetingelseNavn,
  KundeBetalingsbetingelse,
} from '../../types/kunde';
import type { KundeDto, OpprettKundeRequest } from '../../types/kunde';
import { formatBelop } from '../../utils/formatering';

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

function statusBadge(kunde: KundeDto) {
  if (kunde.erSperret)
    return (
      <span
        style={{
          padding: '2px 8px',
          borderRadius: 4,
          backgroundColor: '#ffebee',
          color: '#c62828',
          fontSize: 12,
        }}
      >
        Sperret
      </span>
    );
  if (!kunde.erAktiv)
    return (
      <span
        style={{
          padding: '2px 8px',
          borderRadius: 4,
          backgroundColor: '#f5f5f5',
          color: '#616161',
          fontSize: 12,
        }}
      >
        Inaktiv
      </span>
    );
  return (
    <span
      style={{
        padding: '2px 8px',
        borderRadius: 4,
        backgroundColor: '#e8f5e9',
        color: '#2e7d32',
        fontSize: 12,
      }}
    >
      Aktiv
    </span>
  );
}

const tomtSkjema: OpprettKundeRequest = {
  kundenummer: '',
  navn: '',
  erBedrift: true,
  landkode: 'NO',
  betalingsbetingelse: KundeBetalingsbetingelse.Netto14,
  kanMottaEhf: false,
};

export default function KundeListePage() {
  const [page, setPage] = useState(1);
  const [sok, setSok] = useState('');
  const [visNyKunde, setVisNyKunde] = useState(false);
  const [skjema, setSkjema] = useState<OpprettKundeRequest>({ ...tomtSkjema });

  const { data: paginert, isLoading, error } = useKunder({ page, pageSize: 50 });
  const { data: sokResultat } = useSokKunder(sok);
  const opprettKunde = useOpprettKunde();

  const kunder = sok.length >= 2 ? sokResultat ?? [] : paginert?.items ?? [];

  function handleOpprett(e: React.FormEvent) {
    e.preventDefault();
    opprettKunde.mutate(skjema, {
      onSuccess: () => {
        setVisNyKunde(false);
        setSkjema({ ...tomtSkjema });
      },
    });
  }

  if (error) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil ved lasting av kunder</h1>
        <p>Kunne ikke hente kunder fra server.</p>
      </div>
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: 24,
        }}
      >
        <h1 style={{ margin: 0 }}>Kundereskontro</h1>
        <div style={{ display: 'flex', gap: 8 }}>
          <Link
            to="/kunde/apne-poster"
            style={{
              padding: '8px 16px',
              background: '#0066cc',
              color: '#fff',
              borderRadius: 4,
              textDecoration: 'none',
              fontSize: 14,
            }}
          >
            Åpne poster
          </Link>
          <Link
            to="/kunde/aldersfordeling"
            style={{
              padding: '8px 16px',
              background: '#0066cc',
              color: '#fff',
              borderRadius: 4,
              textDecoration: 'none',
              fontSize: 14,
            }}
          >
            Aldersfordeling
          </Link>
          <Link
            to="/kunde/innbetaling"
            style={{
              padding: '8px 16px',
              background: '#0066cc',
              color: '#fff',
              borderRadius: 4,
              textDecoration: 'none',
              fontSize: 14,
            }}
          >
            Innbetaling
          </Link>
          <Link
            to="/kunde/purring"
            style={{
              padding: '8px 16px',
              background: '#0066cc',
              color: '#fff',
              borderRadius: 4,
              textDecoration: 'none',
              fontSize: 14,
            }}
          >
            Purring
          </Link>
          <button
            onClick={() => setVisNyKunde(!visNyKunde)}
            style={{
              padding: '8px 16px',
              background: '#2e7d32',
              color: '#fff',
              border: 'none',
              borderRadius: 4,
              cursor: 'pointer',
              fontSize: 14,
            }}
          >
            + Ny kunde
          </button>
        </div>
      </div>

      {/* Sok */}
      <div style={{ marginBottom: 16 }}>
        <input
          type="text"
          placeholder="Søk etter kunde (navn, kundenummer, org.nr)..."
          value={sok}
          onChange={(e) => setSok(e.target.value)}
          style={{
            width: '100%',
            padding: '10px 14px',
            border: '1px solid #ccc',
            borderRadius: 4,
            fontSize: 14,
          }}
        />
      </div>

      {/* Ny kunde-skjema */}
      {visNyKunde && (
        <form
          onSubmit={handleOpprett}
          style={{
            padding: 16,
            marginBottom: 16,
            border: '1px solid #e0e0e0',
            borderRadius: 4,
            backgroundColor: '#fafafa',
          }}
        >
          <h3 style={{ marginTop: 0 }}>Ny kunde</h3>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12 }}>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                Kundenummer *
              </label>
              <input
                type="text"
                value={skjema.kundenummer}
                onChange={(e) => setSkjema({ ...skjema, kundenummer: e.target.value })}
                required
                maxLength={20}
                style={{ width: '100%', padding: '6px 10px', border: '1px solid #ccc', borderRadius: 4 }}
              />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                Navn *
              </label>
              <input
                type="text"
                value={skjema.navn}
                onChange={(e) => setSkjema({ ...skjema, navn: e.target.value })}
                required
                maxLength={200}
                style={{ width: '100%', padding: '6px 10px', border: '1px solid #ccc', borderRadius: 4 }}
              />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                Type
              </label>
              <select
                value={skjema.erBedrift ? 'bedrift' : 'privat'}
                onChange={(e) => setSkjema({ ...skjema, erBedrift: e.target.value === 'bedrift' })}
                style={{ width: '100%', padding: '6px 10px', border: '1px solid #ccc', borderRadius: 4 }}
              >
                <option value="bedrift">Bedrift</option>
                <option value="privat">Privatperson</option>
              </select>
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                Org.nr
              </label>
              <input
                type="text"
                value={skjema.organisasjonsnummer ?? ''}
                onChange={(e) =>
                  setSkjema({ ...skjema, organisasjonsnummer: e.target.value || null })
                }
                maxLength={9}
                pattern="\d{9}"
                style={{ width: '100%', padding: '6px 10px', border: '1px solid #ccc', borderRadius: 4 }}
              />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                E-post
              </label>
              <input
                type="email"
                value={skjema.epost ?? ''}
                onChange={(e) => setSkjema({ ...skjema, epost: e.target.value || null })}
                style={{ width: '100%', padding: '6px 10px', border: '1px solid #ccc', borderRadius: 4 }}
              />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                Telefon
              </label>
              <input
                type="text"
                value={skjema.telefon ?? ''}
                onChange={(e) => setSkjema({ ...skjema, telefon: e.target.value || null })}
                style={{ width: '100%', padding: '6px 10px', border: '1px solid #ccc', borderRadius: 4 }}
              />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                Betalingsbetingelse
              </label>
              <select
                value={skjema.betalingsbetingelse}
                onChange={(e) =>
                  setSkjema({
                    ...skjema,
                    betalingsbetingelse: e.target.value as KundeBetalingsbetingelse,
                  })
                }
                style={{ width: '100%', padding: '6px 10px', border: '1px solid #ccc', borderRadius: 4 }}
              >
                {Object.entries(KundeBetalingsbetingelseNavn).map(([key, label]) => (
                  <option key={key} value={key}>
                    {label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13 }}>
                Kredittgrense
              </label>
              <input
                type="number"
                value={skjema.kredittgrense ?? 0}
                onChange={(e) =>
                  setSkjema({ ...skjema, kredittgrense: Number(e.target.value) })
                }
                min={0}
                style={{ width: '100%', padding: '6px 10px', border: '1px solid #ccc', borderRadius: 4 }}
              />
            </div>
            <div style={{ display: 'flex', alignItems: 'end' }}>
              <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 13 }}>
                <input
                  type="checkbox"
                  checked={skjema.kanMottaEhf}
                  onChange={(e) => setSkjema({ ...skjema, kanMottaEhf: e.target.checked })}
                />
                Kan motta EHF
              </label>
            </div>
          </div>
          <div style={{ marginTop: 12, display: 'flex', gap: 8 }}>
            <button
              type="submit"
              disabled={opprettKunde.isPending}
              style={{
                padding: '8px 20px',
                background: '#2e7d32',
                color: '#fff',
                border: 'none',
                borderRadius: 4,
                cursor: 'pointer',
              }}
            >
              {opprettKunde.isPending ? 'Oppretter...' : 'Opprett kunde'}
            </button>
            <button
              type="button"
              onClick={() => setVisNyKunde(false)}
              style={{
                padding: '8px 20px',
                background: '#f5f5f5',
                border: '1px solid #ccc',
                borderRadius: 4,
                cursor: 'pointer',
              }}
            >
              Avbryt
            </button>
          </div>
          {opprettKunde.isError && (
            <p style={{ color: 'red', marginTop: 8 }}>
              Feil ved opprettelse av kunde. Sjekk at alle felt er riktig utfylt.
            </p>
          )}
        </form>
      )}

      {/* Kundeliste */}
      {isLoading ? (
        <p>Laster kunder...</p>
      ) : (
        <>
          <table
            style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}
          >
            <thead>
              <tr>
                <th style={headerStyle}>Kundenr</th>
                <th style={headerStyle}>Navn</th>
                <th style={headerStyle}>Org.nr</th>
                <th style={headerStyle}>Sted</th>
                <th style={headerStyle}>Betingelse</th>
                <th style={{ ...headerStyle, textAlign: 'right' }}>Kredittgrense</th>
                <th style={{ ...headerStyle, textAlign: 'center' }}>Status</th>
              </tr>
            </thead>
            <tbody>
              {kunder.length === 0 && (
                <tr>
                  <td colSpan={7} style={{ ...cellStyle, textAlign: 'center', fontStyle: 'italic' }}>
                    {sok.length >= 2 ? 'Ingen kunder funnet' : 'Ingen kunder registrert'}
                  </td>
                </tr>
              )}
              {kunder.map((kunde, index) => (
                <tr
                  key={kunde.id}
                  style={{ backgroundColor: index % 2 === 0 ? '#fff' : '#fafafa' }}
                >
                  <td style={cellStyle}>
                    <Link
                      to={`/kunde/${kunde.id}`}
                      style={{ color: '#0066cc', textDecoration: 'none' }}
                    >
                      {kunde.kundenummer}
                    </Link>
                  </td>
                  <td style={cellStyle}>
                    <Link
                      to={`/kunde/${kunde.id}`}
                      style={{ color: '#0066cc', textDecoration: 'none' }}
                    >
                      {kunde.navn}
                    </Link>
                  </td>
                  <td style={{ ...cellStyle, fontFamily: 'monospace' }}>
                    {kunde.organisasjonsnummer ?? ''}
                  </td>
                  <td style={cellStyle}>
                    {kunde.poststed
                      ? `${kunde.postnummer ?? ''} ${kunde.poststed}`
                      : ''}
                  </td>
                  <td style={cellStyle}>
                    {KundeBetalingsbetingelseNavn[kunde.betalingsbetingelse]}
                  </td>
                  <td style={{ ...cellStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                    {kunde.kredittgrense > 0 ? formatBelop(kunde.kredittgrense) : ''}
                  </td>
                  <td style={{ ...cellStyle, textAlign: 'center' }}>{statusBadge(kunde)}</td>
                </tr>
              ))}
            </tbody>
          </table>

          {/* Paginering */}
          {paginert && paginert.totalPages > 1 && sok.length < 2 && (
            <div
              style={{
                marginTop: 16,
                display: 'flex',
                justifyContent: 'center',
                gap: 8,
                alignItems: 'center',
              }}
            >
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                style={{
                  padding: '6px 12px',
                  border: '1px solid #ccc',
                  borderRadius: 4,
                  cursor: page <= 1 ? 'default' : 'pointer',
                  opacity: page <= 1 ? 0.5 : 1,
                }}
              >
                Forrige
              </button>
              <span style={{ fontSize: 14 }}>
                Side {paginert.page} av {paginert.totalPages} ({paginert.totalCount} kunder)
              </span>
              <button
                onClick={() => setPage((p) => Math.min(paginert.totalPages, p + 1))}
                disabled={page >= paginert.totalPages}
                style={{
                  padding: '6px 12px',
                  border: '1px solid #ccc',
                  borderRadius: 4,
                  cursor: page >= paginert.totalPages ? 'default' : 'pointer',
                  opacity: page >= paginert.totalPages ? 0.5 : 1,
                }}
              >
                Neste
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
