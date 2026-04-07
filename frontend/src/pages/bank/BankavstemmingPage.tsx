import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  useBankavstemming,
  useBankkonto,
  useBankbevegelser,
  useAutoMatch,
  useManuellMatch,
  useIgnorerBevegelse,
  useFjernMatch,
  useMatchForslag,
} from '../../hooks/api/useBank';
import {
  BankbevegelseStatus,
  BankbevegelseStatusNavn,
  BankbevegelseRetningNavn,
  MatcheTypeNavn,
  AvstemmingStatusNavn,
} from '../../types/bank';
import type {
  BankbevegelseDto,
  MatcheForslagDto,
  AvstemmingStatus,
} from '../../types/bank';
import { formatBelop, formatDato } from '../../utils/formatering';

export default function BankavstemmingPage() {
  const { kontoId } = useParams<{ kontoId: string }>();
  const bankkontoId = kontoId ?? '';

  const { data: konto } = useBankkonto(bankkontoId);
  const { data: avstemming, isLoading: lasterAvstemming } = useBankavstemming(bankkontoId);
  const { data: bevegelser, isLoading: lasterBevegelser } = useBankbevegelser({
    bankkontoId,
  });

  const autoMatch = useAutoMatch();
  const manuellMatch = useManuellMatch();
  const ignorerBevegelse = useIgnorerBevegelse();
  const fjernMatch = useFjernMatch();

  const [valgtBevegelseId, setValgtBevegelseId] = useState<string | null>(null);

  const umatchede = bevegelser?.filter(
    (b) => b.status === BankbevegelseStatus.IkkeMatchet,
  ) ?? [];
  const matchede = bevegelser?.filter(
    (b) => b.status !== BankbevegelseStatus.IkkeMatchet,
  ) ?? [];

  function handleAutoMatch() {
    autoMatch.mutate(bankkontoId);
  }

  function handleIgnorer(id: string) {
    ignorerBevegelse.mutate(id);
  }

  function handleFjernMatch(id: string) {
    fjernMatch.mutate(id);
  }

  function avstemmingStatusFarge(status: AvstemmingStatus): {
    backgroundColor: string;
    color: string;
  } {
    switch (status) {
      case 'Avstemt':
        return { backgroundColor: '#e8f5e9', color: '#2e7d32' };
      case 'AvstemtMedDifferanse':
        return { backgroundColor: '#fff3e0', color: '#e65100' };
      default:
        return { backgroundColor: '#e3f2fd', color: '#1565c0' };
    }
  }

  const isLoading = lasterAvstemming || lasterBevegelser;

  return (
    <div style={{ padding: 24, maxWidth: 1400, margin: '0 auto' }}>
      <div style={{ marginBottom: 16 }}>
        <Link to="/bank" style={{ color: '#0066cc', textDecoration: 'none', fontSize: 14 }}>
          &larr; Tilbake til bankkontoer
        </Link>
      </div>

      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: 24,
        }}
      >
        <div>
          <h1 style={{ margin: 0 }}>Bankavstemming</h1>
          {konto && (
            <p style={{ margin: '4px 0 0', color: '#666', fontSize: 14 }}>
              {konto.kontonummer} &ndash; {konto.beskrivelse} ({konto.banknavn})
            </p>
          )}
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button
            onClick={handleAutoMatch}
            disabled={autoMatch.isPending}
            style={{
              padding: '8px 16px',
              background: '#4caf50',
              color: '#fff',
              border: 'none',
              borderRadius: 4,
              fontSize: 14,
              cursor: 'pointer',
            }}
          >
            {autoMatch.isPending ? 'Matcher...' : 'Kjoer automatisk matching'}
          </button>
          <Link
            to={`/bank/rapport/${bankkontoId}`}
            style={{
              padding: '8px 16px',
              background: '#0066cc',
              color: '#fff',
              borderRadius: 4,
              textDecoration: 'none',
              fontSize: 14,
            }}
          >
            Avstemmingsrapport
          </Link>
        </div>
      </div>

      {/* Avstemmingsstatus-oversikt */}
      {avstemming && (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(4, 1fr)',
            gap: 16,
            marginBottom: 24,
          }}
        >
          <StatusKort
            tittel="Saldo hovedbok"
            verdi={formatBelop(avstemming.saldoHovedbok)}
          />
          <StatusKort
            tittel="Saldo bank"
            verdi={formatBelop(avstemming.saldoBank)}
          />
          <StatusKort
            tittel="Differanse"
            verdi={formatBelop(avstemming.differanse)}
            farge={avstemming.differanse !== 0 ? '#e65100' : '#2e7d32'}
          />
          <div
            style={{
              padding: 16,
              border: '1px solid #e0e0e0',
              borderRadius: 8,
              textAlign: 'center',
            }}
          >
            <div style={{ fontSize: 12, color: '#666', marginBottom: 4 }}>Status</div>
            <span
              style={{
                padding: '4px 12px',
                borderRadius: 12,
                fontSize: 14,
                fontWeight: 600,
                ...avstemmingStatusFarge(avstemming.status),
              }}
            >
              {AvstemmingStatusNavn[avstemming.status]}
            </span>
            <div style={{ fontSize: 12, color: '#666', marginTop: 8 }}>
              {avstemming.antallMatchedeBevegelser} matchet /{' '}
              {avstemming.antallUmatchedeBevegelser} gjenstaaende
            </div>
          </div>
        </div>
      )}

      {autoMatch.isSuccess && (
        <div
          style={{
            padding: 12,
            background: '#e8f5e9',
            borderRadius: 4,
            marginBottom: 16,
            fontSize: 13,
            color: '#2e7d32',
          }}
        >
          Automatisk matching fullfort.
        </div>
      )}

      {isLoading ? (
        <p>Laster avstemming...</p>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24 }}>
          {/* Venstre: Umatchede bankbevegelser */}
          <div>
            <h2 style={{ marginBottom: 12, fontSize: 16 }}>
              Umatchede bankbevegelser ({umatchede.length})
            </h2>
            {umatchede.length === 0 ? (
              <div
                style={{
                  padding: 24,
                  textAlign: 'center',
                  border: '1px solid #e0e0e0',
                  borderRadius: 8,
                  color: '#666',
                }}
              >
                Alle bevegelser er matchet!
              </div>
            ) : (
              <div style={{ maxHeight: 600, overflowY: 'auto' }}>
                {umatchede.map((bev) => (
                  <BevegelseKort
                    key={bev.id}
                    bevegelse={bev}
                    erValgt={valgtBevegelseId === bev.id}
                    onVelg={() =>
                      setValgtBevegelseId(valgtBevegelseId === bev.id ? null : bev.id)
                    }
                    onIgnorer={() => handleIgnorer(bev.id)}
                  />
                ))}
              </div>
            )}
          </div>

          {/* Hoyre: Match-forslag eller matchede bevegelser */}
          <div>
            {valgtBevegelseId ? (
              <MatchForslagPanel
                bankbevegelseId={valgtBevegelseId}
                onMatch={(forslagId, type) => {
                  const forslag = type; // forslag-data
                  manuellMatch.mutate(
                    {
                      bankbevegelseId: valgtBevegelseId,
                      request: {
                        kundeFakturaId: forslag.kundeFakturaId ?? undefined,
                        leverandorFakturaId: forslag.leverandorFakturaId ?? undefined,
                        bilagId: forslag.bilagId ?? undefined,
                      },
                    },
                    {
                      onSuccess: () => setValgtBevegelseId(null),
                    },
                  );
                }}
                isPending={manuellMatch.isPending}
              />
            ) : (
              <div>
                <h2 style={{ marginBottom: 12, fontSize: 16 }}>
                  Matchede bevegelser ({matchede.length})
                </h2>
                {matchede.length === 0 ? (
                  <div
                    style={{
                      padding: 24,
                      textAlign: 'center',
                      border: '1px solid #e0e0e0',
                      borderRadius: 8,
                      color: '#666',
                    }}
                  >
                    Ingen matchede bevegelser enna.
                  </div>
                ) : (
                  <div style={{ maxHeight: 600, overflowY: 'auto' }}>
                    {matchede.map((bev) => (
                      <MatchetBevegelseKort
                        key={bev.id}
                        bevegelse={bev}
                        onFjernMatch={() => handleFjernMatch(bev.id)}
                      />
                    ))}
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

// --- Sub-komponenter ---

function StatusKort({
  tittel,
  verdi,
  farge,
}: {
  tittel: string;
  verdi: string;
  farge?: string;
}) {
  return (
    <div
      style={{
        padding: 16,
        border: '1px solid #e0e0e0',
        borderRadius: 8,
        textAlign: 'center',
      }}
    >
      <div style={{ fontSize: 12, color: '#666', marginBottom: 4 }}>{tittel}</div>
      <div
        style={{
          fontSize: 20,
          fontWeight: 700,
          fontFamily: 'monospace',
          color: farge ?? '#333',
        }}
      >
        {verdi}
      </div>
    </div>
  );
}

function BevegelseKort({
  bevegelse,
  erValgt,
  onVelg,
  onIgnorer,
}: {
  bevegelse: BankbevegelseDto;
  erValgt: boolean;
  onVelg: () => void;
  onIgnorer: () => void;
}) {
  const erInn = bevegelse.retning === 'Inn';
  return (
    <div
      onClick={onVelg}
      style={{
        padding: 12,
        border: erValgt ? '2px solid #0066cc' : '1px solid #e0e0e0',
        borderRadius: 8,
        marginBottom: 8,
        background: erValgt ? '#e3f2fd' : '#fff',
        cursor: 'pointer',
      }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
        <span style={{ fontSize: 13, color: '#666' }}>
          {formatDato(bevegelse.bokforingsdato)}
        </span>
        <span
          style={{
            fontWeight: 700,
            fontFamily: 'monospace',
            color: erInn ? '#2e7d32' : '#c62828',
          }}
        >
          {erInn ? '+' : '-'} {formatBelop(bevegelse.belop)}
        </span>
      </div>
      <div style={{ fontWeight: 600, fontSize: 14 }}>
        {bevegelse.motpart ?? bevegelse.beskrivelse ?? 'Ukjent'}
      </div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 4 }}>
        <span style={{ fontSize: 12, color: '#999' }}>
          {BankbevegelseRetningNavn[bevegelse.retning]}
          {bevegelse.kidNummer && ` | KID: ${bevegelse.kidNummer}`}
        </span>
        <button
          onClick={(e) => {
            e.stopPropagation();
            onIgnorer();
          }}
          style={{
            padding: '2px 8px',
            background: '#f5f5f5',
            border: '1px solid #ccc',
            borderRadius: 4,
            fontSize: 11,
            cursor: 'pointer',
            color: '#666',
          }}
        >
          Ignorer
        </button>
      </div>
    </div>
  );
}

function MatchetBevegelseKort({
  bevegelse,
  onFjernMatch,
}: {
  bevegelse: BankbevegelseDto;
  onFjernMatch: () => void;
}) {
  const erInn = bevegelse.retning === 'Inn';
  return (
    <div
      style={{
        padding: 12,
        border: '1px solid #e0e0e0',
        borderRadius: 8,
        marginBottom: 8,
        background: '#f9fbe7',
      }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
        <span style={{ fontSize: 13, color: '#666' }}>
          {formatDato(bevegelse.bokforingsdato)}
        </span>
        <span
          style={{
            fontWeight: 700,
            fontFamily: 'monospace',
            color: erInn ? '#2e7d32' : '#c62828',
          }}
        >
          {erInn ? '+' : '-'} {formatBelop(bevegelse.belop)}
        </span>
      </div>
      <div style={{ fontWeight: 600, fontSize: 14 }}>
        {bevegelse.motpart ?? bevegelse.beskrivelse ?? 'Ukjent'}
      </div>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginTop: 4,
        }}
      >
        <div>
          <span
            style={{
              padding: '2px 6px',
              borderRadius: 12,
              fontSize: 11,
              fontWeight: 600,
              background: '#e8f5e9',
              color: '#2e7d32',
              marginRight: 4,
            }}
          >
            {BankbevegelseStatusNavn[bevegelse.status]}
          </span>
          {bevegelse.matcheType && (
            <span style={{ fontSize: 11, color: '#666' }}>
              ({MatcheTypeNavn[bevegelse.matcheType]})
            </span>
          )}
        </div>
        <button
          onClick={onFjernMatch}
          style={{
            padding: '2px 8px',
            background: '#ffebee',
            border: '1px solid #ef9a9a',
            borderRadius: 4,
            fontSize: 11,
            cursor: 'pointer',
            color: '#c62828',
          }}
        >
          Fjern match
        </button>
      </div>
      {bevegelse.matchinger.length > 0 && (
        <div style={{ marginTop: 8, paddingTop: 8, borderTop: '1px solid #e0e0e0' }}>
          {bevegelse.matchinger.map((m) => (
            <div key={m.id} style={{ fontSize: 12, color: '#555' }}>
              {m.kundeFakturaNummer && <>Faktura: {m.kundeFakturaNummer}</>}
              {m.leverandorFakturaNummer && <>Lev.faktura: {m.leverandorFakturaNummer}</>}
              {m.bilagNummer && <>Bilag: {m.bilagNummer}</>}
              {' '}&ndash; {formatBelop(m.belop)}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function MatchForslagPanel({
  bankbevegelseId,
  onMatch,
  isPending,
}: {
  bankbevegelseId: string;
  onMatch: (forslagId: string, forslag: MatcheForslagDto) => void;
  isPending: boolean;
}) {
  const { data: forslag, isLoading } = useMatchForslag(bankbevegelseId);

  return (
    <div>
      <h2 style={{ marginBottom: 12, fontSize: 16 }}>Match-forslag</h2>
      {isLoading ? (
        <p>Laster forslag...</p>
      ) : !forslag || forslag.length === 0 ? (
        <div
          style={{
            padding: 24,
            textAlign: 'center',
            border: '1px solid #e0e0e0',
            borderRadius: 8,
            color: '#666',
          }}
        >
          <p>Ingen automatiske forslag funnet.</p>
          <p style={{ fontSize: 13 }}>
            Du kan bokfore bevegelsen manuelt eller ignorere den.
          </p>
        </div>
      ) : (
        <div>
          {forslag.map((f, idx) => (
            <div
              key={idx}
              style={{
                padding: 12,
                border: '1px solid #e0e0e0',
                borderRadius: 8,
                marginBottom: 8,
                background: '#fff',
              }}
            >
              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  marginBottom: 4,
                }}
              >
                <span style={{ fontWeight: 600 }}>{f.beskrivelse}</span>
                <span
                  style={{
                    padding: '2px 8px',
                    borderRadius: 12,
                    fontSize: 11,
                    background: f.konfidens >= 0.8 ? '#e8f5e9' : '#fff3e0',
                    color: f.konfidens >= 0.8 ? '#2e7d32' : '#e65100',
                    fontWeight: 600,
                  }}
                >
                  {Math.round(f.konfidens * 100)} %
                </span>
              </div>
              <div style={{ fontSize: 12, color: '#666', marginBottom: 8 }}>
                {MatcheTypeNavn[f.matcheType]}
                {f.kundeFakturaNummer && <> | Faktura: {f.kundeFakturaNummer}</>}
                {f.leverandorFakturaNummer && <> | Lev.faktura: {f.leverandorFakturaNummer}</>}
                {f.bilagNummer && <> | Bilag: {f.bilagNummer}</>}
              </div>
              <button
                onClick={() => onMatch(String(idx), f)}
                disabled={isPending}
                style={{
                  padding: '4px 16px',
                  background: '#0066cc',
                  color: '#fff',
                  border: 'none',
                  borderRadius: 4,
                  fontSize: 13,
                  cursor: 'pointer',
                }}
              >
                {isPending ? 'Matcher...' : 'Match'}
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
