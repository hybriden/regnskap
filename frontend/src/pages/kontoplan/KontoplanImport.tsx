import { useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useImporterKontoplan, useEksporterKontoplan } from '../../hooks/api/useKontoplan';
import type { ImportFormat, ImportModus, ImportResultatDto, EksportFormat } from '../../types/kontoplan';

export default function KontoplanImport() {
  const navigate = useNavigate();
  const filInputRef = useRef<HTMLInputElement>(null);

  const [valgtFil, setValgtFil] = useState<File | null>(null);
  const [format, setFormat] = useState<ImportFormat>('csv');
  const [modus, setModus] = useState<ImportModus>('opprett');
  const [resultat, setResultat] = useState<ImportResultatDto | null>(null);

  const importer = useImporterKontoplan();
  const eksporter = useEksporterKontoplan();

  async function handleImport() {
    if (!valgtFil) return;
    const result = await importer.mutateAsync({ fil: valgtFil, format, modus });
    setResultat(result);
  }

  async function handleEksport(eksportFormat: EksportFormat) {
    const blob = await eksporter.mutateAsync({ format: eksportFormat, inkluderInaktive: false });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    const ext = eksportFormat === 'saft' ? 'xml' : eksportFormat;
    a.download = `kontoplan.${ext}`;
    a.click();
    URL.revokeObjectURL(url);
  }

  return (
    <div style={{ padding: 24, maxWidth: 800, margin: '0 auto' }}>
      <button
        onClick={() => navigate('/kontoplan')}
        style={{ background: 'none', border: 'none', color: '#1a73e8', cursor: 'pointer', padding: 0, fontSize: 14, marginBottom: 8 }}
      >
        &larr; Tilbake til kontoplan
      </button>
      <h1>Import og eksport av kontoplan</h1>

      {/* Import-seksjon */}
      <div style={{ background: '#f8f9fa', padding: 24, borderRadius: 8, marginBottom: 32 }}>
        <h2 style={{ marginTop: 0 }}>Importer kontoplan</h2>
        <p style={{ color: '#555', fontSize: 14 }}>
          Last opp en CSV- eller JSON-fil med kontoplan. CSV-filen skal bruke semikolon som skilletegn.
        </p>

        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          {/* Filvalg */}
          <div>
            <label style={{ display: 'block', marginBottom: 4, fontWeight: 600 }}>Fil</label>
            <input
              ref={filInputRef}
              type="file"
              accept=".csv,.json"
              onChange={(e) => {
                const fil = e.target.files?.[0] ?? null;
                setValgtFil(fil);
                setResultat(null);
                if (fil) {
                  const ext = fil.name.split('.').pop()?.toLowerCase();
                  if (ext === 'csv' || ext === 'json') {
                    setFormat(ext);
                  }
                }
              }}
              style={{ fontSize: 14 }}
            />
          </div>

          {/* Format */}
          <div>
            <label style={{ display: 'block', marginBottom: 4, fontWeight: 600 }}>Format</label>
            <select
              value={format}
              onChange={(e) => setFormat(e.target.value as ImportFormat)}
              style={{ padding: '8px 12px', border: '1px solid #ccc', borderRadius: 4, fontSize: 14 }}
            >
              <option value="csv">CSV (semikolonseparert)</option>
              <option value="json">JSON</option>
            </select>
          </div>

          {/* Modus */}
          <div>
            <label style={{ display: 'block', marginBottom: 4, fontWeight: 600 }}>Importmodus</label>
            <select
              value={modus}
              onChange={(e) => setModus(e.target.value as ImportModus)}
              style={{ padding: '8px 12px', border: '1px solid #ccc', borderRadius: 4, fontSize: 14 }}
            >
              <option value="opprett">Opprett bare nye kontoer</option>
              <option value="oppdater">Oppdater eksisterende kontoer</option>
              <option value="erstatt">Erstatt hele kontoplanen</option>
            </select>
            {modus === 'erstatt' && (
              <p style={{ color: '#d32f2f', fontSize: 13, marginTop: 4 }}>
                Advarsel: Dette vil erstatte hele kontoplanen. Systemkontoer beholdes, men andre kontoer kan bli slettet.
              </p>
            )}
          </div>

          <button
            onClick={handleImport}
            disabled={!valgtFil || importer.isPending}
            style={{
              padding: '10px 24px',
              background: valgtFil ? '#1a73e8' : '#ccc',
              color: '#fff',
              border: 'none',
              borderRadius: 4,
              cursor: valgtFil ? 'pointer' : 'not-allowed',
              fontSize: 14,
              alignSelf: 'flex-start',
            }}
          >
            {importer.isPending ? 'Importerer...' : 'Start import'}
          </button>
        </div>

        {/* Import-feil */}
        {importer.error && (
          <div style={{ marginTop: 16, padding: 12, background: '#ffeaea', border: '1px solid #f5c6c6', borderRadius: 4, color: '#d32f2f' }}>
            Feil ved import. Kontroller at filen har riktig format.
          </div>
        )}

        {/* Import-resultat */}
        {resultat && (
          <div style={{ marginTop: 24, padding: 16, background: '#fff', border: '1px solid #ddd', borderRadius: 4 }}>
            <h3 style={{ marginTop: 0 }}>Importresultat</h3>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 16, marginBottom: 16 }}>
              <div style={{ textAlign: 'center', padding: 12, background: '#e8f5e9', borderRadius: 4 }}>
                <div style={{ fontSize: 24, fontWeight: 700, color: '#2e7d32' }}>{resultat.opprettet}</div>
                <div style={{ fontSize: 13, color: '#555' }}>Opprettet</div>
              </div>
              <div style={{ textAlign: 'center', padding: 12, background: '#e3f2fd', borderRadius: 4 }}>
                <div style={{ fontSize: 24, fontWeight: 700, color: '#1565c0' }}>{resultat.oppdatert}</div>
                <div style={{ fontSize: 13, color: '#555' }}>Oppdatert</div>
              </div>
              <div style={{ textAlign: 'center', padding: 12, background: '#f5f5f5', borderRadius: 4 }}>
                <div style={{ fontSize: 24, fontWeight: 700, color: '#666' }}>{resultat.hoppetOver}</div>
                <div style={{ fontSize: 13, color: '#555' }}>Hoppet over</div>
              </div>
            </div>

            {resultat.feil.length > 0 && (
              <>
                <h4 style={{ color: '#d32f2f' }}>Feil ({resultat.feil.length})</h4>
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
                  <thead>
                    <tr style={{ textAlign: 'left', borderBottom: '1px solid #ddd' }}>
                      <th style={{ padding: '6px 8px' }}>Linje</th>
                      <th style={{ padding: '6px 8px' }}>Konto</th>
                      <th style={{ padding: '6px 8px' }}>Feilmelding</th>
                    </tr>
                  </thead>
                  <tbody>
                    {resultat.feil.map((feil, i) => (
                      <tr key={i} style={{ borderBottom: '1px solid #f0f0f0' }}>
                        <td style={{ padding: '6px 8px' }}>{feil.linje}</td>
                        <td style={{ padding: '6px 8px', fontFamily: 'monospace' }}>{feil.kontonummer}</td>
                        <td style={{ padding: '6px 8px', color: '#d32f2f' }}>{feil.melding}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </>
            )}
          </div>
        )}
      </div>

      {/* Eksport-seksjon */}
      <div style={{ background: '#f8f9fa', padding: 24, borderRadius: 8 }}>
        <h2 style={{ marginTop: 0 }}>Eksporter kontoplan</h2>
        <p style={{ color: '#555', fontSize: 14 }}>
          Last ned kontoplanen i oonsket format.
        </p>
        <div style={{ display: 'flex', gap: 8 }}>
          <button
            onClick={() => handleEksport('csv')}
            disabled={eksporter.isPending}
            style={{ padding: '10px 24px', background: '#fff', border: '1px solid #ccc', borderRadius: 4, cursor: 'pointer', fontSize: 14 }}
          >
            Last ned CSV
          </button>
          <button
            onClick={() => handleEksport('json')}
            disabled={eksporter.isPending}
            style={{ padding: '10px 24px', background: '#fff', border: '1px solid #ccc', borderRadius: 4, cursor: 'pointer', fontSize: 14 }}
          >
            Last ned JSON
          </button>
          <button
            onClick={() => handleEksport('saft')}
            disabled={eksporter.isPending}
            style={{ padding: '10px 24px', background: '#fff', border: '1px solid #ccc', borderRadius: 4, cursor: 'pointer', fontSize: 14 }}
          >
            Last ned SAF-T XML
          </button>
        </div>
        {eksporter.isPending && <p style={{ fontSize: 13, color: '#555' }}>Forbereder nedlasting...</p>}
      </div>
    </div>
  );
}
