import { Link } from 'react-router-dom';

interface RapportKort {
  tittel: string;
  beskrivelse: string;
  lenke: string;
  ikon: string;
}

const rapporter: RapportKort[] = [
  {
    tittel: 'Resultatregnskap',
    beskrivelse: 'Inntekter og kostnader ihht Regnskapsloven 3-2',
    lenke: '/rapporter/resultatregnskap',
    ikon: 'RR',
  },
  {
    tittel: 'Balanse',
    beskrivelse: 'Eiendeler og gjeld ihht Regnskapsloven 3-2a',
    lenke: '/rapporter/balanse',
    ikon: 'BA',
  },
  {
    tittel: 'Kontantstrøm',
    beskrivelse: 'Kontantstrømoppstilling, indirekte metode',
    lenke: '/rapporter/kontantstrom',
    ikon: 'KS',
  },
  {
    tittel: 'Saldobalanse',
    beskrivelse: 'Utvidet saldobalanse med grupperinger',
    lenke: '/rapporter/saldobalanse',
    ikon: 'SB',
  },
  {
    tittel: 'Hovedbokutskrift',
    beskrivelse: 'Kontospesifikasjon per konto (Bokføringsforskriften 3-1)',
    lenke: '/rapporter/hovedbokutskrift',
    ikon: 'HB',
  },
  {
    tittel: 'SAF-T Eksport',
    beskrivelse: 'Komplett SAF-T Financial XML v1.30',
    lenke: '/rapporter/saft',
    ikon: 'ST',
  },
  {
    tittel: 'Nøkkeltall',
    beskrivelse: 'Likviditet, soliditet og lønnsomhet',
    lenke: '/rapporter/nokkeltall',
    ikon: 'NT',
  },
  {
    tittel: 'Budsjett',
    beskrivelse: 'Budsjettregistrering og sammenligning',
    lenke: '/rapporter/budsjett',
    ikon: 'BU',
  },
];

const containerStyle: React.CSSProperties = {
  maxWidth: 1000,
  margin: '0 auto',
  padding: 24,
};

const gridStyle: React.CSSProperties = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))',
  gap: 20,
  marginTop: 24,
};

const kortStyle: React.CSSProperties = {
  border: '1px solid #e0e0e0',
  borderRadius: 8,
  padding: 20,
  textDecoration: 'none',
  color: 'inherit',
  display: 'flex',
  alignItems: 'flex-start',
  gap: 16,
  transition: 'box-shadow 0.15s, border-color 0.15s',
};

const ikonStyle: React.CSSProperties = {
  width: 48,
  height: 48,
  borderRadius: 8,
  backgroundColor: '#1a5276',
  color: '#fff',
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  fontWeight: 700,
  fontSize: 16,
  flexShrink: 0,
};

export default function RapportOversiktPage() {
  return (
    <div style={containerStyle}>
      <h1 style={{ margin: 0 }}>Rapporter</h1>
      <p style={{ color: '#666', marginTop: 8 }}>
        Finansielle rapporter og eksport. Alle rapporter er skrivebeskyttet.
      </p>

      <div style={gridStyle}>
        {rapporter.map((rapport) => (
          <Link
            key={rapport.lenke}
            to={rapport.lenke}
            style={kortStyle}
            onMouseEnter={(e) => {
              e.currentTarget.style.boxShadow = '0 2px 8px rgba(0,0,0,0.1)';
              e.currentTarget.style.borderColor = '#1a5276';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.boxShadow = 'none';
              e.currentTarget.style.borderColor = '#e0e0e0';
            }}
          >
            <div style={ikonStyle}>{rapport.ikon}</div>
            <div>
              <div style={{ fontWeight: 600, fontSize: 16, marginBottom: 4 }}>
                {rapport.tittel}
              </div>
              <div style={{ color: '#666', fontSize: 14, lineHeight: 1.4 }}>
                {rapport.beskrivelse}
              </div>
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
