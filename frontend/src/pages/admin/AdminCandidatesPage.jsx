import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Pencil, Trash2, UserRoundPlus, Users } from 'lucide-react';
import axiosInstance from '../../api/axiosInstance';
import { extractErrorMessage } from '../../api/extractErrorMessage';
import Card from '../../components/ui/Card';
import Input from '../../components/ui/Input';
import Button from '../../components/ui/Button';
import Spinner from '../../components/ui/Spinner';

const emptyForm = { name: '', party: '', photoUrl: '' };

export default function AdminCandidatesPage() {
  const { id } = useParams();
  const [candidates, setCandidates] = useState([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  const [form, setForm] = useState(emptyForm);
  const [editingId, setEditingId] = useState(null);
  const [formError, setFormError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const loadCandidates = () => {
    setLoading(true);
    axiosInstance
      .get(`/admin/elections/${id}/candidates`)
      .then((response) => setCandidates(response.data))
      .catch((err) => setError(extractErrorMessage(err, 'Could not load candidates.')))
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    loadCandidates();
  }, [id]);

  const startEdit = (candidate) => {
    setEditingId(candidate.candidateId);
    setForm({ name: candidate.name, party: candidate.party, photoUrl: candidate.photoUrl || '' });
  };

  const cancelEdit = () => {
    setEditingId(null);
    setForm(emptyForm);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setSubmitting(true);
    setFormError('');

    try {
      if (editingId) {
        await axiosInstance.put(`/admin/elections/${id}/candidates/${editingId}`, form);
      } else {
        await axiosInstance.post(`/admin/elections/${id}/candidates`, form);
      }
      cancelEdit();
      loadCandidates();
    } catch (err) {
      setFormError(extractErrorMessage(err, 'Could not save candidate.'));
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (candidateId) => {
    if (!window.confirm('Delete this candidate?')) {
      return;
    }

    try {
      await axiosInstance.delete(`/admin/elections/${id}/candidates/${candidateId}`);
      loadCandidates();
    } catch (err) {
      setError(extractErrorMessage(err, 'Could not delete candidate.'));
    }
  };

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Candidates</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Only editable while the election is upcoming.
        </p>
      </div>

      <Card className="p-6">
        <h2 className="mb-4 flex items-center gap-1.5 text-sm font-semibold text-slate-700 dark:text-slate-300">
          <UserRoundPlus className="h-4 w-4" />
          {editingId ? 'Edit candidate' : 'Add candidate'}
        </h2>
        <form onSubmit={handleSubmit} className="grid gap-4 sm:grid-cols-2">
          <Input label="Name" type="text" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
          <Input
            label="Party"
            type="text"
            value={form.party}
            onChange={(e) => setForm({ ...form, party: e.target.value })}
            required
          />
          <div className="sm:col-span-2">
            <Input
              label="Photo URL (optional)"
              type="text"
              value={form.photoUrl}
              onChange={(e) => setForm({ ...form, photoUrl: e.target.value })}
            />
          </div>
          {formError && <p className="text-sm text-rose-600 dark:text-rose-400 sm:col-span-2">{formError}</p>}
          <div className="flex gap-2 sm:col-span-2">
            <Button type="submit" disabled={submitting}>
              {submitting ? 'Saving...' : editingId ? 'Save changes' : 'Add candidate'}
            </Button>
            {editingId && (
              <Button type="button" variant="secondary" onClick={cancelEdit}>
                Cancel
              </Button>
            )}
          </div>
        </form>
      </Card>

      <div>
        <h2 className="mb-3 flex items-center gap-1.5 text-sm font-semibold text-slate-700 dark:text-slate-300">
          <Users className="h-4 w-4" />
          Candidate list
        </h2>
        {loading && <Spinner label="Loading candidates..." />}
        {error && <p className="text-rose-600 dark:text-rose-400">{error}</p>}
        {!loading && !error && candidates.length === 0 && (
          <Card className="p-8 text-center text-slate-500 dark:text-slate-400">No candidates yet.</Card>
        )}
        <ul className="space-y-2">
          {candidates.map((candidate) => (
            <li key={candidate.candidateId}>
              <Card className="flex items-center justify-between gap-4 p-4">
                <div>
                  <p className="font-medium text-slate-900 dark:text-white">{candidate.name}</p>
                  <p className="text-sm text-slate-500 dark:text-slate-400">{candidate.party}</p>
                </div>
                <div className="flex gap-2">
                  <Button type="button" variant="ghost" onClick={() => startEdit(candidate)} aria-label="Edit">
                    <Pencil className="h-4 w-4" />
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    className="text-rose-600 hover:bg-rose-50 dark:text-rose-400 dark:hover:bg-rose-500/10"
                    onClick={() => handleDelete(candidate.candidateId)}
                    aria-label="Delete"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </Card>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
