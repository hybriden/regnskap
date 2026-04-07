import { useState, useCallback, useEffect } from 'react';
import BilagLinjeRad from './BilagLinjeRad';
import { useBilagSerier } from '../hooks/api/useBilag';
import { useMvaKoder } from '../hooks/api/useKontoplan';
import { formatBelop } from '../utils/formatering';
import { BilagType } from '../types/hovedbok';
import type { BilagLinje } from './BilagLinjeRad';
import type { BilagSerieDto } from '../types/bilag';
import type { MvaKodeDto } from '../types/kontoplan';

/** Generer en unik ID for nye linjer */
let linjeIdTeller = 0;
function genererLinjeId(): string {
  linjeIdTeller++;
  return `ny-${linjeIdTeller}-${Date.now()}`;
}

function tomLinje(): BilagLinje {
  return {
    id: genererLinjeId(),
    kontonummer: '',
    kontonavn: '',
    debet: 0,
    kredit: 0,
    mvaKode: '',
    beskrivelse: '',
    beregnetMvaBelop: 0,
  };
}

export interface BilagsEditorData {
  dato: string;
  beskrivelse: string;
  serieKode: string;
  bilagType: string;
  eksternReferanse: string;
  linjer: BilagLinje[];
}

interface BilagsEditorProps {
  /** Initiell data for redigering (f.eks. ved visning av eksisterende bilag) */
  initialData?: Partial<BilagsEditorData>;
  /** Skrivebeskyttet modus */
  disabled?: boolean;
  /** Callback nar data endres */
  onChange?: (data: BilagsEditorData) => void;
  /** Callback for lagring */
  onLagre?: (data: BilagsEditorData) => void;
  /** Callback for avbryt */
  onAvbryt?: () => void;
  /** Vis lagre/avbryt-knapper */
  visKnapper?: boolean;
  /** Laster / sender inn */
  laster?: boolean;
  /** Feilmeldinger fra validering */
  valideringsfeil?: string[];
}

const BilagTypeNavn: Record<string, string> = {
  [BilagType.Manuelt]: 'Manuelt',
  [BilagType.InngaendeFaktura]: 'Inngaende faktura',
  [BilagType.UtgaendeFaktura]: 'Utgaende faktura',
  [BilagType.Bank]: 'Bank',
  [BilagType.Lonn]: 'Lonn',
  [BilagType.Avskrivning]: 'Avskrivning',
  [BilagType.MvaOppgjor]: 'MVA-oppgjor',
  [BilagType.Arsavslutning]: 'Arsavslutning',
  [BilagType.Apningsbalanse]: 'Apningsbalanse',
  [BilagType.Kreditnota]: 'Kreditnota',
  [BilagType.Korreksjon]: 'Korreksjon',
};

