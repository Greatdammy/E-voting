import { useEffect, useMemo, useState } from 'react';
import { fitLine, predict } from '../ml/linearRegression';
import { appendSnapshot, getHistory } from '../utils/turnoutHistory';

const MIN_POINTS = 3;
const MIN_SPAN_MS = 2 * 60 * 1000;

function confidenceFor(pointCount) {
  if (pointCount >= 15) {
    return 'high';
  }
  if (pointCount >= 5) {
    return 'medium';
  }
  return 'low';
}

export function useTurnoutForecast({ electionId, totalVotes, status, endDate }) {
  const [history, setHistory] = useState(() => (electionId ? getHistory(electionId) : []));

  useEffect(() => {
    if (!electionId || typeof totalVotes !== 'number') {
      return;
    }
    setHistory(appendSnapshot(electionId, totalVotes));
  }, [electionId, totalVotes]);

  const forecast = useMemo(() => {
    if (status !== 'Active' || !endDate || history.length < MIN_POINTS) {
      return null;
    }

    const span = history[history.length - 1].t - history[0].t;
    if (span < MIN_SPAN_MS) {
      return null;
    }

    const line = fitLine(history.map((point) => ({ x: point.t, y: point.v })));
    const endTime = new Date(endDate).getTime();
    const rawProjection = predict(line, endTime);
    const currentTotal = history[history.length - 1].v;
    const projectedVotes = Math.round(Math.max(rawProjection, currentTotal));

    return {
      projectedVotes,
      confidence: confidenceFor(history.length),
      pointCount: history.length
    };
  }, [history, status, endDate]);

  return { history, forecast };
}
