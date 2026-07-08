import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import axiosInstance from '../../api/axiosInstance';
import { extractErrorMessage } from '../../api/extractErrorMessage';

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
    <div className="p-6 space-y-8">
      <div>
        <h1 className="text-2xl font-semibold mb-4">Create Election</h1>
        <form onSubmit={handleCreate} className="space-y-3 max-w-md">
          <input
            type="text"
            placeholder="Title"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            required
            className="w-full border border-gray-300 rounded px-3 py-2"
          />
          <textarea
            placeholder="Description"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            required
            className="w-full border border-gray-300 rounded px-3 py-2"
          />
          <label className="block text-sm text-gray-600">
            Start date
            <input
              type="datetime-local"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              required
              className="w-full border border-gray-300 rounded px-3 py-2"
            />
          </label>
          <label className="block text-sm text-gray-600">
            End date
            <input
              type="datetime-local"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              required
              className="w-full border border-gray-300 rounded px-3 py-2"
            />
          </label>
          {formError && <p className="text-red-600">{formError}</p>}
          <button
            type="submit"
            disabled={submitting}
            className="bg-indigo-600 text-white px-4 py-2 rounded disabled:opacity-50"
          >
            {submitting ? 'Creating...' : 'Create election'}
          </button>
        </form>
      </div>

      <div>
        <h2 className="text-xl font-semibold mb-4">Elections</h2>
        {loading && <p>Loading...</p>}
        {error && <p className="text-red-600">{error}</p>}
        <ul className="space-y-2">
          {elections.map((election) => (
            <li
              key={election.electionId}
              className="border border-gray-200 rounded p-4 flex items-center justify-between"
            >
              <div>
                <p className="font-medium">{election.title}</p>
                <p className="text-sm text-gray-500">{election.status}</p>
              </div>
              <Link to={`/admin/elections/${election.electionId}/candidates`} className="text-indigo-600 underline">
                Manage candidates
              </Link>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
