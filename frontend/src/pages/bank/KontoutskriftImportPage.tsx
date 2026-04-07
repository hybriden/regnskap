import { useState, useRef } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import {
  useBankkontoer,
  useImporterKontoutskrift,
  useKontoutskrifter,
} from '../../hooks/api/useBank';
import { KontoutskriftStatusNavn } from '../../types/bank';
import type { ImportKontoutskriftResponse, KontoutskriftDto } from '../../types/bank';
import { formatBelop, formatDato } from '../../utils/formatering';

export default function KontoutskriftImportPage() {
  const [searchParams] = useSearchParams();
  const preselectedKonto = searchParams.get('konto') ?? '';

  const [valgtKontoId, setValgtKontoId] = useState(preselectedKonto);
  const [importResultat, setImportResultat] = useState<ImportKontoutskriftResponse | null>(null);
  const [importFeil, setImportFeil] = useState<string | null>(null);
  const filInputRef = useRef<HTMLInputElement>(null);

  const { data: kontoer, isLoading: lasterKontoer } = useBankkontoer();
  const { data: utskrifter, isLoading: lasterUtskrifter } = useKontoutskrifter(valgtKontoId);
  const importerMutation = useImporterKontoutskrift();

  function handleImport() {
    const fil = filInputRef.current?.files?.[0];
    if (!fil || !valgtKontoId) return;

    setImportResultat(null);
    setImportFeil(null);

    importerMutation.mutate(
      { bankkontoId: valgtKontoId, fil },
      {
        onSuccess: (data) => {
          setImportResultat(data);
          if (filInputRef.current) filInputRef.current.value = '';
        },
        onError: (error) => {
          const msg =
            (error as { response?: { data?: { melding?: string } } })?.response?.data?.melding ??
            'Ukjent feil ved import. Sjekk at filen er gyldig CAMT.053 XML.';
          setImportFeil(msg);
        },
      },
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ marginBottom: 24 }}>
        <Link to="/bank" style={{ color: '#0066cc', textDecoration: 'none', fontSize: 14 }}>
          &larr; Tilbake til bankkontoer
        </Link>
      </div>

      <h1 style={{ marginBottom: 24 }}>Importer kontoutskrift (CAMT.053)</h1>

      {/* Import-skjema */}
      <div
        style={{
          padding: 24,
          border: '1px solid #e0e0e0',
          borderRadius: 8,
          marginBottom: 24,
          background: '#fafafa',
        }}
      >
        <div style={{ display: 'flex', gap: 16, alignItems: 'flex-end', flexWrap: 'wrap' }}>
          <div style={{ flex: 1, minWidth: 200 }}>
            <label style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 13 }}>
              Velg bankkonto
            </label>
            {lasterKontoer ? (
              <p style={{ fontSize: 13, color: '#666' }}>Laster kontoer...</p>
            ) : (
              <select
                value={valgtKontoId}
                onChange={(e) => setValgtKontoId(e.target.value)}
                style={{
                  width: '100%',
                  padding: '8px 12px',
                  border: '1px solid #ccc',
                  borderRadius: 4,
                  fontSize: 14,
                }}
              >
                <option value="">-- Velg bankkonto --</option>
                {kontoer?.map((k) => (
                  <option key={k.id} value={k.id}>
                    {k.kontonummer} - {k.beskrivelse} ({k.banknavn})
                  </option>
                ))}
              </select>
            )}
          </div>
          <div style={{ flex: 1, minWidth: 200 }}>
            <label style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 13 }}>
              CAMT.053-fil (XML)
            </label>
            <input
              type="file"
              accept=".xml,.XML"
              ref={filInputRef}
              style={{ fontSize: 14 }}
            />
          </div>
          <button
            onClick={handleImport}
            disabled={!valgtKontoId || importerMutation.isPending}
            style={{
              padding: '8px 24px',
              background: !valgtKontoId ? '#ccc' : '#0066cc',
              color: '#fff',
              border: 'none',
              borderRadius: 4,
              fontSize: 14,
              cursor: !valgtKontoId ? 'not-allowed' : 'pointer',
              height: 38,
            }}
          >
            {importerMutation.isPending ? 'Importerer...' : 'Importer'}
          </button>
        </div>

        {importFeil && (
          <div
            style={{
              marginTop: 16,
              padding: 12,
              background: '#ffebee',
              color: '#c62828',
              borderRadius: 4,
              fontSize: 13,
            }}
          >
            {importFeil}
          </div>
        )}

        {importResultat && (
          <div
            style={{
              marginTop: 16,
              padding: 16,
              background: '#e8f5e9',
              borderRadius: 4,
              border: '1px solid #c8e6c9',
            }}
          >
            <h3 style={{ marginTop: 0, color: '#2e7d32' }}>Import fullfort</h3>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 12 }}>
              <div>
                <div style={{ fontSize: 12, color: '#666' }}>Periode</div>
                <div style={{ fontWeight: 600 }}>
                  {formatDato(importResultat.periodeFra)} &ndash;{' '}
                  {formatDato(importResultat.periodeTil)}
                </div>
              </div>
              <div>
                <div style={{ fontSize: 12, color: '#666' }}>Inngaende saldo</div>
                <div style={{ fontWeight: 600, fontFamily: 'monospace' }}>
                  {formatBelop(importResultat.inngaendeSaldo)}
                </div>
              </div>
              <div>
                <div style={{ fontSize: 12, color: '#666' }}>Utgaende saldo</div>
                <div style={{ fontWeight: 600, fontFamily: 'monospace' }}>
                  {formatBelop(importResultat.utgaendeSaldo)}
                </div>
              </div>
              <div>
                <div style={{ fontSize: 12, color: '#666' }}>Antall bevegelser</div>
                <div style={{ fontWeight: 600 }}>{importResultat.antallBevegelser}</div>
              </div>
              <div>
                <div style={{ fontSize: 12, color: '#666' }}>Auto-matchet</div>
                <div style={{ fontWeight: 600, color: '#2e7d32' }}>
                  {importResultat.antallAutoMatchet}
                </div>
              </div>
              <div>
                <div style={{ fontSize: 12, color: '#666' }}>Ikke matchet</div>
                <div
                  style={{
                    fontWeight: 600,
                    color: importResultat.antallIkkeMatchet > 0 ? '#e65100' : '#2e7d32',
                  }}
                >
                  {importResultat.antallIkkeMatchet}
                </div>
              </div>
            </div>
            {importResultat.antallIkkeMatchet > 0 && (
              <div style={{ marginTop: 12 }}>
                <Link
                  to={`/bank/avstemming/${valgtKontoId}`}
                  style={{ color: '#0066cc', fontSize: 13 }}
                >
                  Ga til avstemming for a matche gjenstaaende bevegelser &rarr;
                </Link>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Tidligere importerte kontoutskrifter */}
      {valgtKontoId && (
        <div>
          <h2 style={{ marginBottom: 16 }}>Importerte kontoutskrifter</h2>
          {lasterUtskrifter ? (
            <p>Laster kontoutskrifter...</p>
          ) : !utskrifter || utskrifter.length === 0 ? (
            <p style={{ color: '#666' }}>Ingen kontoutskrifter importert for denne kontoen.</p>
          ) : (
            <table
              style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}
            >
              <thead>
                <tr>
                  <th style={thStyle}>Meldings-ID</th>
                  <th style={thStyle}>Periode</th>
                  <th style={thStyle}>Inng. saldo</th>
                  <th style={thStyle}>Utg. saldo</th>
                  <th style={thStyle}>Bevegelser</th>
                  <th style={thStyle}>Status</th>
                </tr>
              </thead>
              <tbody>
                {utskrifter.map((u: KontoutskriftDto) => (
                  <tr key={u.id}>
                    <td style={tdStyle}>
                      <span style={{ fontFamily: 'monospace', fontSize: 12 }}>
                        {u.meldingsId}
                      </span>
                    </td>
                    <td style={tdStyle}>
                      {formatDato(u.periodeFra)} &ndash; {formatDato(u.periodeTil)}
                    </td>
                    <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                      {formatBelop(u.inngaendeSaldo)}
                    </td>
                    <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                      {formatBelop(u.utgaendeSaldo)}
                    </td>
                    <td style={{ ...tdStyle, textAlign: 'center' }}>{u.antallBevegelser}</td>
                    <td style={tdStyle}>
                      <span
                        style={{
                          padding: '2px 8px',
                          borderRadius: 12,
                          fontSize: 12,
                          fontWeight: 600,
                          ...kontoutskriftStatusFarge(u.status),
                        }}
                      >
                        {KontoutskriftStatusNavn[u.status]}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </div>
  );
}

function kontoutskriftStatusFarge(
  status: string,
): { backgroundColor: string; color: string } {
  switch (status) {
    case 'Ferdig':
      return { backgroundColor: '#e8f5e9', color: '#2e7d32' };
    case 'DelvisBehandlet':
      return { backgroundColor: '#fff3e0', color: '#e65100' };
    default:
      return { backgroundColor: '#e3f2fd', color: '#1565c0' };
  }
}

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
