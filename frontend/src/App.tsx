import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import KontoplanPage from './pages/kontoplan/KontoplanPage';
import KontoDetaljer from './pages/kontoplan/KontoDetaljer';
import KontoplanImport from './pages/kontoplan/KontoplanImport';
import HovedbokPage from './pages/hovedbok/HovedbokPage';
import KontoutskriftPage from './pages/hovedbok/KontoutskriftPage';
import SaldobalansePage from './pages/hovedbok/SaldobalansePage';
import PeriodePage from './pages/hovedbok/PeriodePage';
import BilagListePage from './pages/bilag/BilagListePage';
import NyttBilagPage from './pages/bilag/NyttBilagPage';
import BilagDetaljerPage from './pages/bilag/BilagDetaljerPage';
import BilagSeriePage from './pages/bilag/BilagSeriePage';
import MvaOversiktPage from './pages/mva/MvaOversiktPage';
import MvaOppgjorPage from './pages/mva/MvaOppgjorPage';
import MvaAvstemmingPage from './pages/mva/MvaAvstemmingPage';
import MvaMeldingPage from './pages/mva/MvaMeldingPage';
import MvaSammenstillingPage from './pages/mva/MvaSammenstillingPage';
import KundeListePage from './pages/kunde/KundeListePage';
import KundeDetaljerPage from './pages/kunde/KundeDetaljerPage';
import InnbetalingPage from './pages/kunde/InnbetalingPage';
import PurringPage from './pages/kunde/PurringPage';
import AldersfordelingPage from './pages/kunde/AldersfordelingPage';
import ApnePostPage from './pages/kunde/ApnePostPage';
import LeverandorListePage from './pages/leverandor/LeverandorListePage';
import LeverandorDetaljerPage from './pages/leverandor/LeverandorDetaljerPage';
import InngaendeFakturaPage from './pages/leverandor/InngaendeFakturaPage';
import BetalingsforslagPage from './pages/leverandor/BetalingsforslagPage';
import LevAldersfordelingPage from './pages/leverandor/AldersfordelingPage';
import LevApnePostPage from './pages/leverandor/ApnePostPage';
import BankOversiktPage from './pages/bank/BankOversiktPage';
import KontoutskriftImportPage from './pages/bank/KontoutskriftImportPage';
import BankavstemmingPage from './pages/bank/BankavstemmingPage';
import BankbevegelserPage from './pages/bank/BankbevegelserPage';
import AvstemmingsrapportPage from './pages/bank/AvstemmingsrapportPage';
import FakturaListePage from './pages/faktura/FakturaListePage';
import NyFakturaPage from './pages/faktura/NyFakturaPage';
import FakturaDetaljerPage from './pages/faktura/FakturaDetaljerPage';
import FakturaForhandsvisningPage from './pages/faktura/FakturaForhandsvisningPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
    },
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Navigate to="/kontoplan" replace />} />
          <Route path="/kontoplan" element={<KontoplanPage />} />
          <Route path="/kontoplan/import" element={<KontoplanImport />} />
          <Route path="/kontoplan/:id" element={<KontoDetaljer />} />
          <Route path="/hovedbok" element={<HovedbokPage />} />
          <Route path="/hovedbok/kontoutskrift" element={<KontoutskriftPage />} />
          <Route path="/hovedbok/saldobalanse" element={<SaldobalansePage />} />
          <Route path="/hovedbok/perioder" element={<PeriodePage />} />
          <Route path="/bilag" element={<BilagListePage />} />
          <Route path="/bilag/ny" element={<NyttBilagPage />} />
          <Route path="/bilag/serier" element={<BilagSeriePage />} />
          <Route path="/bilag/:id" element={<BilagDetaljerPage />} />
          <Route path="/mva" element={<MvaOversiktPage />} />
          <Route path="/mva/oppgjor/:terminId" element={<MvaOppgjorPage />} />
          <Route path="/mva/avstemming/:terminId" element={<MvaAvstemmingPage />} />
          <Route path="/mva/melding/:terminId" element={<MvaMeldingPage />} />
          <Route path="/mva/sammenstilling" element={<MvaSammenstillingPage />} />
          <Route path="/kunde" element={<KundeListePage />} />
          <Route path="/kunde/:id" element={<KundeDetaljerPage />} />
          <Route path="/kunde/innbetaling" element={<InnbetalingPage />} />
          <Route path="/kunde/purring" element={<PurringPage />} />
          <Route path="/kunde/aldersfordeling" element={<AldersfordelingPage />} />
          <Route path="/kunde/apne-poster" element={<ApnePostPage />} />
          <Route path="/leverandor" element={<LeverandorListePage />} />
          <Route path="/leverandor/faktura/ny" element={<InngaendeFakturaPage />} />
          <Route path="/leverandor/betalingsforslag" element={<BetalingsforslagPage />} />
          <Route path="/leverandor/aldersfordeling" element={<LevAldersfordelingPage />} />
          <Route path="/leverandor/apne-poster" element={<LevApnePostPage />} />
          <Route path="/leverandor/:id" element={<LeverandorDetaljerPage />} />
          <Route path="/bank" element={<BankOversiktPage />} />
          <Route path="/bank/import" element={<KontoutskriftImportPage />} />
          <Route path="/bank/bevegelser" element={<BankbevegelserPage />} />
          <Route path="/bank/avstemming/:kontoId" element={<BankavstemmingPage />} />
          <Route path="/bank/rapport/:kontoId" element={<AvstemmingsrapportPage />} />
          <Route path="/faktura" element={<FakturaListePage />} />
          <Route path="/faktura/ny" element={<NyFakturaPage />} />
          <Route path="/faktura/:id/forhandsvisning" element={<FakturaForhandsvisningPage />} />
          <Route path="/faktura/:id" element={<FakturaDetaljerPage />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  );
}

export default App;
