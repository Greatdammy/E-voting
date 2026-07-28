import { useEffect, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { AlertTriangle, CheckCircle2, Radio, ShieldAlert, XCircle } from 'lucide-react';
import axiosInstance from '../../api/axiosInstance';
import { extractErrorMessage } from '../../api/extractErrorMessage';
import { createIntegrityConnection } from '../../signalr/integrityConnection';
import Card from '../../components/ui/Card';
import Badge from '../../components/ui/Badge';
import Button from '../../components/ui/Button';
import Spinner from '../../components/ui/Spinner';

const alertTypeLabels = {
  VelocitySpike: 'Vote velocity spike',
  TimingCluster: 'Timing cluster'
};

const filters = ['Open', 'Reviewed', 'Dismissed', 'All'];

function formatValue(alertType, value) {
  return alertType === 'TimingCluster' ? `${value.toFixed(2)}s between votes` : `${value.toFixed(1)} votes/window`;
}

function AlertCard({ alert, onReview, reviewing }) {
  return (
    <Card className="p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <div className="flex items-center gap-2">
            <Badge status={alert.severity} />
            <Badge status={alert.status} />
            <span className="text-sm font-medium text-slate-900 dark:text-white">
              {alertTypeLabels[alert.alertType] ?? alert.alertType}
            </span>
          </div>
          <p className="mt-1.5 text-xs text-slate-500 dark:text-slate-400">
            {new Date(alert.windowStart).toLocaleString()} – {new Date(alert.windowEnd).toLocaleTimeString()}
          </p>
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-300">
            Observed {formatValue(alert.alertType, alert.observedValue)} · baseline{' '}
            {formatValue(alert.alertType, alert.baselineValue)}
          </p>
          {alert.reviewNote && (
            <p className="mt-1 text-xs italic text-slate-400 dark:text-slate-500">"{alert.reviewNote}"</p>
          )}
        </div>

        {alert.status === 'Open' && (
          <div className="flex shrink-0 gap-2">
            <Button
              type="button"
              variant="secondary"
              disabled={reviewing}
              onClick={() => onReview(alert.alertId, 'Reviewed')}
            >
              <CheckCircle2 className="h-4 w-4" />
              Mark reviewed
            </Button>
            <Button
              type="button"
              variant="ghost"
              disabled={reviewing}
              onClick={() => onReview(alert.alertId, 'Dismissed')}
            >
              <XCircle className="h-4 w-4" />
              Dismiss
            </Button>
          </div>
        )}
      </div>
    </Card>
  );
}

export default function AdminIntegrityPage() {
  const { id } = useParams();
  const [alerts, setAlerts] = useState([]);
  const [summary, setSummary] = useState(null);
  const [statusFilter, setStatusFilter] = useState('Open');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [live, setLive] = useState(false);
  const [reviewingId, setReviewingId] = useState(null);
  const statusFilterRef = useRef(statusFilter);

  useEffect(() => {
    statusFilterRef.current = statusFilter;
  }, [statusFilter]);

  const loadSummary = () => {
    axiosInstance
      .get(`/admin/elections/${id}/integrity-summary`)
      .then((response) => setSummary(response.data))
      .catch(() => {});
  };

  useEffect(() => {
    setLoading(true);
    setError('');

    const query = statusFilter === 'All' ? '' : `?status=${statusFilter}`;
    axiosInstance
      .get(`/admin/elections/${id}/integrity-alerts${query}`)
      .then((response) => setAlerts(response.data))
      .catch((err) => setError(extractErrorMessage(err, 'Could not load integrity alerts.')))
      .finally(() => setLoading(false));

    loadSummary();
  }, [id, statusFilter]);

  useEffect(() => {
    const connection = createIntegrityConnection();
    connection.on('ReceiveIntegrityAlert', (alert) => {
      const currentFilter = statusFilterRef.current;
      setAlerts((prev) => (currentFilter === 'All' || currentFilter === 'Open' ? [alert, ...prev] : prev));
      loadSummary();
    });

    connection
      .start()
      .then(() => {
        setLive(true);
        return connection.invoke('JoinElection', id);
      })
      .catch(() => setLive(false));

    return () => {
      connection.stop();
    };
    // Connection is scoped to the election only; statusFilterRef.current is
    // read at event-fire time so the handler always sees the latest filter
    // without needing to reconnect the socket when the filter changes.
  }, [id]);

  const handleReview = async (alertId, status) => {
    setReviewingId(alertId);
    try {
      const response = await axiosInstance.post(`/admin/elections/${id}/integrity-alerts/${alertId}/review`, {
        status
      });

      setAlerts((prev) =>
        statusFilter === 'All'
          ? prev.map((a) => (a.alertId === alertId ? response.data : a))
          : prev.filter((a) => a.alertId !== alertId)
      );
      loadSummary();
    } catch (err) {
      setError(extractErrorMessage(err, 'Could not update this alert.'));
    } finally {
      setReviewingId(null);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="flex items-center gap-2 text-2xl font-bold text-slate-900 dark:text-white">
            <ShieldAlert className="h-6 w-6 text-fuchsia-600 dark:text-fuchsia-400" />
            Integrity Guard
          </h1>
          <p className="mt-1 text-sm text-slate-500 dark:text-slate-400">
            Flagged for review — not an automatic action. A human review step decides what happens next.
          </p>
        </div>
        {live && (
          <span className="flex items-center gap-1.5 rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-medium text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400">
            <Radio className="h-3.5 w-3.5 animate-pulse" />
            Live
          </span>
        )}
      </div>

      {summary && (
        <div className="grid grid-cols-3 gap-3 sm:max-w-md">
          <Card className="p-4 text-center">
            <p className="text-2xl font-bold text-sky-600 dark:text-sky-400">{summary.openCount}</p>
            <p className="text-xs text-slate-500 dark:text-slate-400">Open</p>
          </Card>
          <Card className="p-4 text-center">
            <p className="text-2xl font-bold text-emerald-600 dark:text-emerald-400">{summary.reviewedCount}</p>
            <p className="text-xs text-slate-500 dark:text-slate-400">Reviewed</p>
          </Card>
          <Card className="p-4 text-center">
            <p className="text-2xl font-bold text-slate-500 dark:text-slate-400">{summary.dismissedCount}</p>
            <p className="text-xs text-slate-500 dark:text-slate-400">Dismissed</p>
          </Card>
        </div>
      )}

      <div className="flex gap-2">
        {filters.map((filter) => (
          <Button
            key={filter}
            type="button"
            variant={statusFilter === filter ? 'primary' : 'secondary'}
            onClick={() => setStatusFilter(filter)}
          >
            {filter}
          </Button>
        ))}
      </div>

      {loading && <Spinner label="Loading integrity alerts..." />}
      {error && <p className="text-rose-600 dark:text-rose-400">{error}</p>}
      {!loading && !error && alerts.length === 0 && (
        <Card className="flex flex-col items-center gap-2 p-8 text-center text-slate-500 dark:text-slate-400">
          <AlertTriangle className="h-5 w-5" />
          {statusFilter === 'All' ? 'No alerts.' : `No ${statusFilter.toLowerCase()} alerts.`}
        </Card>
      )}

      <div className="space-y-3">
        {alerts.map((alert) => (
          <AlertCard
            key={alert.alertId}
            alert={alert}
            onReview={handleReview}
            reviewing={reviewingId === alert.alertId}
          />
        ))}
      </div>
    </div>
  );
}
