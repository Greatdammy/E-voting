import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosInstance from '../api/axiosInstance';
import { extractErrorMessage } from '../api/extractErrorMessage';

export default function RegisterPage() {
  const navigate = useNavigate();
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();

    if (password !== confirmPassword) {
      setError('Passwords do not match.');
      return;
    }

    setSubmitting(true);
    setError('');

    try {
      await axiosInstance.post('/auth/register', { fullName, email, password });
      navigate('/login', { state: { email, registered: true } });
    } catch (err) {
      setError(extractErrorMessage(err, 'Could not register.'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="p-6 max-w-sm mx-auto space-y-4">
      <h1 className="text-2xl font-semibold">Register</h1>
      <form onSubmit={handleSubmit} className="space-y-3">
        <input
          type="text"
          placeholder="Full name"
          value={fullName}
          onChange={(e) => setFullName(e.target.value)}
          required
          className="w-full border border-gray-300 rounded px-3 py-2"
        />
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
        <input
          type="password"
          placeholder="Confirm password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          required
          className="w-full border border-gray-300 rounded px-3 py-2"
        />
        {error && <p className="text-red-600">{error}</p>}
        <button
          type="submit"
          disabled={submitting}
          className="bg-indigo-600 text-white px-4 py-2 rounded w-full disabled:opacity-50"
        >
          {submitting ? 'Registering...' : 'Register'}
        </button>
      </form>
    </div>
  );
}
