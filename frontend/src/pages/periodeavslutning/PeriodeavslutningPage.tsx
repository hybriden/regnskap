import { useState } from 'react';
import { Link } from 'react-router-dom';
import { usePeriodeStatus, useArsavslutningStatus } from '../../hooks/api/usePeriodeavslutning';
import { ArsavslutningFaseNavn } from '../../types/periodeavslutning';
import type { PeriodeStatusDto } from '../../types/periodeavslutning';
import { formatBelop, formatDato } from '../../utils/formatering';

const currentYear = new Date().getFullYear();

const periodeNavn: Record<number, string> = {
  0: 'Åpningsbalanse',
  1: 'Januar',
  2: 'Februar',
  3: 'Mars',
  4: 'April',
  5: 'Mai',
  6: 'Juni',
  7: 'Juli',
  8: 'August',
  9: 'September',
  10: 'Oktober',
  11: 'November',
  12: 'Desember',
  13: 'Årsavslutning',
};

function statusFarge(status: string): { backgroundColor: string; color: string } {
  switch (status) {
    case 'Lukket':
      return { backgroundColor: '#e8f5e9', color: '#2e7d32' };
    case 'Sperret':
      return { backgroundColor: '#fff3e0', color: '#e65100' };
    case 'Apen':
      return { backgroundColor: '#e3f2fd', color: '#1565c0' };
    default:
      return { backgroundColor: '#f5f5f5', color: '#616161' };
  }
}

function statusTekst(status: string): string {
  switch (status) {
    case 'Lukket':
      return 'Lukket';
    case 'Sperret':
      return 'Sperret';
    case 'Apen':
      return 'Åpen';
    default:
      return status;
  }
}

