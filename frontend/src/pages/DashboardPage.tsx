import { Link } from 'react-router-dom';

const moduler = [
  {
    tittel: 'Kontoplan',
    beskrivelse: 'Kontoer, kontogrupper, MVA-koder (NS 4102)',
    lenke: '/kontoplan',
    ikon: '📋',
  },
  {
    tittel: 'Hovedbok',
    beskrivelse: 'Perioder, kontoutskrift, saldobalanse',
    lenke: '/hovedbok',
    ikon: '📖',
  },
  {
    tittel: 'Bilag',
    beskrivelse: 'Registrering, bilagsserier, vedlegg',
    lenke: '/bilag',
    ikon: '📝',
  },
  {
    tittel: 'MVA',
    beskrivelse: 'Oppgjor, avstemming, MVA-melding (RF-0002)',
    lenke: '/mva',
    ikon: '🏛️',
  },
  {
    tittel: 'Leverandorer',
    beskrivelse: 'Reskontro, faktura, betalingsforslag',
    lenke: '/leverandor',
    ikon: '🏭',
  },
  {
    tittel: 'Kunder',
    beskrivelse: 'Reskontro, innbetaling, purring, KID',
    lenke: '/kunde',
    ikon: '👥',
  },
  {
    tittel: 'Fakturering',
    beskrivelse: 'Opprett faktura, EHF/PEPPOL, PDF',
    lenke: '/faktura',
    ikon: '🧾',
  },
  {
    tittel: 'Bank',
    beskrivelse: 'Kontoutskrift, CAMT.053, avstemming',
    lenke: '/bank',
    ikon: '🏦',
  },
  {
    tittel: 'Rapporter',
    beskrivelse: 'Resultat, balanse, SAF-T, nokkeltall',
    lenke: '/rapporter',
    ikon: '📊',
  },
  {
    tittel: 'Periodeavslutning',
    beskrivelse: 'Manedslukking, arsavslutning, avskrivning',
    lenke: '/periodeavslutning',
    ikon: '🔒',
  },
];

export default function DashboardPage() {
  return (
    <div style={{ maxWidth: 1000, margin: '0 auto', padding: '2rem' }}>
      <header style={{ marginBottom: '2rem', borderBottom: '2px solid #1a56db', paddingBottom: '1rem' }}>
        <h1 style={{ margin: 0, fontSize: '1.8rem', color: '#1a56db' }}>Regnskapssystem</h1>
        <p style={{ margin: '0.5rem 0 0', color: '#666' }}>
          Norsk regnskap med dobbelt bokholderi, SAF-T, MVA og EHF
        </p>
      </header>

      <div style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
        gap: '1rem',
      }}>
        {moduler.map((m) => (
          <Link
            key={m.lenke}
            to={m.lenke}
            style={{
              display: 'block',
              padding: '1.25rem',
              border: '1px solid #e5e7eb',
              borderRadius: '8px',
              textDecoration: 'none',
              color: 'inherit',
              transition: 'border-color 0.15s, box-shadow 0.15s',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.borderColor = '#1a56db';
              e.currentTarget.style.boxShadow = '0 2px 8px rgba(26,86,219,0.1)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.borderColor = '#e5e7eb';
              e.currentTarget.style.boxShadow = 'none';
            }}
          >
            <div style={{ fontSize: '1.5rem', marginBottom: '0.5rem' }}>{m.ikon}</div>
            <h2 style={{ margin: '0 0 0.25rem', fontSize: '1.1rem', color: '#1a56db' }}>{m.tittel}</h2>
            <p style={{ margin: 0, fontSize: '0.85rem', color: '#666' }}>{m.beskrivelse}</p>
          </Link>
        ))}
      </div>

      <footer style={{ marginTop: '3rem', padding: '1rem 0', borderTop: '1px solid #e5e7eb', color: '#999', fontSize: '0.8rem' }}>
        ASP.NET Core 10 + React 19 + TypeScript — 373 tester
      </footer>
    </div>
  );
}
