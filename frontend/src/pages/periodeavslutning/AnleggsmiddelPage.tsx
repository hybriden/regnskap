import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  useAnleggsmidler,
  useAnleggsmiddel,
  useOpprettAnleggsmiddel,
} from '../../hooks/api/usePeriodeavslutning';
import type {
  AnleggsmiddelDto,
  OpprettAnleggsmiddelRequest,
  AvskrivningHistorikkDto,
} from '../../types/periodeavslutning';
import { formatBelop, formatDato } from '../../utils/formatering';

const currentYear = new Date().getFullYear();

const periodeNavn: Record<number, string> = {
  1: 'Januar', 2: 'Februar', 3: 'Mars', 4: 'April',
  5: 'Mai', 6: 'Juni', 7: 'Juli', 8: 'August',
  9: 'September', 10: 'Oktober', 11: 'November', 12: 'Desember',
};

const tomtSkjema: OpprettAnleggsmiddelRequest = {
  navn: '',
  beskrivelse: '',
  anskaffelsesdato: `${currentYear}-01-01`,
  anskaffelseskostnad: 0,
  restverdi: 0,
  levetidManeder: 60,
  balanseKontonummer: '',
  avskrivningsKontonummer: '',
  akkumulertAvskrivningKontonummer: '',
};

function AnleggsmiddelDetaljerView() {
  const { id } = useParams<{ id: string }>();
  const { data: anleggsmiddel, isLoading, error } = useAnleggsmiddel(id ?? '');

  if (isLoading) return <p style={{ padding: 24 }}>Laster anleggsmiddel...</p>;
  if (error || !anleggsmiddel) {
    return (
      <div style={{ padding: 24 }}>
        <Link to="/periodeavslutning/anleggsmidler" style={{ color: '#0066cc', fontSize: 13, textDecoration: 'none' }}>
          &larr; Tilbake til anleggsmidler
        </Link>
        <p style={{ color: 'red', marginTop: 12 }}>Kunne ikke laste anleggsmiddel.</p>
      </div>
    );
  }

  const am = anleggsmiddel;
  const avskrivningsProsent = am.anskaffelseskostnad > 0
    ? ((am.akkumulertAvskrivning / am.avskrivningsgrunnlag) * 100).toFixed(1)
    : '0';

  return (
    <div style={{ padding: 24, maxWidth: 1000, margin: '0 auto' }}>
      <Link to="/periodeavslutning/anleggsmidler" style={{ color: '#0066cc', fontSize: 13, textDecoration: 'none' }}>
        &larr; Tilbake til anleggsmidler
      </Link>

      <h1 style={{ marginTop: 12 }}>{am.navn}</h1>
      {am.beskrivelse && <p style={{ color: '#666', marginTop: -8 }}>{am.beskrivelse}</p>}

      {/* Status-badge */}
      <div style={{ marginBottom: 24 }}>
        {am.erFulltAvskrevet ? (
          <span style={{ padding: '4px 12px', borderRadius: 12, fontSize: 13, fontWeight: 600, background: '#fff3e0', color: '#e65100' }}>
            Fullt avskrevet
          </span>
        ) : am.erAktivt ? (
          <span style={{ padding: '4px 12px', borderRadius: 12, fontSize: 13, fontWeight: 600, background: '#e8f5e9', color: '#2e7d32' }}>
            Aktiv
          </span>
        ) : (
          <span style={{ padding: '4px 12px', borderRadius: 12, fontSize: 13, fontWeight: 600, background: '#ffebee', color: '#c62828' }}>
            Utrangert {am.utrangeringsDato ? formatDato(am.utrangeringsDato) : ''}
          </span>
        )}
      </div>

      {/* Verdier */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))', gap: 16, marginBottom: 32 }}>
        <InfoBoks label="Anskaffelsesdato" verdi={formatDato(am.anskaffelsesdato)} />
        <InfoBoks label="Anskaffelseskostnad" verdi={formatBelop(am.anskaffelseskostnad)} mono />
        <InfoBoks label="Restverdi" verdi={formatBelop(am.restverdi)} mono />
        <InfoBoks label="Avskrivningsgrunnlag" verdi={formatBelop(am.avskrivningsgrunnlag)} mono />
        <InfoBoks label="Levetid" verdi={`${am.levetidManeder} mnd (${(am.levetidManeder / 12).toFixed(1)} ar)`} />
        <InfoBoks label="Manedlig avskrivning" verdi={formatBelop(am.manedligAvskrivning)} mono />
        <InfoBoks label="Arlig avskrivning" verdi={formatBelop(am.arligAvskrivning)} mono />
        <InfoBoks label="Akkumulert avskrivning" verdi={`${formatBelop(am.akkumulertAvskrivning)} (${avskrivningsProsent} %)`} mono />
        <InfoBoks label="Bokfort verdi" verdi={formatBelop(am.bokfortVerdi)} mono fremhevet />
        <InfoBoks label="Gjenvaerende avskrivning" verdi={formatBelop(am.gjenvaerendeAvskrivning)} mono />
      </div>

      {/* Kontoer */}
      <h2 style={{ fontSize: 16, marginBottom: 12 }}>Kontoer</h2>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(250px, 1fr))', gap: 16, marginBottom: 32 }}>
        <InfoBoks label="Balansekonto" verdi={am.balanseKontonummer} />
        <InfoBoks label="Avskrivningskonto" verdi={am.avskrivningsKontonummer} />
        <InfoBoks label="Akkumulert avskr.konto" verdi={am.akkumulertAvskrivningKontonummer} />
        {am.avdelingskode && <InfoBoks label="Avdelingskode" verdi={am.avdelingskode} />}
        {am.prosjektkode && <InfoBoks label="Prosjektkode" verdi={am.prosjektkode} />}
      </div>

      {/* Avskrivningshistorikk */}
      <h2 style={{ fontSize: 16, marginBottom: 12 }}>Avskrivningshistorikk</h2>
      {am.avskrivninger.length === 0 ? (
        <div style={{ padding: 24, textAlign: 'center', border: '1px solid #e0e0e0', borderRadius: 8, color: '#666' }}>
          Ingen avskrivninger bokfort enna.
        </div>
      ) : (
        <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
          <thead>
            <tr>
              <th style={thStyle}>Periode</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Belop</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Akkumulert etter</th>
              <th style={{ ...thStyle, textAlign: 'right' }}>Bokfort verdi etter</th>
              <th style={thStyle}>Bilag</th>
            </tr>
          </thead>
          <tbody>
            {am.avskrivninger.map((h: AvskrivningHistorikkDto) => (
              <tr key={h.id}>
                <td style={tdStyle}>
                  {periodeNavn[h.periode]} {h.ar}
                </td>
                <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(h.belop)}
                </td>
                <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(h.akkumulertEtter)}
                </td>
                <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                  {formatBelop(h.bokfortVerdiEtter)}
                </td>
                <td style={tdStyle}>
                  <Link to={`/bilag/${h.bilagId}`} style={{ color: '#0066cc', textDecoration: 'none', fontSize: 13 }}>
                    Se bilag
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

function InfoBoks({ label, verdi, mono, fremhevet }: { label: string; verdi: string; mono?: boolean; fremhevet?: boolean }) {
  return (
    <div style={{
      padding: 12,
      border: `1px solid ${fremhevet ? '#1565c0' : '#e0e0e0'}`,
      borderRadius: 8,
      background: fremhevet ? '#e3f2fd' : '#fff',
    }}>
      <div style={{ fontSize: 12, color: '#666', marginBottom: 4 }}>{label}</div>
      <div style={{ fontSize: 15, fontWeight: fremhevet ? 700 : 600, fontFamily: mono ? 'monospace' : 'inherit' }}>
        {verdi}
      </div>
    </div>
  );
}

function AnleggsmiddelListeView() {
  const [visAktive, setVisAktive] = useState(true);
  const [visSkjema, setVisSkjema] = useState(false);
  const [skjema, setSkjema] = useState<OpprettAnleggsmiddelRequest>({ ...tomtSkjema });

  const { data: anleggsmidler, isLoading } = useAnleggsmidler(visAktive ? true : undefined);
  const opprettAnleggsmiddel = useOpprettAnleggsmiddel();

  function handleOpprett() {
    if (!skjema.navn || skjema.anskaffelseskostnad <= 0 || !skjema.balanseKontonummer || !skjema.avskrivningsKontonummer || !skjema.akkumulertAvskrivningKontonummer) {
      return;
    }
    opprettAnleggsmiddel.mutate(skjema, {
      onSuccess: () => {
        setSkjema({ ...tomtSkjema });
        setVisSkjema(false);
      },
    });
  }

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Link to="/periodeavslutning" style={{ color: '#0066cc', fontSize: 13, textDecoration: 'none' }}>
        &larr; Tilbake til periodeoversikt
      </Link>

      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 12, marginBottom: 24 }}>
        <h1 style={{ margin: 0 }}>Anleggsmidler</h1>
        <button
          onClick={() => setVisSkjema(!visSkjema)}
          style={visSkjema ? secondaryButtonStyle : primaryButtonStyle}
        >
          {visSkjema ? 'Avbryt' : 'Registrer nytt'}
        </button>
      </div>

      {/* Registreringsskjema */}
      {visSkjema && (
        <div style={{ border: '1px solid #e0e0e0', borderRadius: 8, padding: 24, marginBottom: 24, background: '#fafafa' }}>
          <h2 style={{ fontSize: 18, marginTop: 0, marginBottom: 16 }}>Nytt anleggsmiddel</h2>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
            <div style={{ gridColumn: '1 / -1' }}>
              <label style={labelStyle}>Navn</label>
              <input
                type="text"
                value={skjema.navn}
                onChange={(e) => setSkjema({ ...skjema, navn: e.target.value })}
                placeholder="F.eks. Kontormaskin HP LaserJet"
                style={{ ...inputStyle, width: '100%' }}
              />
            </div>

            <div style={{ gridColumn: '1 / -1' }}>
              <label style={labelStyle}>Beskrivelse (valgfritt)</label>
              <input
                type="text"
                value={skjema.beskrivelse ?? ''}
                onChange={(e) => setSkjema({ ...skjema, beskrivelse: e.target.value || undefined })}
                placeholder="Beskrivelse av anleggsmiddelet"
                style={{ ...inputStyle, width: '100%' }}
              />
            </div>

            <div>
              <label style={labelStyle}>Anskaffelsesdato</label>
              <input
                type="date"
                value={skjema.anskaffelsesdato}
                onChange={(e) => setSkjema({ ...skjema, anskaffelsesdato: e.target.value })}
                style={inputStyle}
              />
            </div>

            <div>
              <label style={labelStyle}>Levetid (maneder)</label>
              <input
                type="number"
                value={skjema.levetidManeder}
                onChange={(e) => setSkjema({ ...skjema, levetidManeder: parseInt(e.target.value) || 0 })}
                min={1}
                style={inputStyle}
              />
              <span style={{ fontSize: 12, color: '#666', marginLeft: 8 }}>
                = {(skjema.levetidManeder / 12).toFixed(1)} ar
              </span>
            </div>

            <div>
              <label style={labelStyle}>Anskaffelseskostnad</label>
              <input
                type="number"
                value={skjema.anskaffelseskostnad || ''}
                onChange={(e) => setSkjema({ ...skjema, anskaffelseskostnad: parseFloat(e.target.value) || 0 })}
                min={0}
                step={0.01}
                style={inputStyle}
              />
            </div>

            <div>
              <label style={labelStyle}>Restverdi (utrangeringsverdi)</label>
              <input
                type="number"
                value={skjema.restverdi || ''}
                onChange={(e) => setSkjema({ ...skjema, restverdi: parseFloat(e.target.value) || 0 })}
                min={0}
                step={0.01}
                style={inputStyle}
              />
            </div>

            <div>
              <label style={labelStyle}>Balansekonto</label>
              <input
                type="text"
                value={skjema.balanseKontonummer}
                onChange={(e) => setSkjema({ ...skjema, balanseKontonummer: e.target.value })}
                placeholder="F.eks. 1200"
                style={inputStyle}
              />
            </div>

            <div>
              <label style={labelStyle}>Avskrivningskonto</label>
              <input
                type="text"
                value={skjema.avskrivningsKontonummer}
                onChange={(e) => setSkjema({ ...skjema, avskrivningsKontonummer: e.target.value })}
                placeholder="F.eks. 6000"
                style={inputStyle}
              />
            </div>

            <div>
              <label style={labelStyle}>Akkumulert avskrivning-konto</label>
              <input
                type="text"
                value={skjema.akkumulertAvskrivningKontonummer}
                onChange={(e) => setSkjema({ ...skjema, akkumulertAvskrivningKontonummer: e.target.value })}
                placeholder="F.eks. 1209"
                style={inputStyle}
              />
            </div>

            <div>
              <label style={labelStyle}>Avdelingskode (valgfritt)</label>
              <input
                type="text"
                value={skjema.avdelingskode ?? ''}
                onChange={(e) => setSkjema({ ...skjema, avdelingskode: e.target.value || undefined })}
                style={inputStyle}
              />
            </div>

            <div>
              <label style={labelStyle}>Prosjektkode (valgfritt)</label>
              <input
                type="text"
                value={skjema.prosjektkode ?? ''}
                onChange={(e) => setSkjema({ ...skjema, prosjektkode: e.target.value || undefined })}
                style={inputStyle}
              />
            </div>
          </div>

          {/* Forhands-beregning */}
          {skjema.anskaffelseskostnad > 0 && skjema.levetidManeder > 0 && (
            <div style={{
              marginTop: 16,
              padding: 12,
              background: '#e3f2fd',
              borderRadius: 8,
              fontSize: 14,
            }}>
              <strong>Beregnet:</strong>{' '}
              Avskrivningsgrunnlag: {formatBelop(skjema.anskaffelseskostnad - skjema.restverdi)} |{' '}
              Manedlig avskrivning: {formatBelop(Math.round(((skjema.anskaffelseskostnad - skjema.restverdi) / skjema.levetidManeder) * 100) / 100)} |{' '}
              Arlig avskrivning: {formatBelop(Math.round(((skjema.anskaffelseskostnad - skjema.restverdi) / skjema.levetidManeder) * 12 * 100) / 100)}
            </div>
          )}

          <div style={{ display: 'flex', gap: 12, marginTop: 24 }}>
            <button onClick={() => setVisSkjema(false)} style={secondaryButtonStyle}>
              Avbryt
            </button>
            <button
              onClick={handleOpprett}
              disabled={opprettAnleggsmiddel.isPending || !skjema.navn || skjema.anskaffelseskostnad <= 0}
              style={{
                ...primaryButtonStyle,
                opacity: (!skjema.navn || skjema.anskaffelseskostnad <= 0) ? 0.5 : 1,
              }}
            >
              {opprettAnleggsmiddel.isPending ? 'Registrerer...' : 'Registrer anleggsmiddel'}
            </button>
          </div>

          {opprettAnleggsmiddel.isError && (
            <p style={{ color: 'red', marginTop: 12, fontSize: 14 }}>Feil ved registrering av anleggsmiddel.</p>
          )}
        </div>
      )}

      {/* Filter */}
      <div style={{ marginBottom: 16, display: 'flex', alignItems: 'center', gap: 12 }}>
        <label style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 14 }}>
          <input
            type="checkbox"
            checked={visAktive}
            onChange={(e) => setVisAktive(e.target.checked)}
          />
          Vis kun aktive
        </label>
      </div>

      {/* Liste */}
      {isLoading ? (
        <p>Laster anleggsmidler...</p>
      ) : !anleggsmidler || anleggsmidler.length === 0 ? (
        <div style={{ padding: 32, textAlign: 'center', border: '1px solid #e0e0e0', borderRadius: 8 }}>
          <p style={{ color: '#666' }}>Ingen anleggsmidler funnet.</p>
        </div>
      ) : (
        <>
          {/* Sammendrag */}
          <div style={{ display: 'flex', gap: 16, marginBottom: 16, flexWrap: 'wrap' }}>
            <div style={sammendragBoks}>
              <div style={{ fontSize: 24, fontWeight: 700 }}>{anleggsmidler.length}</div>
              <div style={{ color: '#666', fontSize: 13 }}>Anleggsmidler</div>
            </div>
            <div style={sammendragBoks}>
              <div style={{ fontSize: 14, fontWeight: 700, fontFamily: 'monospace' }}>
                {formatBelop(anleggsmidler.reduce((sum: number, am: AnleggsmiddelDto) => sum + am.anskaffelseskostnad, 0))}
              </div>
              <div style={{ color: '#666', fontSize: 13 }}>Total anskaffelseskost</div>
            </div>
            <div style={sammendragBoks}>
              <div style={{ fontSize: 14, fontWeight: 700, fontFamily: 'monospace' }}>
                {formatBelop(anleggsmidler.reduce((sum: number, am: AnleggsmiddelDto) => sum + am.bokfortVerdi, 0))}
              </div>
              <div style={{ color: '#666', fontSize: 13 }}>Total bokfort verdi</div>
            </div>
            <div style={sammendragBoks}>
              <div style={{ fontSize: 14, fontWeight: 700, fontFamily: 'monospace' }}>
                {formatBelop(anleggsmidler.reduce((sum: number, am: AnleggsmiddelDto) => sum + am.manedligAvskrivning, 0))}
              </div>
              <div style={{ color: '#666', fontSize: 13 }}>Mnd. avskrivning</div>
            </div>
          </div>

          <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #e0e0e0' }}>
            <thead>
              <tr>
                <th style={thStyle}>Navn</th>
                <th style={thStyle}>Anskaffet</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Anskaffelseskost</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Akkumulert avskr.</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Bokfort verdi</th>
                <th style={{ ...thStyle, textAlign: 'right' }}>Mnd. avskrivning</th>
                <th style={thStyle}>Levetid</th>
                <th style={thStyle}>Status</th>
              </tr>
            </thead>
            <tbody>
              {anleggsmidler.map((am: AnleggsmiddelDto) => (
                <tr key={am.id}>
                  <td style={tdStyle}>
                    <Link to={`/periodeavslutning/anleggsmidler/${am.id}`} style={{ color: '#0066cc', textDecoration: 'none', fontWeight: 600 }}>
                      {am.navn}
                    </Link>
                    {am.beskrivelse && (
                      <div style={{ fontSize: 12, color: '#999' }}>{am.beskrivelse}</div>
                    )}
                  </td>
                  <td style={tdStyle}>{formatDato(am.anskaffelsesdato)}</td>
                  <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                    {formatBelop(am.anskaffelseskostnad)}
                  </td>
                  <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                    {formatBelop(am.akkumulertAvskrivning)}
                  </td>
                  <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace', fontWeight: 600 }}>
                    {formatBelop(am.bokfortVerdi)}
                  </td>
                  <td style={{ ...tdStyle, textAlign: 'right', fontFamily: 'monospace' }}>
                    {formatBelop(am.manedligAvskrivning)}
                  </td>
                  <td style={tdStyle}>
                    <span style={{ fontSize: 13 }}>
                      {am.levetidManeder} mnd
                    </span>
                  </td>
                  <td style={tdStyle}>
                    {am.erFulltAvskrevet ? (
                      <span style={{ padding: '2px 8px', borderRadius: 12, fontSize: 12, fontWeight: 600, background: '#fff3e0', color: '#e65100' }}>
                        Fullt avskrevet
                      </span>
                    ) : !am.erAktivt ? (
                      <span style={{ padding: '2px 8px', borderRadius: 12, fontSize: 12, fontWeight: 600, background: '#ffebee', color: '#c62828' }}>
                        Utrangert
                      </span>
                    ) : (
                      <span style={{ padding: '2px 8px', borderRadius: 12, fontSize: 12, fontWeight: 600, background: '#e8f5e9', color: '#2e7d32' }}>
                        Aktiv
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}
    </div>
  );
}

export default function AnleggsmiddelPage() {
  const { id } = useParams<{ id: string }>();

  if (id) {
    return <AnleggsmiddelDetaljerView />;
  }

  return <AnleggsmiddelListeView />;
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

const inputStyle: React.CSSProperties = {
  padding: '8px 12px',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  width: 200,
};

const labelStyle: React.CSSProperties = {
  display: 'block',
  fontWeight: 600,
  marginBottom: 4,
  fontSize: 14,
};

const primaryButtonStyle: React.CSSProperties = {
  padding: '10px 24px',
  background: '#0066cc',
  color: '#fff',
  border: 'none',
  borderRadius: 4,
  fontSize: 14,
  cursor: 'pointer',
  textDecoration: 'none',
  display: 'inline-block',
};

const secondaryButtonStyle: React.CSSProperties = {
  padding: '10px 24px',
  background: '#f5f5f5',
  color: '#333',
  border: '1px solid #ccc',
  borderRadius: 4,
  fontSize: 14,
  cursor: 'pointer',
};

const sammendragBoks: React.CSSProperties = {
  padding: 16,
  border: '1px solid #e0e0e0',
  borderRadius: 8,
  minWidth: 160,
  textAlign: 'center',
};

// selectStyle kept for future use in filter dropdowns