export default function PeriodeavslutningPage() {
  const [ar, setAr] = useState(currentYear);
  const { data: perioder, isLoading, error } = usePeriodeStatus(ar);
  const { data: arsavslutning } = useArsavslutningStatus(ar);

  const antallLukket = perioder?.filter((p) => p.status === 'Lukket' && p.periode >= 1 && p.periode <= 12).length ?? 0;
  const nesteApenPeriode = perioder?.find((p) => p.status === 'Apen' && p.periode >= 1 && p.periode <= 12);

  if (error) {
    return (
      <div style={{ padding: 24, color: 'red' }}>
        <h1>Feil ved lasting av periodeoversikt</h1>
        <p>Kunne ikke hente periodedata fra server.</p>
      </div>
    );
  }

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
        <h1 style={{ margin: 0 }}>Periodeavslutning</h1>
        <div style={{ display: 'flex', gap: 8 }}>
          <Link to="/periodeavslutning/avskrivning" style={linkButtonStyle}>
            Avskrivninger
          </Link>
          <Link to="/periodeavslutning/periodisering" style={linkButtonStyle}>
            Periodiseringer
          </Link>
          <Link to="/periodeavslutning/anleggsmidler" style={linkButtonStyle}>
            Anleggsmidler
          </Link>
        </div>
      </div>

      {/* Arsvelger */}
      <div style={{ marginBottom: 24, display: 'flex', alignItems: 'center', gap: 12 }}>
        <label style={{ fontWeight: 600 }}>Regnskapsår:</label>
        <select
          value={ar}
          onChange={(e) => setAr(Number(e.target.value))}
          style={selectStyle}
        >
          {Array.from({ length: 5 }, (_, i) => currentYear - 2 + i).map((year) => (
            <option key={year} value={year}>{year}</option>
          ))}
        </select>
      </div>

      {/* Sammendrag */}
      <div style={{ display: 'flex', gap: 16, marginBottom: 24, flexWrap: 'wrap' }}>
        <div style={sammendragBoks}>
          <div style={{ fontSize: 28, fontWeight: 700 }}>{antallLukket}/12</div>
          <div style={{ color: '#666', fontSize: 13 }}>Perioder lukket</div>
        </div>
        <div style={sammendragBoks}>
          <div style={{ fontSize: 14, fontWeight: 600 }}>
            {arsavslutning
              ? ArsavslutningFaseNavn[arsavslutning.fase]
              : 'Ikke startet'}
          </div>
          <div style={{ color: '#666', fontSize: 13 }}>Årsavslutning</div>
        </div>
        {nesteApenPeriode && (
          <div style={{ ...sammendragBoks, borderColor: '#1565c0' }}>
            <div style={{ fontSize: 14, fontWeight: 600 }}>
              {periodeNavn[nesteApenPeriode.periode] ?? `Periode ${nesteApenPeriode.periode}`}
            </div>
            <div style={{ color: '#666', fontSize: 13 }}>Neste periode å lukke</div>
            <Link
              to={`/periodeavslutning/manedslukking?ar=${ar}&periode=${nesteApenPeriode.periode}`}
              style={{ fontSize: 13, color: '#1565c0', marginTop: 4 }}
            >
              Gå til lukking
            </Link>
          </div>
        )}
        {antallLukket === 12 && arsavslutning?.fase === 'IkkeStartet' && (
          <div style={{ ...sammendragBoks, borderColor: '#2e7d32' }}>
            <div style={{ fontSize: 14, fontWeight: 600, color: '#2e7d32' }}>
              Klar for årsavslutning
            </div>
            <Link
              to={`/periodeavslutning/arsavslutning?ar=${ar}`}
              style={{ fontSize: 13, color: '#2e7d32', marginTop: 4 }}
            >
              Start årsavslutning
            </Link>
          </div>
        )}
      </div>

      {/* Periodetabell */}
      {isLoading ? (
        <p>Laster perioder...</p>
      ) : !perioder || perioder.length === 0 ? (
        <div style={{ padding: 32, textAlign: 'center', border: '1px solid #e0e0e0', borderRadius: 8 }}>
          <p style={{ color: '#666' }}>Ingen perioder funnet for {ar}.</p>
        </div>
      ) : (
        <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
          <thead>
            <tr>
              <th style={thStyle}>Periode</th>
              <th style={thStyle}>Status</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Antall bilag</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Sum debet</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Sum kredit</th>
              <th style={thStyle}>Lukket</th>
              <th style={thStyle}>Handlinger</th>
            </tr>
          </thead>
          <tbody>
            {perioder.map((p: PeriodeStatusDto) => (
              <tr key={p.periode} style={{ backgroundColor: p.periode === 0 || p.periode === 13 ? '#fafafa' : '#fff' }}>
                <td style={tdStyle}>
                  <strong>{periodeNavn[p.periode] ?? `Periode ${p.periode}`}</strong>
                  <span style={{ color: '#999', marginLeft: 6, fontSize: 12 }}>({p.periode})</span>
                </td>
                <td style={tdStyle}>
                  <span style={{ padding: '2px 8px', borderRadius: 12, fontSize: 12, fontWeight: 600, ...statusFarge(p.status) }}>
                    {statusTekst(p.status)}
                  </span>
                </td>
                <td style={{ ...tdStyle, textAlign: 'right' }}>{p.antallBilag}</td>
                <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(p.sumDebet)}
                </td>
                <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(p.sumKredit)}
                </td>
                <td style={tdStyle}>
                  {p.lukketTidspunkt ? (
                    <span style={{ fontSize: 12, color: '#666' }}>
                      {formatDato(p.lukketTidspunkt)}
                      {p.lukketAv && ` av ${p.lukketAv}`}
                    </span>
                  ) : (
                    <span style={{ color: '#999', fontSize: 12 }}>--</span>
                  )}
                </td>
                <td style={tdStyle}>
                  {p.status === 'Apen' && p.periode >= 1 && p.periode <= 12 && (
                    <Link
                      to={`/periodeavslutning/manedslukking?ar=${ar}&periode=${p.periode}`}
                      style={actionLinkStyle}
                    >
                      Lukk periode
                    </Link>
                  )}
                  {p.status === 'Lukket' && p.periode >= 1 && p.periode <= 12 && (
                    <span style={{ fontSize: 12, color: '#999' }}>Lukket</span>
                  )}
                  {p.periode === 13 && antallLukket === 12 && p.status !== 'Lukket' && (
                    <Link
                      to={`/periodeavslutning/arsavslutning?ar=${ar}`}
                      style={{ ...actionLinkStyle, background: '#e8f5e9', color: '#2e7d32' }}
                    >
                      Årsavslutning
                    </Link>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
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

const selectStyle: React.CSSProperties = {
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
};

const sammendragBoks: React.CSSProperties = {
  padding: 16,
  border: '1px solid #e0e0e0',
  borderRadius: 8,
  minWidth: 160,
  textAlign: 'center',
};

const linkButtonStyle: React.CSSProperties = {
  padding: '8px 16px',
  background: '#0066cc',
  color: '#fff',
  borderRadius: 4,
  textDecoration: 'none',
  fontSize: 14,
};

const actionLinkStyle: React.CSSProperties = {
  padding: '4px 12px',
  background: '#e3f2fd',
  color: '#1565c0',
  borderRadius: 4,
  textDecoration: 'none',
  fontSize: 13,
};
