import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useBankkontoer, useOpprettBankkonto } from '../../hooks/api/useBank';
import type { BankkontoDto, OpprettBankkontoRequest } from '../../types/bank';

export default function BankOversiktPage() {
  const { data: kontoer, isLoading, error } = useBankkontoer();
  const opprettKonto = useOpprettBankkonto();
  const [visNyForm, setVisNyForm] = useState(false);
  const [nyKonto, setNyKonto] = useState<OpprettBankkontoRequest>({
    kontonummer: '',
    banknavn: '',
    beskrivelse: '',
    hovedbokkkontoId: '',
  });

  function handleOpprett() {
    if (!nyKonto.kontonummer || !nyKonto.banknavn || !nyKonto.hovedbokkkontoId) return;
    opprettKonto.mutate(nyKonto, {
      onSuccess: () => {
        setVisNyForm(false);
        setNyKonto({ kontonummer: '', banknavn: '', beskrivelse: '', hovedbokkkontoId: '' });
      },
    });
  }

  function formatBankkonto(kontonummer: string): string {
    if (kontonummer.length === 11) {
      return `${kontonummer.slice(0, 4)}.${kontonummer.slice(4, 6)}.${kontonummer.slice(6)}`;
    }
    return kontonummer;
  }

  if (error) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil ved lasting av bankkontoer</h1>
        <p>Kunne ikke hente bankkontoer fra server.</p>
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
        <h1 style={{ margin: 0 }}>Bankkontoer</h1>
        <div style={{ display: 'flex', gap: 8 }}>
          <Link
            to="/bank/import"
            style={{
              padding: '8px 16px',
              background: '#0066cc',
              color: '#fff',
              borderRadius: 4,
              textDecoration: 'none',
              fontSize: 14,
            }}
          >
            Importer kontoutskrift
          </Link>
          <Link
            to="/bank/bevegelser"
            style={{
              padding: '8px 16px',
              background: '#e3f2fd',
              color: '#1565c0',
              borderRadius: 4,
              textDecoration: 'none',
              fontSize: 14,
            }}
          >
            Bankbevegelser
          </Link>
          <button
            onClick={() => setVisNyForm(!visNyForm)}
            style={{
              padding: '8px 16px',
              background: '#4caf50',
              color: '#fff',
              border: 'none',
              borderRadius: 4,
              fontSize: 14,
              cursor: 'pointer',
            }}
          >
            + Ny bankkonto
          </button>
        </div>
      </div>

      {visNyForm && (
        <div
          style={{
            padding: 16,
            border: '1px solid #e0e0e0',
            borderRadius: 8,
            marginBottom: 24,
            background: '#fafafa',
          }}
        >
          <h3 style={{ marginTop: 0 }}>Registrer ny bankkonto</h3>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
            <div>
              <label style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 13 }}>
                Kontonummer (11 siffer)
              </label>
              <input
                type="text"
                maxLength={11}
                value={nyKonto.kontonummer}
                onChange={(e) => setNyKonto({ ...nyKonto, kontonummer: e.target.value })}
                style={inputStyle}
                placeholder="12345678901"
              />
            </div>
            <div>
              <label style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 13 }}>
                Banknavn
              </label>
              <input
                type="text"
                value={nyKonto.banknavn}
                onChange={(e) => setNyKonto({ ...nyKonto, banknavn: e.target.value })}
                style={inputStyle}
                placeholder="DNB, Nordea, etc."
              />
            </div>
            <div>
              <label style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 13 }}>
                Beskrivelse
              </label>
              <input
                type="text"
                value={nyKonto.beskrivelse}
                onChange={(e) => setNyKonto({ ...nyKonto, beskrivelse: e.target.value })}
                style={inputStyle}
                placeholder="Driftskonto, Skattetrekk, etc."
              />
            </div>
            <div>
              <label style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 13 }}>
                Hovedbokkonto-ID (1920-serien)
              </label>
              <input
                type="text"
                value={nyKonto.hovedbokkkontoId}
                onChange={(e) => setNyKonto({ ...nyKonto, hovedbokkkontoId: e.target.value })}
                style={inputStyle}
                placeholder="UUID for hovedbokkonto"
              />
            </div>
            <div>
              <label style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 13 }}>
                IBAN (valgfritt)
              </label>
              <input
                type="text"
                value={nyKonto.iban ?? ''}
                onChange={(e) =>
                  setNyKonto({ ...nyKonto, iban: e.target.value || undefined })
                }
                style={inputStyle}
                placeholder="NO93 8601 1117 947"
              />
            </div>
            <div>
              <label style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 13 }}>
                BIC/SWIFT (valgfritt)
              </label>
              <input
                type="text"
                value={nyKonto.bic ?? ''}
                onChange={(e) =>
                  setNyKonto({ ...nyKonto, bic: e.target.value || undefined })
                }
                style={inputStyle}
                placeholder="DNBANOKKXXX"
              />
            </div>
          </div>
          <div style={{ marginTop: 16, display: 'flex', gap: 8 }}>
            <button
              onClick={handleOpprett}
              disabled={opprettKonto.isPending}
              style={{
                padding: '8px 24px',
                background: '#4caf50',
                color: '#fff',
                border: 'none',
                borderRadius: 4,
                fontSize: 14,
                cursor: 'pointer',
              }}
            >
              {opprettKonto.isPending ? 'Lagrer...' : 'Lagre'}
            </button>
            <button
              onClick={() => setVisNyForm(false)}
              style={{
                padding: '8px 24px',
                background: '#f5f5f5',
                color: '#333',
                border: '1px solid #ccc',
                borderRadius: 4,
                fontSize: 14,
                cursor: 'pointer',
              }}
            >
              Avbryt
            </button>
          </div>
          {opprettKonto.isError && (
            <p style={{ color: 'red', marginTop: 8, fontSize: 13 }}>
              Feil ved opprettelse av bankkonto. Sjekk at kontonummer er gyldig (11 siffer, MOD11).
            </p>
          )}
        </div>
      )}

      {isLoading ? (
        <p>Laster bankkontoer...</p>
      ) : !kontoer || kontoer.length === 0 ? (
        <div
          style={{
            padding: 32,
            textAlign: 'center',
            border: '1px solid #e0e0e0',
            borderRadius: 8,
          }}
        >
          <p style={{ color: '#666' }}>
            Ingen bankkontoer registrert. Klikk &quot;Ny bankkonto&quot; for a legge til en.
          </p>
        </div>
      ) : (
        <table
          style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}
        >
          <thead>
            <tr>
              <th style={thStyle}>Kontonummer</th>
              <th style={thStyle}>Bank</th>
              <th style={thStyle}>Beskrivelse</th>
              <th style={thStyle}>Hovedbokkonto</th>
              <th style={thStyle}>Valuta</th>
              <th style={thStyle}>Standard</th>
              <th style={thStyle}>Handlinger</th>
            </tr>
          </thead>
          <tbody>
            {kontoer.map((konto: BankkontoDto) => (
              <tr
                key={konto.id}
                style={{ backgroundColor: konto.erAktiv ? '#fff' : '#f5f5f5' }}
              >
                <td style={tdStyle}>
                  <strong style={{ fontFamily: 'monospace' }}>
                    {formatBankkonto(konto.kontonummer)}
                  </strong>
                </td>
                <td style={tdStyle}>{konto.banknavn}</td>
                <td style={tdStyle}>{konto.beskrivelse}</td>
                <td style={tdStyle}>
                  <span style={{ fontFamily: 'monospace' }}>{konto.hovedbokkontonummer}</span>
                </td>
                <td style={tdStyle}>{konto.valutakode}</td>
                <td style={tdStyle}>
                  {konto.erStandardInnbetaling && (
                    <span
                      style={{
                        padding: '2px 6px',
                        borderRadius: 12,
                        fontSize: 11,
                        background: '#e8f5e9',
                        color: '#2e7d32',
                        marginRight: 4,
                      }}
                    >
                      Inn
                    </span>
                  )}
                  {konto.erStandardUtbetaling && (
                    <span
                      style={{
                        padding: '2px 6px',
                        borderRadius: 12,
                        fontSize: 11,
                        background: '#ffebee',
                        color: '#c62828',
                      }}
                    >
                      Ut
                    </span>
                  )}
                </td>
                <td style={tdStyle}>
                  <div style={{ display: 'flex', gap: 8 }}>
                    <Link
                      to={`/bank/avstemming/${konto.id}`}
                      style={{
                        padding: '4px 12px',
                        background: '#fff3e0',
                        color: '#e65100',
                        borderRadius: 4,
                        textDecoration: 'none',
                        fontSize: 13,
                      }}
                    >
                      Avstemming
                    </Link>
                    <Link
                      to={`/bank/rapport/${konto.id}`}
                      style={{
                        padding: '4px 12px',
                        background: '#e3f2fd',
                        color: '#1565c0',
                        borderRadius: 4,
                        textDecoration: 'none',
                        fontSize: 13,
                      }}
                    >
                      Rapport
                    </Link>
                    <Link
                      to={`/bank/import?konto=${konto.id}`}
                      style={{
                        padding: '4px 12px',
                        background: '#f3e5f5',
                        color: '#7b1fa2',
                        borderRadius: 4,
                        textDecoration: 'none',
                        fontSize: 13,
                      }}
                    >
                      Importer
                    </Link>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

const inputStyle: React.CSSProperties = {
  width: '100%',
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  boxSizing: 'border-box',
};

const thStyle: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '2px solid #333',
  textAlign: 'left',
  fontWeight: 700,
  backgroundColor: '#f8f8f8',
  fontSize: 14,
};

const tdStyle: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '1px solid #e0e0e0',
  fontSize: 14,
};
