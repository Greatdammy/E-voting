import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useSelector } from 'react-redux';
import { Radio, ShieldAlert, Trophy } from 'lucide-react';
import axiosInstance from '../api/axiosInstance';
import { createResultsConnection } from '../signalr/resultsConnection';
import ResultsChart from '../components/ResultsChart';
import TurnoutForecastCard from '../components/TurnoutForecastCard';
import Card from '../components/ui/Card';
import Spinner from '../components/ui/Spinner';
import { useTurnoutForecast } from '../hooks/useTurnoutForecast';

function Leaderboard({ tally, totalVotes }) {
  const sorted = [...tally].sort((a, b) => b.voteCount - a.voteCount);

  return (
    <ul className="space-y-2">
      {sorted.map((candidate, index) => {
        const pct = totalVotes > 0 ? Math.round((candidate.voteCount / totalVotes) * 100) : 0;
        return (
          <li key={candidate.candidateId} className="rounded-lg border border-slate-200 p-3 dark:border-slate-800">
            <div className="flex items-center justify-between text-sm">
              <span className="flex items-center gap-2 font-medium text-slate-900 dark:text-white">
                {index === 0 && candidate.voteCount > 0 && <Trophy className="h-4 w-4 text-amber-500" />}
                {candidate.name}
                <span className="font-normal text-slate-500 dark:text-slate-400">{candidate.party}</span>
              </span>
              <span className="tabular-nums text-slate-600 dark:text-slate-300">
                {candidate.voteCount} · {pct}%
              </span>
            </div>
            <div className="mt-2 h-1.5 w-full overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
              <div className="h-full rounded-full bg-indigo-600 dark:bg-indigo-500" style={{ width: `${pct}%` }} />
            </div>
          </li>
        );
      })}
    </ul>
  );
}

export default function ResultsPage() {
  const { id } = useParams();
  const { role } = useSelector((state) => state.auth);
  const [results, setResults] = useState(null);
  const [error, setError] = useState('');
  const [live, setLive] = useState(false);
  const [endDate, setEndDate] = useState(null);
  const { history, forecast } = useTurnoutForecast({
    electionId: id,
    totalVotes: results?.totalVotes,
    status: results?.status,
    endDate
  });

  useEffect(() => {
    let isMounted = true;

    axiosInstance
      .get(`/elections/${id}/results`)
      .then((response) => {
        if (isMounted) {
          setResults(response.data);
        }
      })
      .catch(() => {
        if (isMounted) {
          setError('Could not load results.');
        }
      });

    // GET /elections is Voter-scoped and returns startDate/endDate; this
    // results page itself is public. Best-effort only — an anonymous or
    // non-voter viewer simply won't get a forecast card, since there's no
    // other frontend-accessible source for the election's end date.
    axiosInstance
      .get('/elections')
      .then((response) => {
        if (!isMounted) {
          return;
        }
        const match = response.data.find((election) => election.electionId === id);
        if (match) {
          setEndDate(match.endDate);
        }
      })
      .catch(() => {});

    const connection = createResultsConnection();
    connection.on('ReceiveResults', (payload) => {
      if (isMounted) {
        setResults(payload);
      }
    });

    connection
      .start()
      .then(() => {
        if (isMounted) {
          setLive(true);
        }
        return connection.invoke('JoinElection', id);
      })
      .catch(() => {
        if (isMounted) {
          setError((prev) => prev || 'Live updates unavailable.');
        }
      });

    return () => {
      isMounted = false;
      connection.stop();
    };
  }, [id]);

  if (error && !results) {
    return <p className="text-rose-600 dark:text-rose-400">{error}</p>;
  }

  if (!results) {
    return <Spinner label="Loading results..." />;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 dark:text-white">{results.title}</h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            {results.status} · {results.totalVotes} total votes
          </p>
        </div>
        <div className="flex items-center gap-2">
          {(role === 'Administrator' || role === 'ElectionOfficer') && (
            <Link
              to={`/admin/elections/${id}/integrity`}
              className="flex items-center gap-1.5 rounded-full bg-fuchsia-100 px-2.5 py-1 text-xs font-medium text-fuchsia-700 hover:bg-fuchsia-200 dark:bg-fuchsia-500/10 dark:text-fuchsia-400 dark:hover:bg-fuchsia-500/20"
            >
              <ShieldAlert className="h-3.5 w-3.5" />
              Monitored by an automated integrity check
            </Link>
          )}
          {live && (
            <span className="flex items-center gap-1.5 rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-medium text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400">
              <Radio className="h-3.5 w-3.5 animate-pulse" />
              Live
            </span>
          )}
        </div>
      </div>

      <Card className="p-5">
        <ResultsChart tally={results.tally} />
      </Card>

      {results.status === 'Active' && endDate && (
        <TurnoutForecastCard history={history} forecast={forecast} endDate={endDate} />
      )}

      <div>
        <h2 className="mb-3 flex items-center gap-1.5 text-sm font-semibold text-slate-700 dark:text-slate-300">
          <Trophy className="h-4 w-4" />
          Standings
        </h2>
        <Leaderboard tally={results.tally} totalVotes={results.totalVotes} />
      </div>
    </div>
  );
}
