import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import axiosInstance from '../api/axiosInstance';
import { setCredentials } from '../store/authSlice';
import { extractErrorMessage } from '../api/extractErrorMessage';

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
    <div className="p-6 max-w-sm mx-auto space-y-4">
      <h1 className="text-2xl font-semibold">Login</h1>
      {location.state?.registered && (
        <p className="text-green-600">Registration successful — please log in.</p>
      )}
      <form onSubmit={handleSubmit} className="space-y-3">
        <input
          type="email"
          placeholder="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          className="w-full border border-gray-300 rounded px-3 py-2"
        />
        <input
          type="password"
          placeholder="Password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          className="w-full border border-gray-300 rounded px-3 py-2"
        />
        {error && <p className="text-red-600">{error}</p>}
        <button
          type="submit"
          disabled={submitting}
          className="bg-indigo-600 text-white px-4 py-2 rounded w-full disabled:opacity-50"
        >
          {submitting ? 'Logging in...' : 'Login'}
        </button>
      </form>
    </div>
  );
}
