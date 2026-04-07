import { usePerioder } from '../hooks/api/useHovedbok';
import { PeriodeStatusNavn } from '../types/hovedbok';
import type { PeriodeDto } from '../types/hovedbok';

interface PeriodeVelgerProps {
  ar: number;
  valgtPeriode: number | undefined;
  onChange: (periode: PeriodeDto | null) => void;
  label?: string;
  /** Vis bare åpne perioder */
  bareApne?: boolean;
  disabled?: boolean;
}

export default function PeriodeVelger({
  ar,
  valgtPeriode,
  onChange,
  label = 'Periode',
  bareApne = false,
  disabled = false,
}: PeriodeVelgerProps) {
  const { data: perioderResponse, isLoading } = usePerioder(ar);

  const perioder = perioderResponse?.perioder ?? [];
  const filtrertePerioder = bareApne
    ? perioder.filter((p) => p.status === 'Apen')
    : perioder;

  function handleChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const verdi = e.target.value;
    if (verdi === '') {
      onChange(null);
    } else {
      const periode = perioder.find((p) => p.periode === Number(verdi));
      onChange(periode ?? null);
    }
  }

  return (
    <div>
      {label && (
        <label style={{ display: 'block', marginBottom: 4, fontWeight: 600 }}>
          {label}
        </label>
      )}
      <select
        value={valgtPeriode ?? ''}
        onChange={handleChange}
        disabled={disabled || isLoading}
        style={{
          width: '100%',
          padding: '8px 12px',
          border: '1px solid #ccc',
          borderRadius: 4,
          fontSize: 14,
          backgroundColor: '#fff',
          boxSizing: 'border-box',
        }}
      >
        <option value="">
          {isLoading ? 'Laster perioder...' : 'Velg periode'}
        </option>
        {filtrertePerioder.map((p) => (
          <option key={p.id} value={p.periode}>
            {p.periodenavn} ({PeriodeStatusNavn[p.status]})
          </option>
        ))}
      </select>
    </div>
  );
}
