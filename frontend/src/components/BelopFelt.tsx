import { useState, useRef, type ChangeEvent, type FocusEvent, type KeyboardEvent } from 'react';
import { formatBelop, parseBelop } from '../utils/formatering';

interface BelopFeltProps {
  verdi: number;
  onChange: (verdi: number) => void;
  label?: string;
  placeholder?: string;
  disabled?: boolean;
  paakrevd?: boolean;
  /** Tillat negative verdier */
  tillattNegativ?: boolean;
  /** Callback ved Enter-tastetrykk */
  onEnter?: () => void;
}

export default function BelopFelt({
  verdi,
  onChange,
  label,
  placeholder = '0,00',
  disabled = false,
  paakrevd = false,
  tillattNegativ = false,
  onEnter,
}: BelopFeltProps) {
  const [redigerer, setRedigerer] = useState(false);
  const [tekstVerdi, setTekstVerdi] = useState('');
  const inputRef = useRef<HTMLInputElement>(null);

  const erNegativ = verdi < 0;

  function handleFokus(_e: FocusEvent<HTMLInputElement>) {
    setRedigerer(true);
    // Vis uformatert verdi for enklere redigering
    setTekstVerdi(verdi === 0 ? '' : verdi.toFixed(2).replace('.', ','));
  }

  function handleEndring(e: ChangeEvent<HTMLInputElement>) {
    // Tillat kun tall, komma, mellomrom og eventuelt minus
    let renset = e.target.value;
    if (tillattNegativ) {
      renset = renset.replace(/[^0-9,\s-]/g, '');
    } else {
      renset = renset.replace(/[^0-9,\s]/g, '');
    }
    setTekstVerdi(renset);
  }

  function handleBlur() {
    setRedigerer(false);
    const parsed = parseBelop(tekstVerdi);
    if (parsed !== null) {
      if (!tillattNegativ && parsed < 0) {
        onChange(0);
      } else {
        onChange(Math.round(parsed * 100) / 100);
      }
    } else if (tekstVerdi.trim() === '') {
      onChange(0);
    }
  }

  function handleTast(e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Enter') {
      inputRef.current?.blur();
      onEnter?.();
    }
  }

  return (
    <div>
      {label && (
        <label style={{ display: 'block', marginBottom: 4, fontWeight: 600 }}>
          {label}
          {paakrevd && <span style={{ color: 'red' }}> *</span>}
        </label>
      )}
      <input
        ref={inputRef}
        type="text"
        inputMode="decimal"
        value={redigerer ? tekstVerdi : formatBelop(verdi)}
        onChange={handleEndring}
        onFocus={handleFokus}
        onBlur={handleBlur}
        onKeyDown={handleTast}
        placeholder={placeholder}
        disabled={disabled}
        required={paakrevd}
        style={{
          width: '100%',
          padding: '8px 12px',
          border: '1px solid #ccc',
          borderRadius: 4,
          fontSize: 14,
          textAlign: 'right',
          fontFamily: 'monospace',
          color: erNegativ && !redigerer ? 'red' : 'inherit',
          boxSizing: 'border-box',
        }}
        autoComplete="off"
      />
    </div>
  );
}
