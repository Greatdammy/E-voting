import { useState } from 'react';
import axiosInstance from '../../api/axiosInstance';
import { extractErrorMessage } from '../../api/extractErrorMessage';

const roles = ['Voter', 'ElectionOfficer', 'Administrator'];

export default function AdminCreateUserPage() {
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState(roles[0]);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSubmitting(true);
    setError('');
    setSuccess('');

    try {
      await axiosInstance.post('/admin/users', { fullName, email, password, role });
      setSuccess(`User ${email} created with role ${role}.`);
      setFullName('');
      setEmail('');
      setPassword('');
      setRole(roles[0]);
    } catch (err) {
      setError(extractErrorMessage(err, 'Could not create user.'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="p-6 max-w-sm space-y-4">
      <h1 className="text-2xl font-semibold">Create User</h1>
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
        <select
          value={role}
          onChange={(e) => setRole(e.target.value)}
          className="w-full border border-gray-300 rounded px-3 py-2"
        >
          {roles.map((r) => (
            <option key={r} value={r}>
              {r}
            </option>
          ))}
        </select>
        {error && <p className="text-red-600">{error}</p>}
        {success && <p className="text-green-600">{success}</p>}
        <button
          type="submit"
          disabled={submitting}
          className="bg-indigo-600 text-white px-4 py-2 rounded w-full disabled:opacity-50"
        >
          {submitting ? 'Creating...' : 'Create user'}
        </button>
      </form>
    </div>
  );
}
