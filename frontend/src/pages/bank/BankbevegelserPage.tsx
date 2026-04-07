import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useBankkontoer, useBankbevegelser } from '../../hooks/api/useBank';
import {
  BankbevegelseStatus,
  BankbevegelseStatusNavn,
  BankbevegelseRetning,
  BankbevegelseRetningNavn,
  MatcheTypeNavn,
} from '../../types/bank';
import type { BankbevegelseDto, BankbevegelseStatus as BevStatus } from '../../types/bank';
import { formatBelop, formatDato } from '../../utils/formatering';

export default function BankbevegelserPage() {
  const { data: kontoer, isLoading: lasterKontoer } = useBankkontoer();

  const [valgtKontoId, setValgtKontoId] = useState('');
  const [statusFilter, setStatusFilter] = useState<BevStatus | ''>('');
  const [retningFilter, setRetningFilter] = useState<string>('');

  const { data: bevegelser, isLoading: lasterBevegelser } = useBankbevegelser({
    bankkontoId: valgtKontoId,
    status: statusFilter || undefined,
    retning: (retningFilter as 'Inn' | 'Ut') || undefined,
  });

  const filtrerteBevegelser = bevegelser ?? [];

  // Beregn statistikk
  const antallTotalt = filtrerteBevegelser.length;
  const sumInn = filtrerteBevegelser
    .filter((b) => b.retning === BankbevegelseRetning.Inn)
    .reduce((sum, b) => sum + b.belop, 0);
  const sumUt = filtrerteBevegelser
    .filter((b) => b.retning === BankbevegelseRetning.Ut)
    .reduce((sum, b) => sum + b.belop, 0);

  return (
    <div style={{ padding: 24, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ marginBottom: 16 }}>
        <Link to="/bank" style={{ color: '#0066cc', textDecoration: 'none', fontSize: 14 }}>
          &larr; Tilbake til bankkontoer
        </Link>
      </div>

      <h1 style={{ marginBottom: 24 }}>Bankbevegelser</h1>

      {/* Filtre */}
      <div
        style={{
          display: 'flex',
          gap: 16,
          alignItems: 'flex-end',
          marginBottom: 24,
          flexWrap: 'wrap',
        }}
      >
        <div>
          <label style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 13 }}>
            Bankkonto
          </label>
          {lasterKontoer ? (
            <span style={{ fontSize: 13, color: '#666' }}>Laster...</span>
          ) : (
            <select
              value={valgtKontoId}
              onChange={(e) => setValgtKontoId(e.target.value)}
              style={selectStyle}
            >
              <option value="">-- Velg bankkonto --</option>
              {kontoer?.map((k) => (
                <option key={k.id} value={k.id}>
                  {k.kontonummer} - {k.beskrivelse}
                </option>
              ))}
            </select>
          )}
        </div>
        <div>
          <label style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 13 }}>
            Status
          </label>
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as BevStatus | '')}
            style={selectStyle}
          >
            <option value="">Alle</option>
            {Object.entries(BankbevegelseStatusNavn).map(([key, navn]) => (
              <option key={key} value={key}>
                {navn}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 13 }}>
            Retning
          </label>
          <select
            value={retningFilter}
            onChange={(e) => setRetningFilter(e.target.value)}
            style={selectStyle}
          >
            <option value="">Alle</option>
            {Object.entries(BankbevegelseRetningNavn).map(([key, navn]) => (
              <option key={key} value={key}>
                {navn}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Statistikk */}
      {valgtKontoId && bevegelser && (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(3, 1fr)',
            gap: 16,
            marginBottom: 24,
          }}
        >
          <div
            style={{
              padding: 12,
              border: '1px solid #e0e0e0',
              borderRadius: 8,
              textAlign: 'center',
            }}
          >
            <div style={{ fontSize: 12, color: '#666' }}>Antall transaksjoner</div>
            <div style={{ fontSize: 20, fontWeight: 700 }}>{antallTotalt}</div>
          </div>
          <div
            style={{
              padding: 12,
              border: '1px solid #e0e0e0',
              borderRadius: 8,
              textAlign: 'center',
            }}
          >
            <div style={{ fontSize: 12, color: '#666' }}>Sum innbetalinger</div>
            <div style={{ fontSize: 20, fontWeight: 700, color: '#2e7d32', fontFamily: 'monospace' }}>
              {formatBelop(sumInn)}
            </div>
          </div>
          <div
            style={{
              padding: 12,
              border: '1px solid #e0e0e0',
              borderRadius: 8,
              textAlign: 'center',
            }}
          >
            <div style={{ fontSize: 12, color: '#666' }}>Sum utbetalinger</div>
            <div style={{ fontSize: 20, fontWeight: 700, color: '#c62828', fontFamily: 'monospace' }}>
              {formatBelop(sumUt)}
            </div>
          </div>
        </div>
      )}

      {/* Tabell */}
      {!valgtKontoId ? (
        <div
          style={{
            padding: 32,
            textAlign: 'center',
            border: '1px solid #e0e0e0',
            borderRadius: 8,
            color: '#666',
          }}
        >
          Velg en bankkonto for a vise bevegelser.
        </div>
      ) : lasterBevegelser ? (
        <p>Laster bevegelser...</p>
      ) : filtrerteBevegelser.length === 0 ? (
        <div
          style={{
            padding: 32,
            textAlign: 'center',
            border: '1px solid #e0e0e0',
            borderRadius: 8,
            color: '#666',
          }}
        >
          Ingen bevegelser funnet med valgte filtre.
        </div>
      ) : (
        <table
          style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}
        >
          <thead>
            <tr>
              <th style={thStyle}>Dato</th>
              <th style={thStyle}>Retning</th>
              <th style={thStyle}>Motpart</th>
              <th style={thStyle}>Beskrivelse</th>
              <th style={thStyle}>KID</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Belop</th>
              <th style={thStyle}>Status</th>
              <th style={thStyle}>Match-type</th>
            </tr>
          </thead>
          <tbody>
            {filtrerteBevegelser.map((bev: BankbevegelseDto) => {
              const erInn = bev.retning === BankbevegelseRetning.Inn;
              return (
                <tr
                  key={bev.id}
                  style={{
                    backgroundColor: bevegelseRadFarge(bev.status),
                  }}
                >
                  <td style={tdStyle}>{formatDato(bev.bokforingsdato)}</td>
                  <td style={tdStyle}>
                    <span
                      style={{
                        padding: '2px 6px',
                        borderRadius: 12,
                        fontSize: 11,
                        fontWeight: 600,
                        background: erInn ? '#e8f5e9' : '#ffebee',
                        color: erInn ? '#2e7d32' : '#c62828',
                      }}
                    >
                      {BankbevegelseRetningNavn[bev.retning]}
                    </span>
                  </td>
                  <td style={tdStyle}>{bev.motpart ?? ''}</td>
                  <td style={tdStyle}>{bev.beskrivelse ?? ''}</td>
                  <td style={{ ...tdStyle, fontFamily: 'monospace', fontSize: 12 }}>
                    {bev.kidNummer ?? ''}
                  </td>
                  <td
                    style={{
                      ...tdStyle,
                      textAlign: 'right',
                      fontFamily: 'monospace',
                      fontWeight: 600,
                      color: erInn ? '#2e7d32' : '#c62828',
                    }}
                  >
                    {erInn ? '' : '-'}{formatBelop(bev.belop)}
                  </td>
                  <td style={tdStyle}>
                    <span
                      style={{
                        padding: '2px 8px',
                        borderRadius: 12,
                        fontSize: 11,
                        fontWeight: 600,
                        ...statusBadgeFarge(bev.status),
                      }}
                    >
                      {BankbevegelseStatusNavn[bev.status]}
                    </span>
                  </td>
                  <td style={tdStyle}>
                    {bev.matcheType ? (
                      <span style={{ fontSize: 12 }}>
                        {MatcheTypeNavn[bev.matcheType]}
                        {bev.matcheKonfidens != null && (
                          <span style={{ color: '#999', marginLeft: 4 }}>
                            ({Math.round(bev.matcheKonfidens * 100)} %)
                          </span>
                        )}
                      </span>
                    ) : (
                      ''
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
}

function bevegelseRadFarge(status: string): string {
  switch (status) {
    case 'IkkeMatchet':
      return '#fff8f8';
    case 'Ignorert':
      return '#f5f5f5';
    default:
      return '#fff';
  }
}

function statusBadgeFarge(status: string): { backgroundColor: string; color: string } {
  switch (status) {
    case 'IkkeMatchet':
      return { backgroundColor: '#ffebee', color: '#c62828' };
    case 'AutoMatchet':
      return { backgroundColor: '#e8f5e9', color: '#2e7d32' };
    case 'ManueltMatchet':
      return { backgroundColor: '#e3f2fd', color: '#1565c0' };
    case 'Splittet':
      return { backgroundColor: '#f3e5f5', color: '#7b1fa2' };
    case 'Bokfort':
      return { backgroundColor: '#fff3e0', color: '#e65100' };
    case 'Ignorert':
      return { backgroundColor: '#f5f5f5', color: '#616161' };
    default:
      return { backgroundColor: '#f5f5f5', color: '#616161' };
  }
}

const selectStyle: React.CSSProperties = {
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  minWidth: 200,
};

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
