import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';

export default function ResultsChart({ tally }) {
  return (
    <ResponsiveContainer width="100%" height={360}>
      <BarChart data={tally}>
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="name" />
        <YAxis allowDecimals={false} />
        <Tooltip />
        <Bar dataKey="voteCount" fill="#6366f1" />
      </BarChart>
    </ResponsiveContainer>
  );
}
