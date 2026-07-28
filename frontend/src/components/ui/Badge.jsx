const statusStyles = {
  Active: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400',
  Upcoming: 'bg-amber-100 text-amber-700 dark:bg-amber-500/10 dark:text-amber-400',
  Closed: 'bg-rose-100 text-rose-700 dark:bg-rose-500/10 dark:text-rose-400',
  // Integrity alert severity + review status — same palette formula, reused
  // rather than invented: Info/Open share the sky "awaiting action" tone,
  // Warning matches Upcoming's amber, Critical matches Closed's rose.
  Info: 'bg-sky-100 text-sky-700 dark:bg-sky-500/10 dark:text-sky-400',
  Warning: 'bg-amber-100 text-amber-700 dark:bg-amber-500/10 dark:text-amber-400',
  Critical: 'bg-rose-100 text-rose-700 dark:bg-rose-500/10 dark:text-rose-400',
  Open: 'bg-sky-100 text-sky-700 dark:bg-sky-500/10 dark:text-sky-400',
  Reviewed: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400',
  Dismissed: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300'
};

export default function Badge({ status, children }) {
  const style = statusStyles[status] || 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300';
  return (
    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${style}`}>
      {children ?? status}
    </span>
  );
}
