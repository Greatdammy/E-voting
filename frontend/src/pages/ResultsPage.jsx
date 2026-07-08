import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import axiosInstance from '../api/axiosInstance';
import { createResultsConnection } from '../signalr/resultsConnection';
import ResultsChart from '../components/ResultsChart';

export default function ResultsPage() {
  const { id } = useParams();
  const [results, setResults] = useState(null);
  const [error, setError] = useState('');

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

    const connection = createResultsConnection();
    connection.on('ReceiveResults', (payload) => {
      if (isMounted) {
        setResults(payload);
      }
    });

    connection
      .start()
      .then(() => connection.invoke('JoinElection', id))
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
    return <p className="p-6 text-red-600">{error}</p>;
  }

  if (!results) {
    return <p className="p-6">Loading results...</p>;
  }

  return (
    <div className="p-6 space-y-4">
      <h1 className="text-2xl font-semibold">{results.title}</h1>
      <p className="text-sm text-gray-500">
        Status: {results.status} · Total votes: {results.totalVotes}
      </p>
      <ResultsChart tally={results.tally} />
    </div>
  );
}
