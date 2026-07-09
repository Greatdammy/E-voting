import { useState } from 'react';
import { CheckCircle2, Mail, ShieldPlus, User } from 'lucide-react';
import axiosInstance from '../../api/axiosInstance';
import { extractErrorMessage } from '../../api/extractErrorMessage';
import Card from '../../components/ui/Card';
import Input from '../../components/ui/Input';
import Button from '../../components/ui/Button';

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
    <div className="mx-auto max-w-sm">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Create user</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Provision an Officer or Administrator account.</p>
      </div>

      <Card className="p-6">
        <form onSubmit={handleSubmit} className="space-y-4">
          <Input label="Full name" icon={User} type="text" value={fullName} onChange={(e) => setFullName(e.target.value)} required />
          <Input label="Email" icon={Mail} type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          <Input
            label="Password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Role</span>
            <select
              value={role}
              onChange={(e) => setRole(e.target.value)}
              className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/30 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
            >
              {roles.map((r) => (
                <option key={r} value={r}>
                  {r}
                </option>
              ))}
            </select>
          </label>
          {error && <p className="text-sm text-rose-600 dark:text-rose-400">{error}</p>}
          {success && (
            <p className="flex items-center gap-1.5 text-sm text-emerald-600 dark:text-emerald-400">
              <CheckCircle2 className="h-4 w-4" />
              {success}
            </p>
          )}
          <Button type="submit" disabled={submitting} className="w-full">
            <ShieldPlus className="h-4 w-4" />
            {submitting ? 'Creating...' : 'Create user'}
          </Button>
        </form>
      </Card>
    </div>
  );
}
