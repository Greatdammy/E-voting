import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { CalendarPlus, ListChecks, ShieldAlert, Users2 } from 'lucide-react';
import axiosInstance from '../../api/axiosInstance';
import { extractErrorMessage } from '../../api/extractErrorMessage';
import Card from '../../components/ui/Card';
import Badge from '../../components/ui/Badge';
import Input from '../../components/ui/Input';
import Button from '../../components/ui/Button';
import Spinner from '../../components/ui/Spinner';

export default function AdminElectionsPage() {
  const [elections, setElections] = useState([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [formError, setFormError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const loadElections = () => {
    setLoading(true);
    axiosInstance
      .get('/admin/elections')
      .then((response) => setElections(response.data))
      .catch((err) => setError(extractErrorMessage(err, 'Could not load elections.')))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadElections();
  }, []);

  const handleCreate = async (event) => {
    event.preventDefault();
    setSubmitting(true);
    setFormError('');

    try {
      await axiosInstance.post('/admin/elections', {
        title,
        description,
        startDate: new Date(startDate).toISOString(),
        endDate: new Date(endDate).toISOString()
      });
      setTitle('');
      setDescription('');
      setStartDate('');
      setEndDate('');
      loadElections();
    } catch (err) {
      setFormError(extractErrorMessage(err, 'Could not create election.'));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Elections</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Create elections and manage their candidates.</p>
      </div>

      <Card className="p-6">
        <h2 className="mb-4 flex items-center gap-1.5 text-sm font-semibold text-slate-700 dark:text-slate-300">
          <CalendarPlus className="h-4 w-4" />
          Create election
        </h2>
        <form onSubmit={handleCreate} className="grid gap-4 sm:grid-cols-2">
          <div className="sm:col-span-2">
            <Input label="Title" type="text" value={title} onChange={(e) => setTitle(e.target.value)} required />
          </div>
          <div className="sm:col-span-2">
            <label className="block">
              <span className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">Description</span>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                required
                rows={3}
                className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/30 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
              />
            </label>
          </div>
          <Input
            label="Start date"
            type="datetime-local"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            required
          />
          <Input
            label="End date"
            type="datetime-local"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
            required
          />
          {formError && <p className="text-sm text-rose-600 dark:text-rose-400 sm:col-span-2">{formError}</p>}
          <div className="sm:col-span-2">
            <Button type="submit" disabled={submitting}>
              {submitting ? 'Creating...' : 'Create election'}
            </Button>
          </div>
        </form>
      </Card>

      <div>
        <h2 className="mb-3 flex items-center gap-1.5 text-sm font-semibold text-slate-700 dark:text-slate-300">
          <ListChecks className="h-4 w-4" />
          All elections
        </h2>
        {loading && <Spinner label="Loading elections..." />}
        {error && <p className="text-rose-600 dark:text-rose-400">{error}</p>}
        {!loading && !error && elections.length === 0 && (
          <Card className="p-8 text-center text-slate-500 dark:text-slate-400">No elections yet.</Card>
        )}
        <ul className="space-y-2">
          {elections.map((election) => (
            <li key={election.electionId}>
              <Card className="flex items-center justify-between gap-4 p-4">
                <div>
                  <p className="font-medium text-slate-900 dark:text-white">{election.title}</p>
                  <div className="mt-1">
                    <Badge status={election.status} />
                  </div>
                </div>
                <div className="flex gap-2">
                  <Link to={`/admin/elections/${election.electionId}/candidates`}>
                    <Button variant="secondary">
                      <Users2 className="h-4 w-4" />
                      Manage candidates
                    </Button>
                  </Link>
                  <Link to={`/admin/elections/${election.electionId}/integrity`}>
                    <Button variant="secondary">
                      <ShieldAlert className="h-4 w-4" />
                      Integrity
                    </Button>
                  </Link>
                </div>
              </Card>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
