import { Navigate, Route, Routes } from 'react-router-dom';
import { useSelector } from 'react-redux';
import NavBar from './components/NavBar';
import HelpWidget from './components/HelpWidget';
import ProtectedRoute from './routes/ProtectedRoute';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import ElectionsPage from './pages/ElectionsPage';
import BallotPage from './pages/BallotPage';
import ResultsPage from './pages/ResultsPage';
import AdminElectionsPage from './pages/admin/AdminElectionsPage';
import AdminCandidatesPage from './pages/admin/AdminCandidatesPage';
import AdminCreateUserPage from './pages/admin/AdminCreateUserPage';
import AdminIntegrityPage from './pages/admin/AdminIntegrityPage';

function Home() {
  const { token, role } = useSelector((state) => state.auth);

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  return <Navigate to={role === 'Voter' ? '/elections' : '/admin/elections'} replace />;
}

export default function App() {
  return (
    <div className="min-h-screen bg-slate-50 dark:bg-slate-950">
      <NavBar />
      <HelpWidget />
      <main className="mx-auto max-w-5xl px-4 py-8 sm:px-6">
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/elections/:id/results" element={<ResultsPage />} />

          <Route element={<ProtectedRoute allowedRoles={['Voter']} />}>
            <Route path="/elections" element={<ElectionsPage />} />
            <Route path="/elections/:id/ballot" element={<BallotPage />} />
          </Route>

          <Route element={<ProtectedRoute allowedRoles={['Administrator', 'ElectionOfficer']} />}>
            <Route path="/admin/elections" element={<AdminElectionsPage />} />
            <Route path="/admin/elections/:id/candidates" element={<AdminCandidatesPage />} />
            <Route path="/admin/elections/:id/integrity" element={<AdminIntegrityPage />} />
          </Route>

          <Route element={<ProtectedRoute allowedRoles={['Administrator']} />}>
            <Route path="/admin/users" element={<AdminCreateUserPage />} />
          </Route>
        </Routes>
      </main>
    </div>
  );
}
