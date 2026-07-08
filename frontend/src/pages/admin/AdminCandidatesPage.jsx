import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import axiosInstance from '../../api/axiosInstance';
import { extractErrorMessage } from '../../api/extractErrorMessage';

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
    <div className="p-6 space-y-8">
      <div>
        <h1 className="text-2xl font-semibold mb-4">{editingId ? 'Edit Candidate' : 'Add Candidate'}</h1>
        <form onSubmit={handleSubmit} className="space-y-3 max-w-md">
          <input
            type="text"
            placeholder="Name"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            required
            className="w-full border border-gray-300 rounded px-3 py-2"
          />
          <input
            type="text"
            placeholder="Party"
            value={form.party}
            onChange={(e) => setForm({ ...form, party: e.target.value })}
            required
            className="w-full border border-gray-300 rounded px-3 py-2"
          />
          <input
            type="text"
            placeholder="Photo URL (optional)"
            value={form.photoUrl}
            onChange={(e) => setForm({ ...form, photoUrl: e.target.value })}
            className="w-full border border-gray-300 rounded px-3 py-2"
          />
          {formError && <p className="text-red-600">{formError}</p>}
          <div className="flex gap-2">
            <button
              type="submit"
              disabled={submitting}
              className="bg-indigo-600 text-white px-4 py-2 rounded disabled:opacity-50"
            >
              {submitting ? 'Saving...' : editingId ? 'Save changes' : 'Add candidate'}
            </button>
            {editingId && (
              <button type="button" onClick={cancelEdit} className="px-4 py-2 rounded border border-gray-300">
                Cancel
              </button>
            )}
          </div>
        </form>
      </div>

      <div>
        <h2 className="text-xl font-semibold mb-4">Candidates</h2>
        {loading && <p>Loading...</p>}
        {error && <p className="text-red-600">{error}</p>}
        <ul className="space-y-2">
          {candidates.map((candidate) => (
            <li
              key={candidate.candidateId}
              className="border border-gray-200 rounded p-4 flex items-center justify-between"
            >
              <div>
                <p className="font-medium">{candidate.name}</p>
                <p className="text-sm text-gray-500">{candidate.party}</p>
              </div>
              <div className="flex gap-3">
                <button type="button" onClick={() => startEdit(candidate)} className="text-indigo-600 underline">
                  Edit
                </button>
                <button
                  type="button"
                  onClick={() => handleDelete(candidate.candidateId)}
                  className="text-red-600 underline"
                >
                  Delete
                </button>
              </div>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
