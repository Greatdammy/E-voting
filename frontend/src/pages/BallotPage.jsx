import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { CheckCircle2, Vote } from 'lucide-react';
import axiosInstance from '../api/axiosInstance';
import { extractErrorMessage } from '../api/extractErrorMessage';
import Card from '../components/ui/Card';
import Button from '../components/ui/Button';
import Spinner from '../components/ui/Spinner';

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
      <div className="mx-auto max-w-lg text-center">
        <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-emerald-100 text-emerald-600 dark:bg-emerald-500/10 dark:text-emerald-400">
          <CheckCircle2 className="h-7 w-7" />
        </div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Vote recorded</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
          Keep this confirmation for your records — it verifies your vote without revealing your choice.
        </p>

        <Card className="mt-6 space-y-3 p-5 text-left text-sm">
          <div className="flex justify-between gap-4">
            <span className="text-slate-500 dark:text-slate-400">Vote ID</span>
            <span className="font-mono text-slate-900 dark:text-slate-100">{confirmation.voteId}</span>
          </div>
          <div className="flex justify-between gap-4">
            <span className="text-slate-500 dark:text-slate-400">Confirmation hash</span>
            <span className="break-all font-mono text-slate-900 dark:text-slate-100">
              {confirmation.confirmationHash}
            </span>
          </div>
          <div className="flex justify-between gap-4">
            <span className="text-slate-500 dark:text-slate-400">Voted at</span>
            <span className="text-slate-900 dark:text-slate-100">
              {new Date(confirmation.votedAt).toLocaleString()}
            </span>
          </div>
        </Card>

        <Link to={`/elections/${id}/results`}>
          <Button variant="secondary" className="mt-6">
            View live results
          </Button>
        </Link>
      </div>
    );
  }

  if (error && !ballot) {
    return <p className="text-rose-600 dark:text-rose-400">{error}</p>;
  }

  if (!ballot) {
    return <Spinner label="Loading ballot..." />;
  }

  return (
    <form onSubmit={handleSubmit} className="mx-auto max-w-xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-white">{ballot.title}</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">{ballot.description}</p>
      </div>

      <div className="space-y-2">
        {ballot.candidates.map((candidate) => (
          <label
            key={candidate.candidateId}
            className="flex cursor-pointer items-center gap-3 rounded-xl border border-slate-200 bg-white p-4 [&:has(:checked)]:border-indigo-500 [&:has(:checked)]:ring-2 [&:has(:checked)]:ring-indigo-500/30 dark:border-slate-800 dark:bg-slate-900"
          >
            <input
              type="radio"
              name="candidate"
              value={candidate.candidateId}
              checked={selectedCandidateId === candidate.candidateId}
              onChange={() => setSelectedCandidateId(candidate.candidateId)}
              className="h-4 w-4 border-slate-300 text-indigo-600 focus:ring-indigo-500"
            />
            <span>
              <span className="block font-medium text-slate-900 dark:text-white">{candidate.name}</span>
              <span className="text-sm text-slate-500 dark:text-slate-400">{candidate.party}</span>
            </span>
          </label>
        ))}
      </div>

      {error && <p className="text-sm text-rose-600 dark:text-rose-400">{error}</p>}

      <Button type="submit" disabled={submitting}>
        <Vote className="h-4 w-4" />
        {submitting ? 'Submitting...' : 'Submit vote'}
      </Button>
    </form>
  );
}
