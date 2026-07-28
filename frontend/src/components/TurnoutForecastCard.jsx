import { Line, LineChart, ResponsiveContainer, Tooltip } from 'recharts';
import { Sparkles } from 'lucide-react';
import Card from './ui/Card';
import Spinner from './ui/Spinner';
import { useDarkMode } from '../hooks/useDarkMode';

// Fuchsia is the shared "AI accent" — used only here, on integrity alerts,
// and on the elections-list insight chip, so it reads as one consistent
// signal for "this is AI-derived, not the official count," never the
// brand's indigo/violet. Contrast checked by hand against these same card
// surfaces (#ffffff light / #0f172a dark) per the dataviz skill's WCAG
// text-contrast rule for a lone accent color (not a categorical palette):
// fuchsia-600 on white ≈ 4.7:1, fuchsia-400 on #0f172a ≈ 7.2:1 — both clear
// the 4.5:1 normal-text floor. Observed history keeps the de-emphasis hue.
const accentColor = { light: '#c026d3', dark: '#e879f9' };
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
          <Sparkles className="h-4 w-4 text-fuchsia-600 dark:text-fuchsia-400" />
          Turnout Forecast
        </div>
        {forecast && (
          <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs font-medium text-slate-600 dark:bg-slate-800 dark:text-slate-300">
            {confidenceLabel[forecast.confidence]}
          </span>
        )}
      </div>

      {!forecast && <Spinner variant="ai" label="Gathering data for a turnout forecast…" className="mt-2" />}

      {forecast && (
        <>
          <p className="mt-3 text-2xl font-bold text-slate-900 dark:text-white">
            ~{forecast.projectedVotes.toLocaleString()} votes
          </p>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            Projected by {new Date(endDate).toLocaleDateString()}
          </p>

          <div className="mt-4 h-24 text-fuchsia-600 dark:text-fuchsia-400">
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
