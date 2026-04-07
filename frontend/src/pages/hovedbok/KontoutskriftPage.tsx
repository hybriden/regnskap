import { useState } from 'react';
import { useKontoutskrift } from '../../hooks/api/useHovedbok';
import { formatBelop, formatDato } from '../../utils/formatering';
import KontoVelger from '../../components/KontoVelger';
import RegnskapsTabell from '../../components/RegnskapsTabell';
import type { RegnskapsLinje } from '../../components/RegnskapsTabell';
import type { KontoutskriftParams } from '../../types/hovedbok';

const currentYear = new Date().getFullYear();

export default function KontoutskriftPage() {
  const [kontonummer, setKontonummer] = useState('');
  const [fraDato, setFraDato] = useState(`${currentYear}-01-01`);
  const [tilDato, setTilDato] = useState(`${currentYear}-12-31`);

  const params: KontoutskriftParams = {
    fraDato,
    tilDato,
    antall: 500,
  };

  const { data, isLoading, error } = useKontoutskrift(kontonummer, params);

  const linjer: RegnskapsLinje[] = (data?.posteringer ?? []).map((p) => ({
    id: `${p.bilagsId}-${p.linjenummer}`,
    dato: formatDato(p.bilagsdato),
    referanse: p.bilagsId,
    beskrivelse: p.beskrivelse || p.bilagBeskrivelse,
    debet: p.side === 'Debet' ? p.belop : 0,
    kredit: p.side === 'Kredit' ? p.belop : 0,
    saldo: p.lOpendeBalanse,
  }));

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <h1 style={{ marginBottom: 24 }}>Kontoutskrift</h1>

      {/* Filtere */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '1fr 200px 200px',
          gap: 16,
          marginBottom: 24,
          padding: 16,
          backgroundColor: '#f8f8f8',
          borderRadius: 8,
          border: '1px solid #e0e0e0',
        }}
      >
        <KontoVelger
          label="Konto"
          verdi={kontonummer}
          onChange={(nr) => setKontonummer(nr)}
          paakrevd
        />
        <div>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600 }}>Fra dato</label>
          <input
            type="date"
            value={fraDato}
            onChange={(e) => setFraDato(e.target.value)}
            style={{
              width: '100%',
              padding: '8px 12px',
              border: '1px solid #ccc',
              borderRadius: 4,
              fontSize: 14,
              boxSizing: 'border-box',
            }}
          />
        </div>
        <div>
          <label style={{ display: 'block', marginBottom: 4, fontWeight: 600 }}>Til dato</label>
          <input
            type="date"
            value={tilDato}
            onChange={(e) => setTilDato(e.target.value)}
            style={{
              width: '100%',
              padding: '8px 12px',
              border: '1px solid #ccc',
              borderRadius: 4,
              fontSize: 14,
              boxSizing: 'border-box',
            }}
          />
        </div>
      </div>

      {/* Kontoinfo */}
      {data && (
        <div
          style={{
            marginBottom: 16,
            padding: 12,
            backgroundColor: '#e8f0fe',
            borderRadius: 4,
            display: 'flex',
            gap: 24,
            alignItems: 'center',
          }}
        >
          <span>
            <strong>Konto:</strong> {data.kontonummer} {data.kontonavn}
          </span>
          <span>
            <strong>Type:</strong> {data.kontotype}
          </span>
          <span>
            <strong>Normalbalanse:</strong> {data.normalbalanse}
          </span>
        </div>
      )}

      {/* Resultat */}
      {!kontonummer ? (
        <p style={{ color: '#666', textAlign: 'center', padding: 40 }}>
          Velg en konto for å vise kontoutskrift.
        </p>
      ) : isLoading ? (
        <p>Laster kontoutskrift...</p>
      ) : error ? (
        <p style={{ color: 'red' }}>Feil ved henting av kontoutskrift. Prøv igjen.</p>
      ) : data ? (
        <>
          <RegnskapsTabell
            linjer={linjer}
            visSaldo
            visSum
            visDato
            visReferanse
            inngaendeBalanse={data.inngaendeBalanse}
          />
          <div
            style={{
              marginTop: 16,
              padding: 12,
              backgroundColor: '#f8f8f8',
              borderRadius: 4,
              display: 'flex',
              gap: 24,
              fontFamily: 'monospace',
              fontSize: 14,
            }}
          >
            <span>
              <strong>Sum debet:</strong> {formatBelop(data.sumDebet)}
            </span>
            <span>
              <strong>Sum kredit:</strong> {formatBelop(data.sumKredit)}
            </span>
            <span
              style={{
                fontWeight: 700,
                color: data.utgaendeBalanse < 0 ? 'red' : 'inherit',
              }}
            >
              <strong>Utgående balanse:</strong> {formatBelop(data.utgaendeBalanse)}
            </span>
          </div>
          {data.totaltAntall > data.antall && (
            <p style={{ color: '#666', marginTop: 8, fontSize: 13 }}>
              Viser {data.antall} av {data.totaltAntall} posteringer.
            </p>
          )}
        </>
      ) : null}
    </div>
  );
}