export default function BilagsEditor({
  initialData,
  disabled = false,
  onChange,
  onLagre,
  onAvbryt,
  visKnapper = true,
  laster = false,
  valideringsfeil = [],
}: BilagsEditorProps) {
  const [dato, setDato] = useState(initialData?.dato ?? new Date().toISOString().slice(0, 10));
  const [beskrivelse, setBeskrivelse] = useState(initialData?.beskrivelse ?? '');
  const [serieKode, setSerieKode] = useState(initialData?.serieKode ?? 'MAN');
  const [bilagType, setBilagType] = useState(initialData?.bilagType ?? BilagType.Manuelt);
  const [eksternReferanse, setEksternReferanse] = useState(initialData?.eksternReferanse ?? '');
  const [linjer, setLinjer] = useState<BilagLinje[]>(
    initialData?.linjer ?? [tomLinje(), tomLinje()],
  );

  const { data: serier = [] } = useBilagSerier();
  const { data: mvaKoder = [] } = useMvaKoder({ erAktiv: true });

  // Beregn summer
  const sumDebet = linjer.reduce((sum, l) => sum + l.debet, 0);
  const sumKredit = linjer.reduce((sum, l) => sum + l.kredit, 0);
  const differanse = Math.round((sumDebet - sumKredit) * 100) / 100;
  const erIBalanse = differanse === 0;

  // Beregn MVA-summer for forhåndsvisning
  const sumMva = linjer.reduce((sum, l) => {
    const mva = mvaKoder.find((m: MvaKodeDto) => m.kode === l.mvaKode);
    if (!mva) return sum;
    const grunnlag = l.debet > 0 ? l.debet : l.kredit;
    return sum + grunnlag * (mva.sats / 100);
  }, 0);

  // Notify parent on changes
  const getData = useCallback((): BilagsEditorData => ({
    dato,
    beskrivelse,
    serieKode,
    bilagType,
    eksternReferanse,
    linjer,
  }), [dato, beskrivelse, serieKode, bilagType, eksternReferanse, linjer]);

  useEffect(() => {
    onChange?.(getData());
  }, [dato, beskrivelse, serieKode, bilagType, eksternReferanse, linjer, onChange, getData]);

  // Oppdater type nar serie endres
  function handleSerieEndring(kode: string) {
    setSerieKode(kode);
    const serie = serier.find((s: BilagSerieDto) => s.kode === kode);
    if (serie) {
      setBilagType(serie.standardType);
    }
  }

  function handleLinjeEndring(index: number, felt: Partial<BilagLinje>) {
    setLinjer((prev) => {
      const oppdatert = [...prev];
      oppdatert[index] = { ...oppdatert[index], ...felt };
      return oppdatert;
    });
  }

  function leggTilLinje() {
    setLinjer((prev) => [...prev, tomLinje()]);
  }

  function fjernLinje(index: number) {
    setLinjer((prev) => {
      if (prev.length <= 2) return prev; // Minimum 2 linjer
      return prev.filter((_, i) => i !== index);
    });
  }

  function handleLagre() {
    onLagre?.(getData());
  }

  // Tastatursnarvei: Ctrl+S for lagring
  useEffect(() => {
    if (disabled) return;
    function handleKeyDown(e: globalThis.KeyboardEvent) {
      if (e.ctrlKey && e.key === 's') {
        e.preventDefault();
        handleLagre();
      }
    }
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  });

  return (
    <div>
      {/* Header: dato, beskrivelse, serie, type */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '160px 1fr 160px 200px',
          gap: 16,
          marginBottom: 16,
        }}
      >
        {/* Dato */}
        <div>
          <label style={labelStyle}>
            Bilagsdato <span style={{ color: 'red' }}>*</span>
          </label>
          <input
            type="date"
            value={dato}
            onChange={(e) => setDato(e.target.value)}
            disabled={disabled}
            style={inputStyle}
          />
        </div>

        {/* Beskrivelse */}
        <div>
          <label style={labelStyle}>
            Beskrivelse <span style={{ color: 'red' }}>*</span>
          </label>
          <input
            type="text"
            value={beskrivelse}
            onChange={(e) => setBeskrivelse(e.target.value)}
            disabled={disabled}
            placeholder="Beskrivelse av bilaget..."
            style={inputStyle}
          />
        </div>

        {/* Bilagserie */}
        <div>
          <label style={labelStyle}>Bilagserie</label>
          <select
            value={serieKode}
            onChange={(e) => handleSerieEndring(e.target.value)}
            disabled={disabled}
            style={inputStyle}
          >
            {serier.length === 0 && <option value="MAN">MAN - Manuelt bilag</option>}
            {serier.map((s: BilagSerieDto) => (
              <option key={s.kode} value={s.kode}>
                {s.kode} - {s.navn}
              </option>
            ))}
          </select>
        </div>

        {/* Bilagtype */}
        <div>
          <label style={labelStyle}>Type</label>
          <select
            value={bilagType}
            onChange={(e) => setBilagType(e.target.value)}
            disabled={disabled}
            style={inputStyle}
          >
            {Object.entries(BilagTypeNavn).map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Ekstern referanse */}
      <div style={{ marginBottom: 16 }}>
        <label style={labelStyle}>Ekstern referanse</label>
        <input
          type="text"
          value={eksternReferanse}
          onChange={(e) => setEksternReferanse(e.target.value)}
          disabled={disabled}
          placeholder="Fakturanummer, ordrenummer etc."
          style={{ ...inputStyle, maxWidth: 400 }}
        />
      </div>

      {/* Posteringslinjer */}
      <div style={{ overflowX: 'auto' }}>
        <table
          style={{
            width: '100%',
            borderCollapse: 'collapse',
            border: '1px solid #e0e0e0',
            minWidth: 900,
          }}
        >
          <thead>
            <tr>
              <th style={{ ...thStyle, width: 40, textAlign: 'center' }}>#</th>
              <th style={{ ...thStyle, textAlign: 'left' }}>Konto</th>
              <th style={{ ...thStyle, width: 140 }}>Debet</th>
              <th style={{ ...thStyle, width: 140 }}>Kredit</th>
              <th style={{ ...thStyle, width: 160 }}>MVA-kode</th>
              <th style={{ ...thStyle, textAlign: 'left' }}>Beskrivelse</th>
              <th style={{ ...thStyle, width: 40 }} />
            </tr>
          </thead>
          <tbody>
            {linjer.map((linje, index) => (
              <BilagLinjeRad
                key={linje.id}
                linje={linje}
                linjenummer={index + 1}
                mvaKoder={mvaKoder}
                disabled={disabled}
                onEndre={(felt) => handleLinjeEndring(index, felt)}
                onFjern={() => fjernLinje(index)}
                onEnterNyLinje={leggTilLinje}
              />
            ))}
          </tbody>

          {/* Sum-rad */}
          <tfoot>
            <tr style={{ backgroundColor: '#f8f8f8' }}>
              <td style={sumCellStyle} />
              <td style={{ ...sumCellStyle, textAlign: 'left', fontWeight: 700 }}>Sum</td>
              <td style={{ ...sumCellStyle, fontWeight: 700 }}>{formatBelop(sumDebet)}</td>
              <td style={{ ...sumCellStyle, fontWeight: 700 }}>{formatBelop(sumKredit)}</td>
              <td style={sumCellStyle} />
              <td style={sumCellStyle} />
              <td style={sumCellStyle} />
            </tr>
          </tfoot>
        </table>
      </div>

      {/* Balanse-indikator og MVA-info */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginTop: 12,
          padding: '8px 12px',
          borderRadius: 4,
          backgroundColor: erIBalanse ? '#e8f5e9' : '#ffebee',
          border: `1px solid ${erIBalanse ? '#a5d6a7' : '#ef9a9a'}`,
        }}
      >
        <div>
          <span
            style={{
              fontWeight: 700,
              color: erIBalanse ? '#2e7d32' : '#c62828',
            }}
          >
            {erIBalanse
              ? 'Bilaget er i balanse'
              : `Ikke i balanse - differanse: ${formatBelop(differanse)}`}
          </span>
          {sumMva > 0 && (
            <span style={{ marginLeft: 16, color: '#666', fontSize: 13 }}>
              Beregnet MVA: {formatBelop(sumMva)}
            </span>
          )}
        </div>
        <div style={{ display: 'flex', gap: 16, fontFamily: 'monospace', fontSize: 14 }}>
          <span>
            Debet: <strong>{formatBelop(sumDebet)}</strong>
          </span>
          <span>
            Kredit: <strong>{formatBelop(sumKredit)}</strong>
          </span>
        </div>
      </div>

      {/* Legg til linje-knapp */}
      {!disabled && (
        <div style={{ marginTop: 8 }}>
          <button
            type="button"
            onClick={leggTilLinje}
            style={{
              padding: '6px 16px',
              border: '1px dashed #999',
              borderRadius: 4,
              background: '#fafafa',
              cursor: 'pointer',
              fontSize: 13,
              color: '#333',
            }}
          >
            + Legg til linje
          </button>
          <span style={{ marginLeft: 12, color: '#888', fontSize: 12 }}>
            Trykk Enter i siste felt for ny linje. Minimum 2 linjer.
          </span>
        </div>
      )}

      {/* Valideringsfeil */}
      {valideringsfeil.length > 0 && (
        <div
          style={{
            marginTop: 12,
            padding: 12,
            backgroundColor: '#ffebee',
            border: '1px solid #ef9a9a',
            borderRadius: 4,
          }}
        >
          <strong style={{ color: '#c62828' }}>Valideringsfeil:</strong>
          <ul style={{ margin: '4px 0 0', paddingLeft: 20 }}>
            {valideringsfeil.map((feil, i) => (
              <li key={i} style={{ color: '#c62828', fontSize: 13 }}>
                {feil}
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Lagre/Avbryt-knapper */}
      {visKnapper && !disabled && (
        <div style={{ marginTop: 16, display: 'flex', gap: 12 }}>
          <button
            type="button"
            onClick={handleLagre}
            disabled={laster}
            style={{
              padding: '10px 24px',
              background: '#0066cc',
              color: '#fff',
              border: 'none',
              borderRadius: 4,
              fontSize: 14,
              fontWeight: 600,
              cursor: laster ? 'not-allowed' : 'pointer',
              opacity: laster ? 0.7 : 1,
            }}
          >
            {laster ? 'Lagrer...' : 'Lagre bilag'}
          </button>
          {onAvbryt && (
            <button
              type="button"
              onClick={onAvbryt}
              disabled={laster}
              style={{
                padding: '10px 24px',
                background: '#f0f0f0',
                color: '#333',
                border: '1px solid #ccc',
                borderRadius: 4,
                fontSize: 14,
                cursor: 'pointer',
              }}
            >
              Avbryt
            </button>
          )}
          <span style={{ marginLeft: 'auto', color: '#888', fontSize: 12, alignSelf: 'center' }}>
            Ctrl+S for a lagre
          </span>
        </div>
      )}
    </div>
  );
}

// --- Styles ---

const labelStyle: React.CSSProperties = {
  display: 'block',
  marginBottom: 4,
  fontWeight: 600,
  fontSize: 13,
};

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
  textAlign: 'right',
  fontWeight: 700,
  backgroundColor: '#f8f8f8',
  fontSize: 13,
};

const sumCellStyle: React.CSSProperties = {
  padding: '8px 12px',
  borderTop: '2px solid #333',
  textAlign: 'right',
  fontFamily: 'monospace',
  fontSize: 14,
};
