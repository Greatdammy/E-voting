import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { BarChart3, CalendarClock, CheckCircle2, Vote } from 'lucide-react';
import axiosInstance from '../api/axiosInstance';
import { extractErrorMessage } from '../api/extractErrorMessage';
import { pruneStaleElections } from '../utils/turnoutHistory';
import Card from '../components/ui/Card';
import Badge from '../components/ui/Badge';
import Spinner from '../components/ui/Spinner';
import Button from '../components/ui/Button';

export default function ElectionsPage() {
  const [elections, setElections] = useState([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    axiosInstance
      .get('/elections')
      .then((response) => {
        setElections(response.data);
        pruneStaleElections(response.data.map((election) => election.electionId));
      })
      .catch((err) => setError(extractErrorMessage(err, 'Could not load elections.')))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return <Spinner label="Loading elections..." />;
  }

  if (error) {
    return <p className="text-rose-600 dark:text-rose-400">{error}</p>;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Elections</h1>
        <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">Vote in active elections or check results.</p>
      </div>

      {elections.length === 0 && (
        <Card className="p-8 text-center text-slate-500 dark:text-slate-400">No elections yet.</Card>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        {elections.map((election) => (
          <Card key={election.electionId} className="flex flex-col gap-4 p-5">
            <div className="flex items-start justify-between gap-2">
              <div>
                <h2 className="font-semibold text-slate-900 dark:text-white">{election.title}</h2>
                <p className="mt-1 flex items-center gap-1 text-xs text-slate-500 dark:text-slate-400">
                  <CalendarClock className="h-3.5 w-3.5" />
                  {new Date(election.startDate).toLocaleDateString()} –{' '}
                  {new Date(election.endDate).toLocaleDateString()}
                </p>
              </div>
              <Badge status={election.status} />
            </div>

            {election.hasVoted && (
              <p className="flex items-center gap-1.5 text-sm text-emerald-600 dark:text-emerald-400">
                <CheckCircle2 className="h-4 w-4" />
                You voted
              </p>
            )}

            <div className="mt-auto flex gap-2">
              {election.status === 'Active' && !election.hasVoted && (
                <Link to={`/elections/${election.electionId}/ballot`} className="flex-1">
                  <Button className="w-full">
                    <Vote className="h-4 w-4" />
                    Vote
                  </Button>
                </Link>
              )}
              {election.status !== 'Upcoming' && (
                <Link to={`/elections/${election.electionId}/results`} className="flex-1">
                  <Button variant="secondary" className="w-full">
                    <BarChart3 className="h-4 w-4" />
                    Results
                  </Button>
                </Link>
              )}
            </div>
          </Card>
        ))}
      </div>
    </div>
  );
}
