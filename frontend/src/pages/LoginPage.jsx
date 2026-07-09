import { useState } from 'react';
import { useLocation, useNavigate, Link } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { CheckCircle2, Lock, LogIn, Mail } from 'lucide-react';
import axiosInstance from '../api/axiosInstance';
import { setCredentials } from '../store/authSlice';
import { extractErrorMessage } from '../api/extractErrorMessage';
import Card from '../components/ui/Card';
import Input from '../components/ui/Input';
import Button from '../components/ui/Button';

export default function LoginPage() {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState(location.state?.email || '');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSubmitting(true);
    setError('');

    try {
      const response = await axiosInstance.post('/auth/login', { email, password });
      const { token, expiresAt, userId, role } = response.data;
      dispatch(setCredentials({ token, expiresAt, userId, role }));
      navigate(role === 'Voter' ? '/elections' : '/admin/elections');
    } catch (err) {
      setError(extractErrorMessage(err, 'Invalid email or password.'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="mx-auto max-w-sm">
      <div className="mb-6 text-center">
        <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Welcome back</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Log in to cast your vote or manage elections.
        </p>
      </div>

      <Card className="p-6">
        {location.state?.registered && (
          <div className="mb-4 flex items-start gap-2 rounded-lg bg-emerald-50 p-3 text-sm text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400">
            <CheckCircle2 className="mt-0.5 h-4 w-4 flex-shrink-0" />
            <span>Registration successful — please log in.</span>
          </div>
        )}
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Email"
            icon={Mail}
            type="email"
            placeholder="Email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
          <Input
            label="Password"
            icon={Lock}
            type="password"
            placeholder="Password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
          {error && <p className="text-sm text-rose-600 dark:text-rose-400">{error}</p>}
          <Button type="submit" disabled={submitting} className="w-full">
            <LogIn className="h-4 w-4" />
            {submitting ? 'Logging in...' : 'Login'}
          </Button>
        </form>
      </Card>

      <p className="mt-4 text-center text-sm text-slate-500 dark:text-slate-400">
        Don&apos;t have an account?{' '}
        <Link to="/register" className="font-medium text-indigo-600 hover:text-indigo-500 dark:text-indigo-400">
          Register
        </Link>
      </p>
    </div>
  );
}
