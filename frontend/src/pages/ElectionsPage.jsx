import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import axiosInstance from '../api/axiosInstance';
import { extractErrorMessage } from '../api/extractErrorMessage';

export default function ElectionsPage() {
  const [elections, setElections] = useState([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    axiosInstance
      .get('/elections')
      .then((response) => setElections(response.data))
      .catch((err) => setError(extractErrorMessage(err, 'Could not load elections.')))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return <p className="p-6">Loading elections...</p>;
  }

  if (error) {
    return <p className="p-6 text-red-600">{error}</p>;
  }

  return (
    <div className="p-6 space-y-4">
      <h1 className="text-2xl font-semibold">Elections</h1>
      {elections.length === 0 && <p>No elections yet.</p>}
      <ul className="space-y-2">
        {elections.map((election) => (
          <li key={election.electionId} className="border border-gray-200 rounded p-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="font-medium">{election.title}</p>
                <p className="text-sm text-gray-500">
                  {election.status} {election.hasVoted ? '· You voted' : ''}
                </p>
              </div>
              <div className="flex gap-3">
                {election.status === 'Active' && !election.hasVoted && (
                  <Link to={`/elections/${election.electionId}/ballot`} className="text-indigo-600 underline">
                    Vote
                  </Link>
                )}
                {election.status !== 'Upcoming' && (
                  <Link to={`/elections/${election.electionId}/results`} className="text-indigo-600 underline">
                    Results
                  </Link>
                )}
              </div>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}
