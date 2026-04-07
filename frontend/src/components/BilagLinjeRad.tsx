import { useRef, useEffect, type KeyboardEvent } from 'react';
import KontoVelger from './KontoVelger';
import BelopFelt from './BelopFelt';
import type { KontoOppslagDto, MvaKodeDto } from '../types/kontoplan';

export interface BilagLinje {
  id: string;
  kontonummer: string;
  kontonavn: string;
  debet: number;
  kredit: number;
  mvaKode: string;
  beskrivelse: string;
  /** Foreslatt MVA-belop basert pa valgt kode */
  beregnetMvaBelop: number;
}

interface BilagLinjeRadProps {
  linje: BilagLinje;
  linjenummer: number;
  mvaKoder: MvaKodeDto[];
  disabled?: boolean;
  onEndre: (felt: Partial<BilagLinje>) => void;
  onFjern: () => void;
  onEnterNyLinje: () => void;
  /** Ref-callback for tastaturnavigering */
  onMountRad?: (element: HTMLTableRowElement | null) => void;
}

const cellStyle: React.CSSProperties = {
  padding: '2px 4px',
  borderBottom: '1px solid #e0e0e0',
  verticalAlign: 'top',
};

export default function BilagLinjeRad({
  linje,
  linjenummer,
  mvaKoder,
  disabled = false,
  onEndre,
  onFjern,
  onEnterNyLinje,
  onMountRad,
}: BilagLinjeRadProps) {
  const radRef = useRef<HTMLTableRowElement>(null);

  useEffect(() => {
    onMountRad?.(radRef.current);
  }, [onMountRad]);

  function handleKontoValgt(kontonummer: string, konto: KontoOppslagDto | null) {
    onEndre({
      kontonummer,
      kontonavn: konto?.navn ?? '',
      // Sett standard MVA-kode fra kontoen
      mvaKode: konto?.standardMvaKode ?? linje.mvaKode,
    });
  }

  function handleMvaEndring(kode: string) {
    onEndre({ mvaKode: kode });
  }

  function handleDebetEndring(verdi: number) {
    // Hvis debet fylles inn, nullstill kredit
    onEndre({ debet: verdi, kredit: verdi > 0 ? 0 : linje.kredit });
  }

  function handleKreditEndring(verdi: number) {
    // Hvis kredit fylles inn, nullstill debet
    onEndre({ kredit: verdi, debet: verdi > 0 ? 0 : linje.debet });
  }

  function handleTastatur(e: KeyboardEvent<HTMLTableRowElement>) {
    if (e.key === 'Enter' && !e.shiftKey && e.target instanceof HTMLInputElement) {
      // Enter pa et input-felt: opprett ny linje
      e.preventDefault();
      onEnterNyLinje();
    }
  }

  // Beregn MVA-info for visning
  const valgtMva = mvaKoder.find((m) => m.kode === linje.mvaKode);
  const grunnlag = linje.debet > 0 ? linje.debet : linje.kredit;
  const mvaBelop = valgtMva ? grunnlag * (valgtMva.sats / 100) : 0;

  return (
    <tr
      ref={radRef}
      onKeyDown={handleTastatur}
      style={{ backgroundColor: linjenummer % 2 === 0 ? '#fff' : '#fafafa' }}
    >
      {/* Linjenummer */}
      <td style={{ ...cellStyle, textAlign: 'center', color: '#999', width: 40, fontSize: 13 }}>
        {linjenummer}
      </td>

      {/* Konto */}
      <td style={{ ...cellStyle, minWidth: 200 }}>
        <KontoVelger
          verdi={linje.kontonummer}
          onChange={handleKontoValgt}
          disabled={disabled}
          placeholder="Konto..."
        />
      </td>

      {/* Debet */}
      <td style={{ ...cellStyle, width: 140 }}>
        <BelopFelt
          verdi={linje.debet}
          onChange={handleDebetEndring}
          disabled={disabled}
          placeholder="0,00"
        />
      </td>

      {/* Kredit */}
      <td style={{ ...cellStyle, width: 140 }}>
        <BelopFelt
          verdi={linje.kredit}
          onChange={handleKreditEndring}
          disabled={disabled}
          placeholder="0,00"
        />
      </td>

      {/* MVA-kode */}
      <td style={{ ...cellStyle, width: 160 }}>
        <select
          value={linje.mvaKode}
          onChange={(e) => handleMvaEndring(e.target.value)}
          disabled={disabled}
          style={{
            width: '100%',
            padding: '8px 8px',
            border: '1px solid #ccc',
            borderRadius: 4,
            fontSize: 13,
            boxSizing: 'border-box',
            backgroundColor: disabled ? '#f5f5f5' : '#fff',
          }}
        >
          <option value="">Ingen MVA</option>
          {mvaKoder.map((mva) => (
            <option key={mva.kode} value={mva.kode}>
              {mva.kode} - {mva.beskrivelse} ({mva.sats}%)
            </option>
          ))}
        </select>
        {mvaBelop > 0 && (
          <div style={{ fontSize: 11, color: '#666', marginTop: 2, textAlign: 'right' }}>
            MVA: {mvaBelop.toLocaleString('nb-NO', { minimumFractionDigits: 2 })}
          </div>
        )}
      </td>

      {/* Beskrivelse */}
      <td style={{ ...cellStyle, minWidth: 150 }}>
        <input
          type="text"
          value={linje.beskrivelse}
          onChange={(e) => onEndre({ beskrivelse: e.target.value })}
          disabled={disabled}
          placeholder="Beskrivelse..."
          style={{
            width: '100%',
            padding: '8px 8px',
            border: '1px solid #ccc',
            borderRadius: 4,
            fontSize: 13,
            boxSizing: 'border-box',
          }}
        />
      </td>

      {/* Fjern-knapp */}
      <td style={{ ...cellStyle, width: 40, textAlign: 'center' }}>
        {!disabled && (
          <button
            type="button"
            onClick={onFjern}
            title="Fjern linje"
            style={{
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              color: '#cc0000',
              fontSize: 18,
              fontWeight: 700,
              padding: '4px 8px',
              lineHeight: 1,
            }}
          >
            &times;
          </button>
        )}
      </td>
    </tr>
  );
}
