import { Line, LineChart, ResponsiveContainer, Tooltip } from 'recharts';
import { TrendingUp } from 'lucide-react';
import Card from './ui/Card';
import { useDarkMode } from '../hooks/useDarkMode';

// Reuses the same indigo/slate pair already validated (via the dataviz
// skill's validate_palette.js) against these chart surfaces in
// ResultsChart.jsx — projected segment takes the accent per the stat-tile
// trend spec ("current period in the accent"), observed history is the
// de-emphasis hue.
const accentColor = { light: '#4f46e5', dark: '#6366f1' };
const mutedColor = { light: '#94a3b8', dark: '#64748b' };
const cardSurfaceColor = { light: '#ffffff', dark: '#0f172a' };

const confidenceLabel = {
  low: 'Low confidence',
  medium: 'Medium confidence',
  high: 'High confidence'
};

function buildChartData(history, forecast, endDate) {
  const observed = history.map((point) => ({ t: point.t, observed: point.v }));
  if (!forecast) {
    return observed;
  }
  const last = history[history.length - 1];
  return [
    ...observed,
    { t: last.t, projected: last.v },
    { t: new Date(endDate).getTime(), projected: forecast.projectedVotes }
  ];
}

function makeProjectedEndpointDot(ringColor) {
  return function ProjectedEndpointDot({ cx, cy, payload, points }) {
    const isLast = points && points[points.length - 1]?.payload === payload;
    if (!isLast) {
      return null;
    }
    return <circle cx={cx} cy={cy} r={4} fill="currentColor" stroke={ringColor} strokeWidth={2} />;
  };
}

export default function TurnoutForecastCard({ history, forecast, endDate }) {
  const { theme } = useDarkMode();

  return (
    <Card className="p-5">
      <div className="flex items-center justify-between gap-4">
        <div className="flex items-center gap-2 text-sm font-semibold text-slate-700 dark:text-slate-300">
          <TrendingUp className="h-4 w-4" />
          Turnout Forecast
        </div>
        {forecast && (
          <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-600 dark:bg-slate-800 dark:text-slate-300">
            {confidenceLabel[forecast.confidence]}
          </span>
        )}
      </div>

      {!forecast && (
        <p className="mt-2 text-sm text-slate-500 dark:text-slate-400">
          Gathering data for a turnout forecast&hellip;
        </p>
      )}

      {forecast && (
        <>
          <p className="mt-3 text-2xl font-bold text-slate-900 dark:text-white">
            ~{forecast.projectedVotes.toLocaleString()} votes
          </p>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            Projected by {new Date(endDate).toLocaleDateString()}
          </p>

          <div className="mt-4 h-24 text-indigo-600 dark:text-indigo-400">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={buildChartData(history, forecast, endDate)} margin={{ top: 4, right: 4, left: 4, bottom: 4 }}>
                <Tooltip
                  labelFormatter={(t) => new Date(t).toLocaleString()}
                  formatter={(value, name) => [value, name === 'projected' ? 'Projected' : 'Observed']}
                />
                <Line
                  type="monotone"
                  dataKey="observed"
                  stroke={mutedColor[theme]}
                  strokeWidth={2}
                  dot={false}
                  isAnimationActive={false}
                />
                <Line
                  type="monotone"
                  dataKey="projected"
                  stroke={accentColor[theme]}
                  strokeWidth={2}
                  strokeDasharray="4 4"
                  dot={makeProjectedEndpointDot(cardSurfaceColor[theme])}
                  isAnimationActive={false}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>

          <div className="mt-2 flex items-center gap-4 text-xs text-slate-400 dark:text-slate-500">
            <span className="flex items-center gap-1.5">
              <span className="inline-block h-0.5 w-3" style={{ backgroundColor: mutedColor[theme] }} />
              Observed
            </span>
            <span className="flex items-center gap-1.5">
              <span
                className="inline-block h-0.5 w-3"
                style={{ backgroundImage: `linear-gradient(to right, ${accentColor[theme]} 60%, transparent 40%)`, backgroundSize: '6px 2px' }}
              />
              Projected
            </span>
          </div>

          <p className="mt-2 text-xs text-slate-400 dark:text-slate-500">
            Estimated from {forecast.pointCount} snapshots observed in this browser — not the
            official count.
          </p>
        </>
      )}
    </Card>
  );
}
