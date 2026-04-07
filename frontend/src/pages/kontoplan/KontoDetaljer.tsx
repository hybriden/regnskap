import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  useKonto,
  useOppdaterKonto,
  useSlettKonto,
  useDeaktiverKonto,
  useAktiverKonto,
  useMvaKoder,
} from '../../hooks/api/useKontoplan';
import { formatKontonummer } from '../../utils/formatering';
import type { OppdaterKontoRequest, GrupperingsKategori } from '../../types/kontoplan';

export default function KontoDetaljer() {
  const { id: kontonummer = '' } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: konto, isLoading, error } = useKonto(kontonummer);
  const { data: mvaKoder = [] } = useMvaKoder();
  const oppdater = useOppdaterKonto(kontonummer);
  const slett = useSlettKonto();
  const deaktiver = useDeaktiverKonto();
  const aktiver = useAktiverKonto();

  const [redigerer, setRedigerer] = useState(false);
  const [skjema, setSkjema] = useState<OppdaterKontoRequest>({
    navn: '',
    navnEn: null,
    erAktiv: true,
    erBokforbar: true,
    standardMvaKode: null,
    beskrivelse: null,
    grupperingsKategori: null,
    grupperingsKode: null,
    kreverAvdeling: false,
    kreverProsjekt: false,
  });

  useEffect(() => {
    if (konto) {
      setSkjema({
        navn: konto.navn,
        navnEn: konto.navnEn,
        erAktiv: konto.erAktiv,
        erBokforbar: konto.erBokforbar,
        standardMvaKode: konto.standardMvaKode,
        beskrivelse: konto.beskrivelse,
        grupperingsKategori: konto.grupperingsKategori,
        grupperingsKode: konto.grupperingsKode,
        kreverAvdeling: konto.kreverAvdeling,
        kreverProsjekt: konto.kreverProsjekt,
      });
    }
  }, [konto]);

  async function handleLagre() {
    await oppdater.mutateAsync(skjema);
    setRedigerer(false);
  }

  async function handleSlett() {
    if (!confirm(`Er du sikker pa at du vil slette konto ${kontonummer}?`)) return;
    await slett.mutateAsync(kontonummer);
    navigate('/kontoplan');
  }

  async function handleDeaktiver() {
    await deaktiver.mutateAsync(kontonummer);
  }

  async function handleAktiver() {
    await aktiver.mutateAsync(kontonummer);
  }

  if (isLoading) return <div style={{ padding: 24 }}>Laster konto...</div>;
  if (error) return <div style={{ padding: 24, color: 'red' }}>Feil: Konto {kontonummer} ble ikke funnet.</div>;
  if (!konto) return null;

  const feltStyle = {
    padding: '8px 12px',
    border: '1px solid #ccc',
    borderRadius: 4,
    fontSize: 14,
    width: '100%',
    boxSizing: 'border-box' as const,
  };

  return (
    <div style={{ padding: 24, maxWidth: 800, margin: '0 auto' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <div>
          <button
            onClick={() => navigate('/kontoplan')}
            style={{ background: 'none', border: 'none', color: '#1a73e8', cursor: 'pointer', padding: 0, fontSize: 14 }}
          >
            &larr; Tilbake til kontoplan
          </button>
          <h1 style={{ margin: '8px 0 0' }}>
            {formatKontonummer(konto.kontonummer)} - {konto.navn}
          </h1>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          {!redigerer ? (
            <>
              <button
                onClick={() => setRedigerer(true)}
                style={{ padding: '8px 16px', background: '#1a73e8', color: '#fff', border: 'none', borderRadius: 4, cursor: 'pointer' }}
              >
                Rediger
              </button>
              {konto.erAktiv ? (
                <button onClick={handleDeaktiver} style={{ padding: '8px 16px', background: '#f57c00', color: '#fff', border: 'none', borderRadius: 4, cursor: 'pointer' }}>
                  Deaktiver
                </button>
              ) : (
                <button onClick={handleAktiver} style={{ padding: '8px 16px', background: '#388e3c', color: '#fff', border: 'none', borderRadius: 4, cursor: 'pointer' }}>
                  Aktiver
                </button>
              )}
              {!konto.erSystemkonto && (
                <button onClick={handleSlett} style={{ padding: '8px 16px', background: '#d32f2f', color: '#fff', border: 'none', borderRadius: 4, cursor: 'pointer' }}>
                  Slett
                </button>
              )}
            </>
          ) : (
            <>
              <button
                onClick={handleLagre}
                disabled={oppdater.isPending}
                style={{ padding: '8px 16px', background: '#1a73e8', color: '#fff', border: 'none', borderRadius: 4, cursor: 'pointer' }}
              >
                {oppdater.isPending ? 'Lagrer...' : 'Lagre'}
              </button>
              <button
                onClick={() => setRedigerer(false)}
                style={{ padding: '8px 16px', background: '#f0f0f0', border: '1px solid #ccc', borderRadius: 4, cursor: 'pointer' }}
              >
                Avbryt
              </button>
            </>
          )}
        </div>
      </div>

      {/* Feilmeldinger */}
      {oppdater.error && (
        <div style={{ padding: 12, background: '#ffeaea', border: '1px solid #f5c6c6', borderRadius: 4, marginBottom: 16, color: '#d32f2f' }}>
          Feil ved lagring. Proov igjen.
        </div>
      )}
      {slett.error && (
        <div style={{ padding: 12, background: '#ffeaea', border: '1px solid #f5c6c6', borderRadius: 4, marginBottom: 16, color: '#d32f2f' }}>
          Kunne ikke slette kontoen. Har den posteringer eller underkontoer?
        </div>
      )}

      {/* Badges */}
      <div style={{ display: 'flex', gap: 8, marginBottom: 24 }}>
        <span style={{ padding: '4px 10px', borderRadius: 12, fontSize: 12, background: '#e3f2fd', color: '#1565c0' }}>
          {konto.kontotype}
        </span>
        <span style={{ padding: '4px 10px', borderRadius: 12, fontSize: 12, background: '#f3e5f5', color: '#7b1fa2' }}>
          {konto.normalbalanse}
        </span>
        {konto.erSystemkonto && (
          <span style={{ padding: '4px 10px', borderRadius: 12, fontSize: 12, background: '#fff3e0', color: '#e65100' }}>
            Systemkonto
          </span>
        )}
        <span style={{ padding: '4px 10px', borderRadius: 12, fontSize: 12, background: konto.erAktiv ? '#e8f5e9' : '#fbe9e7', color: konto.erAktiv ? '#2e7d32' : '#c62828' }}>
          {konto.erAktiv ? 'Aktiv' : 'Inaktiv'}
        </span>
      </div>

      {/* Skjema */}
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        {/* Kontonummer (ikke redigerbart) */}
        <div>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13, color: '#555' }}>Kontonummer</label>
          <div style={{ ...feltStyle, background: '#f5f5f5', color: '#333' }}>{formatKontonummer(konto.kontonummer)}</div>
        </div>

        {/* Kontogruppe (ikke redigerbart) */}
        <div>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13, color: '#555' }}>Kontogruppe</label>
          <div style={{ ...feltStyle, background: '#f5f5f5', color: '#333' }}>{konto.gruppekode} - {konto.gruppeNavn}</div>
        </div>

        {/* Navn */}
        <div>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13, color: '#555' }}>Navn</label>
          {redigerer ? (
            <input
              type="text"
              value={skjema.navn}
              onChange={(e) => setSkjema({ ...skjema, navn: e.target.value })}
              style={feltStyle}
              required
            />
          ) : (
            <div style={feltStyle}>{konto.navn}</div>
          )}
        </div>

        {/* Engelsk navn */}
        <div>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13, color: '#555' }}>Navn (engelsk)</label>
          {redigerer ? (
            <input
              type="text"
              value={skjema.navnEn ?? ''}
              onChange={(e) => setSkjema({ ...skjema, navnEn: e.target.value || null })}
              style={feltStyle}
            />
          ) : (
            <div style={feltStyle}>{konto.navnEn ?? '-'}</div>
          )}
        </div>

        {/* Standard MVA-kode */}
        <div>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13, color: '#555' }}>Standard MVA-kode</label>
          {redigerer ? (
            <select
              value={skjema.standardMvaKode ?? ''}
              onChange={(e) => setSkjema({ ...skjema, standardMvaKode: e.target.value || null })}
              style={feltStyle}
            >
              <option value="">Ingen</option>
              {mvaKoder.filter((m) => m.erAktiv).map((m) => (
                <option key={m.kode} value={m.kode}>
                  {m.kode} - {m.beskrivelse} ({m.sats} %)
                </option>
              ))}
            </select>
          ) : (
            <div style={feltStyle}>{konto.standardMvaKode ?? 'Ingen'}</div>
          )}
        </div>

        {/* SAF-T StandardAccountId */}
        <div>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13, color: '#555' }}>SAF-T StandardAccountId</label>
          <div style={{ ...feltStyle, background: '#f5f5f5', color: '#333' }}>{konto.standardAccountId}</div>
        </div>

        {/* Grupperingskategori */}
        <div>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13, color: '#555' }}>Grupperingskategori</label>
          {redigerer ? (
            <select
              value={skjema.grupperingsKategori ?? ''}
              onChange={(e) => setSkjema({ ...skjema, grupperingsKategori: (e.target.value || null) as GrupperingsKategori | null })}
              style={feltStyle}
            >
              <option value="">Ingen</option>
              <option value="RF1167">RF-1167 (Naringsoppgave 1)</option>
              <option value="RF1175">RF-1175 (Naringsoppgave 2)</option>
              <option value="RF1323">RF-1323 (Sma foretak)</option>
            </select>
          ) : (
            <div style={feltStyle}>{konto.grupperingsKategori ?? 'Ingen'}</div>
          )}
        </div>

        {/* Grupperingskode */}
        <div>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13, color: '#555' }}>Grupperingskode</label>
          {redigerer ? (
            <input
              type="text"
              value={skjema.grupperingsKode ?? ''}
              onChange={(e) => setSkjema({ ...skjema, grupperingsKode: e.target.value || null })}
              style={feltStyle}
            />
          ) : (
            <div style={feltStyle}>{konto.grupperingsKode ?? '-'}</div>
          )}
        </div>

        {/* Beskrivelse (full bredde) */}
        <div style={{ gridColumn: '1 / -1' }}>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600, fontSize: 13, color: '#555' }}>Beskrivelse</label>
          {redigerer ? (
            <textarea
              value={skjema.beskrivelse ?? ''}
              onChange={(e) => setSkjema({ ...skjema, beskrivelse: e.target.value || null })}
              style={{ ...feltStyle, minHeight: 80, resize: 'vertical' }}
            />
          ) : (
            <div style={{ ...feltStyle, minHeight: 40 }}>{konto.beskrivelse ?? '-'}</div>
          )}
        </div>

        {/* Avkrysninger */}
        {redigerer && (
          <div style={{ gridColumn: '1 / -1', display: 'flex', gap: 24, flexWrap: 'wrap' }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <input
                type="checkbox"
                checked={skjema.erBokforbar}
                onChange={(e) => setSkjema({ ...skjema, erBokforbar: e.target.checked })}
              />
              Bokforbar
            </label>
            <label style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <input
                type="checkbox"
                checked={skjema.kreverAvdeling}
                onChange={(e) => setSkjema({ ...skjema, kreverAvdeling: e.target.checked })}
              />
              Krever avdeling
            </label>
            <label style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <input
                type="checkbox"
                checked={skjema.kreverProsjekt}
                onChange={(e) => setSkjema({ ...skjema, kreverProsjekt: e.target.checked })}
              />
              Krever prosjekt
            </label>
          </div>
        )}
      </div>

      {/* Underkontoer */}
      {konto.underkontoer.length > 0 && (
        <div style={{ marginTop: 32 }}>
          <h2 style={{ fontSize: 16, marginBottom: 12 }}>Underkontoer</h2>
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ textAlign: 'left', borderBottom: '1px solid #ddd' }}>
                <th style={{ padding: '8px 12px' }}>Konto</th>
                <th style={{ padding: '8px 12px' }}>Navn</th>
                <th style={{ padding: '8px 12px' }}>Status</th>
              </tr>
            </thead>
            <tbody>
              {konto.underkontoer.map((uk) => (
                <tr
                  key={uk.kontonummer}
                  onClick={() => navigate(`/kontoplan/${uk.kontonummer}`)}
                  style={{ borderBottom: '1px solid #f0f0f0', cursor: 'pointer' }}
                >
                  <td style={{ padding: '8px 12px', fontFamily: 'monospace' }}>{formatKontonummer(uk.kontonummer)}</td>
                  <td style={{ padding: '8px 12px' }}>{uk.navn}</td>
                  <td style={{ padding: '8px 12px', color: uk.erAktiv ? 'green' : 'red', fontSize: 13 }}>
                    {uk.erAktiv ? 'Aktiv' : 'Inaktiv'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
