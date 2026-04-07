import { formatBelop } from '../utils/formatering';

export interface RegnskapsLinje {
  id?: string;
  dato?: string;
  referanse?: string;
  beskrivelse: string;
  debet: number;
  kredit: number;
  saldo?: number;
}

interface RegnskapsTabellProps {
  linjer: RegnskapsLinje[];
  /** Vis lOpende saldo-kolonne */
  visSaldo?: boolean;
  /** Vis sumrad nederst */
  visSum?: boolean;
  /** Vis dato-kolonne */
  visDato?: boolean;
  /** Vis referanse-kolonne */
  visReferanse?: boolean;
  /** Inngaende balanse (vises som forste rad hvis satt) */
  inngaendeBalanse?: number;
  /** Tittel over tabellen */
  tittel?: string;
}

const cellStyle: React.CSSProperties = {
  padding: '8px 12px',
  borderBottom: '1px solid #e0e0e0',
  textAlign: 'right',
  fontFamily: 'monospace',
  fontSize: 14,
};

const headerStyle: React.CSSProperties = {
  ...cellStyle,
  fontWeight: 700,
  borderBottom: '2px solid #333',
  fontFamily: 'inherit',
  backgroundColor: '#f8f8f8',
};

const sumCellStyle: React.CSSProperties = {
  ...cellStyle,
  fontWeight: 700,
  borderTop: '2px solid #333',
  borderBottom: 'none',
};

function belopCelle(verdi: number, style?: React.CSSProperties) {
  const erNegativ = verdi < 0;
  return (
    <td
      style={{
        ...cellStyle,
        ...style,
        color: erNegativ ? 'red' : 'inherit',
      }}
    >
      {verdi !== 0 ? formatBelop(verdi) : ''}
    </td>
  );
}

function saldoCelle(verdi: number, style?: React.CSSProperties) {
  const erNegativ = verdi < 0;
  return (
    <td
      style={{
        ...cellStyle,
        ...style,
        color: erNegativ ? 'red' : 'inherit',
        fontWeight: 600,
      }}
    >
      {formatBelop(verdi)}
    </td>
  );
}

export default function RegnskapsTabell({
  linjer,
  visSaldo = false,
  visSum = true,
  visDato = false,
  visReferanse = false,
  inngaendeBalanse,
  tittel,
}: RegnskapsTabellProps) {
  const sumDebet = linjer.reduce((sum, l) => sum + l.debet, 0);
  const sumKredit = linjer.reduce((sum, l) => sum + l.kredit, 0);

  return (
    <div>
      {tittel && <h3 style={{ marginBottom: 8 }}>{tittel}</h3>}
      <table
        style={{
          width: '100%',
          borderCollapse: 'collapse',
          border: '1px solid #e0e0e0',
        }}
      >
        <thead>
          <tr>
            {visDato && <th style={{ ...headerStyle, textAlign: 'left', width: 100 }}>Dato</th>}
            {visReferanse && (
              <th style={{ ...headerStyle, textAlign: 'left', width: 120 }}>Referanse</th>
            )}
            <th style={{ ...headerStyle, textAlign: 'left' }}>Beskrivelse</th>
            <th style={{ ...headerStyle, width: 140 }}>Debet</th>
            <th style={{ ...headerStyle, width: 140 }}>Kredit</th>
            {visSaldo && <th style={{ ...headerStyle, width: 150 }}>Saldo</th>}
          </tr>
        </thead>
        <tbody>
          {inngaendeBalanse !== undefined && (
            <tr style={{ backgroundColor: '#f5f5f5' }}>
              {visDato && <td style={{ ...cellStyle, textAlign: 'left' }} />}
              {visReferanse && <td style={{ ...cellStyle, textAlign: 'left' }} />}
              <td style={{ ...cellStyle, textAlign: 'left', fontStyle: 'italic' }}>
                Inngående balanse
              </td>
              <td style={cellStyle} />
              <td style={cellStyle} />
              {visSaldo && saldoCelle(inngaendeBalanse)}
            </tr>
          )}
          {linjer.map((linje, index) => (
            <tr
              key={linje.id ?? index}
              style={{ backgroundColor: index % 2 === 0 ? '#fff' : '#fafafa' }}
            >
              {visDato && (
                <td style={{ ...cellStyle, textAlign: 'left' }}>{linje.dato ?? ''}</td>
              )}
              {visReferanse && (
                <td style={{ ...cellStyle, textAlign: 'left', fontSize: 13 }}>
                  {linje.referanse ?? ''}
                </td>
              )}
              <td style={{ ...cellStyle, textAlign: 'left' }}>{linje.beskrivelse}</td>
              {belopCelle(linje.debet)}
              {belopCelle(linje.kredit)}
              {visSaldo && linje.saldo !== undefined && saldoCelle(linje.saldo)}
              {visSaldo && linje.saldo === undefined && <td style={cellStyle} />}
            </tr>
          ))}
        </tbody>
        {visSum && (
          <tfoot>
            <tr style={{ backgroundColor: '#f8f8f8' }}>
              {visDato && <td style={sumCellStyle} />}
              {visReferanse && <td style={sumCellStyle} />}
              <td style={{ ...sumCellStyle, textAlign: 'left' }}>Sum</td>
              <td style={sumCellStyle}>{formatBelop(sumDebet)}</td>
              <td style={sumCellStyle}>{formatBelop(sumKredit)}</td>
              {visSaldo && (
                <td
                  style={{
                    ...sumCellStyle,
                    fontWeight: 700,
                    color: sumDebet - sumKredit < 0 ? 'red' : 'inherit',
                  }}
                >
                  {linjer.length > 0 && linjer[linjer.length - 1].saldo !== undefined
                    ? formatBelop(linjer[linjer.length - 1].saldo!)
                    : ''}
                </td>
              )}
            </tr>
          </tfoot>
        )}
      </table>
    </div>
  );
}
