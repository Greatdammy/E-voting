import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import axiosInstance from '../api/axiosInstance';
import { extractErrorMessage } from '../api/extractErrorMessage';

export default function BallotPage() {
  const { id } = useParams();
  const [ballot, setBallot] = useState(null);
  const [selectedCandidateId, setSelectedCandidateId] = useState('');
  const [error, setError] = useState('');
  const [confirmation, setConfirmation] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    axiosInstance
      .get(`/elections/${id}/ballot`)
      .then((response) => setBallot(response.data))
      .catch((err) => setError(extractErrorMessage(err, 'Could not load the ballot.')));
  }, [id]);

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!selectedCandidateId) {
      setError('Select a candidate before submitting.');
      return;
    }

    setSubmitting(true);
    setError('');

    try {
      const response = await axiosInstance.post(`/elections/${id}/vote`, {
        candidateId: selectedCandidateId
      });
      setConfirmation(response.data);
    } catch (err) {
      setError(extractErrorMessage(err, 'Could not submit your vote.'));
    } finally {
      setSubmitting(false);
    }
  };

  if (confirmation) {
    return (
      <div className="p-6 space-y-4">
        <h1 className="text-2xl font-semibold">Vote recorded</h1>
        <p>Your vote has been recorded. Keep this confirmation for your records:</p>
        <div className="border border-gray-200 rounded p-4 space-y-1 text-sm">
          <p>
            <span className="font-medium">Vote ID:</span> {confirmation.voteId}
          </p>
          <p>
            <span className="font-medium">Confirmation hash:</span> {confirmation.confirmationHash}
          </p>
          <p>
            <span className="font-medium">Voted at:</span> {new Date(confirmation.votedAt).toLocaleString()}
          </p>
        </div>
        <Link to={`/elections/${id}/results`} className="text-indigo-600 underline">
          View live results
        </Link>
      </div>
    );
  }

  if (error && !ballot) {
    return <p className="p-6 text-red-600">{error}</p>;
  }

  if (!ballot) {
    return <p className="p-6">Loading ballot...</p>;
  }

  return (
    <form onSubmit={handleSubmit} className="p-6 space-y-4">
      <h1 className="text-2xl font-semibold">{ballot.title}</h1>
      <p className="text-gray-600">{ballot.description}</p>
      <div className="space-y-2">
        {ballot.candidates.map((candidate) => (
          <label
            key={candidate.candidateId}
            className="flex items-center gap-3 border border-gray-200 rounded p-3"
          >
            <input
              type="radio"
              name="candidate"
              value={candidate.candidateId}
              checked={selectedCandidateId === candidate.candidateId}
              onChange={() => setSelectedCandidateId(candidate.candidateId)}
            />
            <span>
              <span className="font-medium">{candidate.name}</span>
              <span className="text-gray-500"> — {candidate.party}</span>
            </span>
          </label>
        ))}
      </div>
      {error && <p className="text-red-600">{error}</p>}
      <button
        type="submit"
        disabled={submitting}
        className="bg-indigo-600 text-white px-4 py-2 rounded disabled:opacity-50"
      >
        {submitting ? 'Submitting...' : 'Submit vote'}
      </button>
    </form>
  );
}
