import { Link, useLocation } from 'react-router-dom';

const nav = [
  { label: 'Hjem', path: '/' },
  { label: 'Kontoplan', path: '/kontoplan' },
  { label: 'Hovedbok', path: '/hovedbok' },
  { label: 'Bilag', path: '/bilag' },
  { label: 'MVA', path: '/mva' },
  { label: 'Leverandorer', path: '/leverandor' },
  { label: 'Kunder', path: '/kunde' },
  { label: 'Faktura', path: '/faktura' },
  { label: 'Bank', path: '/bank' },
  { label: 'Rapporter', path: '/rapporter' },
  { label: 'Periode', path: '/periodeavslutning' },
];

export default function Layout({ children }: { children: React.ReactNode }) {
  const location = useLocation();

  return (
    <div style={{ display: 'flex', minHeight: '100vh' }}>
      <nav style={{
        width: 200,
        background: '#1a1a2e',
        color: '#fff',
        padding: '1rem 0',
        flexShrink: 0,
      }}>
        <div style={{ padding: '0 1rem 1rem', borderBottom: '1px solid #333', marginBottom: '0.5rem' }}>
          <Link to="/" style={{ color: '#fff', textDecoration: 'none', fontWeight: 700, fontSize: '1.1rem' }}>
            Regnskap
          </Link>
        </div>
        {nav.map((item) => {
          const isActive = item.path === '/'
            ? location.pathname === '/'
            : location.pathname.startsWith(item.path);
          return (
            <Link
              key={item.path}
              to={item.path}
              style={{
                display: 'block',
                padding: '0.4rem 1rem',
                color: isActive ? '#fff' : '#9ca3af',
                background: isActive ? 'rgba(26, 86, 219, 0.3)' : 'transparent',
                borderLeft: isActive ? '3px solid #1a56db' : '3px solid transparent',
                textDecoration: 'none',
                fontSize: '0.9rem',
              }}
            >
              {item.label}
            </Link>
          );
        })}
      </nav>
      <main style={{ flex: 1, padding: '1.5rem 2rem', overflow: 'auto' }}>
        {children}
      </main>
    </div>
  );
}
