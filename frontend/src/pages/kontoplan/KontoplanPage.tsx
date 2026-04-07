import { useState, useMemo } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useKontoer } from '../../hooks/api/useKontoplan';
import { formatKontonummer } from '../../utils/formatering';
import { Kontoklasse, KontoklasseNavn, type KontoSokParams, type KontoListeDto } from '../../types/kontoplan';

export default function KontoplanPage() {
  const navigate = useNavigate();

  const [sok, setSok] = useState('');
  const [kontoklasseFilter, setKontoklasseFilter] = useState<number | undefined>(undefined);
  const [visInaktive, setVisInaktive] = useState(false);
  const [side, setSide] = useState(1);

  const params: KontoSokParams = {
    sok: sok || undefined,
    kontoklasse: kontoklasseFilter,
    erAktiv: visInaktive ? undefined : true,
    side,
    antall: 100,
  };

  const { data: resultat, isLoading, error } = useKontoer(params);

  // Grupper kontoer etter kontoklasse
  const grupperteKontoer = useMemo(() => {
    if (!resultat?.data) return new Map<number, KontoListeDto[]>();
    const map = new Map<number, KontoListeDto[]>();
    for (const konto of resultat.data) {
      const klasse = konto.kontoklasse;
      if (!map.has(klasse)) {
        map.set(klasse, []);
      }
      map.get(klasse)!.push(konto);
    }
    return map;
  }, [resultat?.data]);

  const kontoklasser = Object.values(Kontoklasse) as Kontoklasse[];

  if (error) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil ved lasting av kontoplan</h1>
        <p>Kunne ikke hente kontoer fra server. Proov igjen senere.</p>
      </div>
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h1 style={{ margin: 0 }}>Kontoplan</h1>
        <div style={{ display: 'flex', gap: 8 }}>
          <Link
            to="/kontoplan/import"
            style={{
              padding: '8px 16px',
              background: '#f0f0f0',
              borderRadius: 4,
              textDecoration: 'none',
              color: '#333',
              border: '1px solid #ccc',
            }}
          >
            Importer
          </Link>
          <button
            onClick={() => navigate('/kontoplan/ny')}
            style={{
              padding: '8px 16px',
              background: '#1a73e8',
              color: '#fff',
              border: 'none',
              borderRadius: 4,
              cursor: 'pointer',
            }}
          >
            + Ny konto
          </button>
        </div>
      </div>

      {/* Filtre */}
      <div style={{ display: 'flex', gap: 16, marginBottom: 24, flexWrap: 'wrap', alignItems: 'center' }}>
        <input
          type="text"
          value={sok}
          onChange={(e) => { setSok(e.target.value); setSide(1); }}
          placeholder="Sok etter kontonummer eller navn..."
          style={{
            flex: 1,
            minWidth: 200,
            padding: '8px 12px',
            border: '1px solid #ccc',
            borderRadius: 4,
            fontSize: 14,
          }}
        />
        <select
          value={kontoklasseFilter ?? ''}
          onChange={(e) => { setKontoklasseFilter(e.target.value ? Number(e.target.value) : undefined); setSide(1); }}
          style={{ padding: '8px 12px', border: '1px solid #ccc', borderRadius: 4, fontSize: 14 }}
        >
          <option value="">Alle kontoklasser</option>
          {kontoklasser.map((k) => (
            <option key={k} value={k}>
              {k} - {KontoklasseNavn[k]}
            </option>
          ))}
        </select>
        <label style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: 14 }}>
          <input
            type="checkbox"
            checked={visInaktive}
            onChange={(e) => setVisInaktive(e.target.checked)}
          />
          Vis inaktive
        </label>
      </div>

      {isLoading && <p>Laster kontoplan...</p>}

      {/* Kontoliste gruppert etter kontoklasse */}
      {!isLoading && resultat && (
        <>
          {Array.from(grupperteKontoer.entries())
            .sort(([a], [b]) => a - b)
            .map(([klasse, kontoer]) => (
              <div key={klasse} style={{ marginBottom: 32 }}>
                <h2 style={{ fontSize: 18, borderBottom: '2px solid #1a73e8', paddingBottom: 8, color: '#1a73e8' }}>
                  {klasse} - {KontoklasseNavn[klasse as Kontoklasse] ?? `Klasse ${klasse}`}
                </h2>
                <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                  <thead>
                    <tr style={{ textAlign: 'left', borderBottom: '1px solid #ddd' }}>
                      <th style={{ padding: '8px 12px', width: 100 }}>Konto</th>
                      <th style={{ padding: '8px 12px' }}>Navn</th>
                      <th style={{ padding: '8px 12px', width: 100 }}>Type</th>
                      <th style={{ padding: '8px 12px', width: 80 }}>Balanse</th>
                      <th style={{ padding: '8px 12px', width: 60 }}>MVA</th>
                      <th style={{ padding: '8px 12px', width: 60 }}>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {kontoer.map((konto) => (
                      <tr
                        key={konto.kontonummer}
                        onClick={() => navigate(`/kontoplan/${konto.kontonummer}`)}
                        style={{
                          borderBottom: '1px solid #f0f0f0',
                          cursor: 'pointer',
                          opacity: konto.erAktiv ? 1 : 0.5,
                        }}
                        onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = '#f8f9fa'; }}
                        onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = 'transparent'; }}
                      >
                        <td style={{ padding: '8px 12px', fontFamily: 'monospace', fontWeight: 600 }}>
                          {formatKontonummer(konto.kontonummer)}
                          {konto.overordnetKontonummer && (
                            <span style={{ marginLeft: 4, color: '#888', fontSize: 11 }}>&#x21b3;</span>
                          )}
                        </td>
                        <td style={{ padding: '8px 12px' }}>
                          {konto.navn}
                          {konto.erSystemkonto && (
                            <span style={{ marginLeft: 8, fontSize: 11, color: '#888', background: '#f0f0f0', padding: '1px 6px', borderRadius: 3 }}>
                              system
                            </span>
                          )}
                        </td>
                        <td style={{ padding: '8px 12px', fontSize: 13, color: '#555' }}>{konto.kontotype}</td>
                        <td style={{ padding: '8px 12px', fontSize: 13, color: '#555' }}>{konto.normalbalanse}</td>
                        <td style={{ padding: '8px 12px', fontSize: 13, color: '#555' }}>{konto.standardMvaKode ?? '-'}</td>
                        <td style={{ padding: '8px 12px' }}>
                          {konto.erAktiv ? (
                            <span style={{ color: 'green', fontSize: 13 }}>Aktiv</span>
                          ) : (
                            <span style={{ color: 'red', fontSize: 13 }}>Inaktiv</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ))}

          {/* Paginering */}
          {resultat.totaltAntall > resultat.antall && (
            <div style={{ display: 'flex', justifyContent: 'center', gap: 8, marginTop: 24 }}>
              <button
                disabled={side <= 1}
                onClick={() => setSide((s) => s - 1)}
                style={{ padding: '8px 16px', border: '1px solid #ccc', borderRadius: 4, cursor: side <= 1 ? 'not-allowed' : 'pointer' }}
              >
                Forrige
              </button>
              <span style={{ padding: '8px 16px', lineHeight: '1.5' }}>
                Side {resultat.side} av {Math.ceil(resultat.totaltAntall / resultat.antall)}
              </span>
              <button
                disabled={side * resultat.antall >= resultat.totaltAntall}
                onClick={() => setSide((s) => s + 1)}
                style={{ padding: '8px 16px', border: '1px solid #ccc', borderRadius: 4, cursor: side * resultat.antall >= resultat.totaltAntall ? 'not-allowed' : 'pointer' }}
              >
                Neste
              </button>
            </div>
          )}

          <p style={{ color: '#666', fontSize: 13, textAlign: 'center', marginTop: 8 }}>
            Viser {resultat.data.length} av {resultat.totaltAntall} kontoer
          </p>
        </>
      )}
    </div>
  );
}
