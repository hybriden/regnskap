import { useState, useRef, useEffect, type KeyboardEvent } from 'react';
import { useKontoOppslag } from '../hooks/api/useKontoplan';
import { formatKontonummer } from '../utils/formatering';
import type { KontoOppslagDto } from '../types/kontoplan';

interface KontoVelgerProps {
  verdi: string;
  onChange: (kontonummer: string, konto: KontoOppslagDto | null) => void;
  placeholder?: string;
  disabled?: boolean;
  label?: string;
  paakrevd?: boolean;
}

export default function KontoVelger({
  verdi,
  onChange,
  placeholder = 'Sok etter konto...',
  disabled = false,
  label,
  paakrevd = false,
}: KontoVelgerProps) {
  const [sokeTekst, setSokeTekst] = useState(verdi);
  const [erApen, setErApen] = useState(false);
  const [valgtIndex, setValgtIndex] = useState(-1);
  const inputRef = useRef<HTMLInputElement>(null);
  const listeRef = useRef<HTMLUListElement>(null);

  const { data: resultater = [], isLoading } = useKontoOppslag(sokeTekst);

  useEffect(() => {
    setSokeTekst(verdi);
  }, [verdi]);

  function handleInputEndring(tekst: string) {
    setSokeTekst(tekst);
    setErApen(tekst.length >= 1);
    setValgtIndex(-1);
    if (tekst === '') {
      onChange('', null);
    }
  }

  function velgKonto(konto: KontoOppslagDto) {
    setSokeTekst(`${konto.kontonummer} ${konto.navn}`);
    setErApen(false);
    onChange(konto.kontonummer, konto);
  }

  function handleTastatur(e: KeyboardEvent<HTMLInputElement>) {
    if (!erApen || resultater.length === 0) return;

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setValgtIndex((prev) => Math.min(prev + 1, resultater.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setValgtIndex((prev) => Math.max(prev - 1, 0));
    } else if (e.key === 'Enter' && valgtIndex >= 0) {
      e.preventDefault();
      velgKonto(resultater[valgtIndex]);
    } else if (e.key === 'Escape') {
      setErApen(false);
    }
  }

  return (
    <div style={{ position: 'relative' }}>
      {label && (
        <label style={{ display: 'block', marginBottom: 4, fontWeight: 600 }}>
          {label}
          {paakrevd && <span style={{ color: 'red' }}> *</span>}
        </label>
      )}
      <input
        ref={inputRef}
        type="text"
        value={sokeTekst}
        onChange={(e) => handleInputEndring(e.target.value)}
        onFocus={() => sokeTekst.length >= 1 && setErApen(true)}
        onBlur={() => setTimeout(() => setErApen(false), 200)}
        onKeyDown={handleTastatur}
        placeholder={placeholder}
        disabled={disabled}
        required={paakrevd}
        style={{
          width: '100%',
          padding: '8px 12px',
          border: '1px solid #ccc',
          borderRadius: 4,
          fontSize: 14,
          boxSizing: 'border-box',
        }}
        autoComplete="off"
      />
      {erApen && (
        <ul
          ref={listeRef}
          style={{
            position: 'absolute',
            top: '100%',
            left: 0,
            right: 0,
            margin: 0,
            padding: 0,
            listStyle: 'none',
            background: '#fff',
            border: '1px solid #ccc',
            borderTop: 'none',
            borderRadius: '0 0 4px 4px',
            maxHeight: 250,
            overflowY: 'auto',
            zIndex: 100,
            boxShadow: '0 4px 8px rgba(0,0,0,0.1)',
          }}
        >
          {isLoading && (
            <li style={{ padding: '8px 12px', color: '#666' }}>Soker...</li>
          )}
          {!isLoading && resultater.length === 0 && sokeTekst.length >= 1 && (
            <li style={{ padding: '8px 12px', color: '#666' }}>Ingen treff</li>
          )}
          {resultater.map((konto, index) => (
            <li
              key={konto.kontonummer}
              onMouseDown={() => velgKonto(konto)}
              style={{
                padding: '8px 12px',
                cursor: 'pointer',
                backgroundColor: index === valgtIndex ? '#e8f0fe' : 'transparent',
                borderBottom: '1px solid #f0f0f0',
                display: 'flex',
                gap: 12,
              }}
            >
              <span style={{ fontWeight: 600, fontFamily: 'monospace' }}>
                {formatKontonummer(konto.kontonummer)}
              </span>
              <span>{konto.navn}</span>
              <span style={{ marginLeft: 'auto', color: '#888', fontSize: 12 }}>
                {konto.kontotype}
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
