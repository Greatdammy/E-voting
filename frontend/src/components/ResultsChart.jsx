import { Bar, BarChart, CartesianGrid, LabelList, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { useDarkMode } from '../hooks/useDarkMode';

// Validated via the dataviz skill's validate_palette.js against the actual
// chart surfaces (#ffffff light / #0f172a dark) — both pass lightness band,
// chroma floor, and contrast. Single hue for all bars: candidates are a
// nominal category with one measure (vote count), not a multi-series chart,
// so per the color formula every bar takes the same slot-1 hue.
const barColor = { light: '#4f46e5', dark: '#6366f1' };
const gridColor = { light: '#e1e0d9', dark: '#2c2c2a' };
const axisColor = { light: '#898781', dark: '#898781' };
const labelColor = { light: '#0b0b0b', dark: '#ffffff' };

function ChartTooltip({ active, payload }) {
  if (!active || !payload?.length) {
    return null;
  }

  const { name, party, voteCount } = payload[0].payload;

  return (
    <div className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm shadow-lg dark:border-slate-700 dark:bg-slate-800">
      <p className="text-base font-semibold text-slate-900 dark:text-white">{voteCount} votes</p>
      <p className="text-slate-500 dark:text-slate-400">
        {name} · {party}
      </p>
    </div>
  );
}

export default function ResultsChart({ tally }) {
  const { theme } = useDarkMode();

  return (
    <ResponsiveContainer width="100%" height={320}>
      <BarChart data={tally} margin={{ top: 24, right: 8, left: 8, bottom: 8 }}>
        <CartesianGrid vertical={false} stroke={gridColor[theme]} />
        <XAxis
          dataKey="name"
          tick={{ fill: axisColor[theme], fontSize: 12 }}
          axisLine={{ stroke: gridColor[theme] }}
          tickLine={false}
        />
        <YAxis allowDecimals={false} tick={{ fill: axisColor[theme], fontSize: 12 }} axisLine={false} tickLine={false} />
        <Tooltip
          content={<ChartTooltip />}
          cursor={{ fill: theme === 'dark' ? 'rgba(255,255,255,0.04)' : 'rgba(15,23,42,0.04)' }}
        />
        <Bar dataKey="voteCount" fill={barColor[theme]} radius={[4, 4, 0, 0]} maxBarSize={24}>
          <LabelList dataKey="voteCount" position="top" fill={labelColor[theme]} fontSize={12} fontWeight={600} />
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
